using System.Collections;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Networking;

public class testServidor : MonoBehaviour
{
    [Header("Servidor")]
    [SerializeField] private string baseUrl = "https://proyecto-y1ud.onrender.com";

    [Header("PORTADRONES (arrastrar)")]
    [SerializeField] private Transform porta1; // host (aéreo)
    [SerializeField] private Transform porta2; // join (naval)

    [Header("DRONES P1 (AÉREO: 12)")]
    [SerializeField] private Transform[] dronesP1;

    [Header("DRONES P2 (NAVAL: 6)")]
    [SerializeField] private Transform[] dronesP2;

    [Header("Sync")]
    [SerializeField] private float intervalo = 0.1f;

    [Header("Smooth remoto")]
    [SerializeField] private float smoothPos = 12f;
    [SerializeField] private float smoothRot = 12f;

    // Estado sala
    private string codigoSala = "";
    private string miSessionId = "";
    private string codigoIngresado = "";

    // Slot: 1 = host (aéreo), 2 = join (naval)
    private int miSlot = 0;

    // Coroutines
    private Coroutine sendLoop;
    private Coroutine receiveLoop;

    // Porta: se coloca una vez
    private bool portaEnviada = false;

    // Remotos smoothing: objId -> targets
    private readonly System.Collections.Generic.Dictionary<string, Vector3> remoteTargetPos =
        new System.Collections.Generic.Dictionary<string, Vector3>();
    private readonly System.Collections.Generic.Dictionary<string, Quaternion> remoteTargetRot =
        new System.Collections.Generic.Dictionary<string, Quaternion>();

    // Cache de transforms por objId
    private readonly System.Collections.Generic.Dictionary<string, Transform> misObjetos =
        new System.Collections.Generic.Dictionary<string, Transform>();
    private readonly System.Collections.Generic.Dictionary<string, Transform> objetosRemotos =
        new System.Collections.Generic.Dictionary<string, Transform>();

    // Estado recibido (para UI)
    private StateResponse lastState;

    private WaitForSeconds waitIntervalo;

    [SerializeField] float minPos = 0.01f;
    [SerializeField] float minRot = 0.5f;
    private Vector3[] ultimaPos;
    private Quaternion[] ultimaRot;

    void Awake()
    {
        waitIntervalo = new WaitForSeconds(intervalo);
    }

    void Start()
    {
        Debug.Log("testServidor activo");

        if (porta1 == null || porta2 == null)
        {
            Debug.LogError("Asigná porta1 y porta2 en el Inspector.");
            return;
        }

        if (dronesP1 == null || dronesP1.Length == 0)
            Debug.LogWarning("dronesP1 está vacío (aéreo). Si es intencional, ok.");

        if (dronesP2 == null || dronesP2.Length == 0)
            Debug.LogWarning("dronesP2 está vacío (naval). Si es intencional, ok.");

        RebuildObjectMapsForSlotPreview();
    }

    void Update()
    {
        // Host: crear sala
        if (Input.GetKeyDown(KeyCode.C))
            StartCoroutine(CreateAndStore());

        // Join: escribir código + Enter
        CapturarCodigoJoin();

        // Colocar PORTA (por ahora con K). Luego lo cambiamos a confirmación con mouse.
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (!portaEnviada)
                StartCoroutine(PlacePortaOnce());
            else
                Debug.Log("PORTA ya fue enviado/lockeado.");
        }

        // Aplicar smoothing a TODOS los objetos remotos
        foreach (var kv in objetosRemotos)
        {
            string objId = kv.Key;
            Transform t = kv.Value;
            if (t == null) continue;

            if (remoteTargetPos.TryGetValue(objId, out Vector3 tp))
                t.position = Vector3.Lerp(t.position, tp, 1f - Mathf.Exp(-smoothPos * Time.deltaTime));

            if (remoteTargetRot.TryGetValue(objId, out Quaternion tr))
                t.rotation = Quaternion.Slerp(t.rotation, tr, 1f - Mathf.Exp(-smoothRot * Time.deltaTime));
        }
    }

    void IniciarSyncAutomatico()
    {
        if (sendLoop == null) sendLoop = StartCoroutine(SendLoop());
        if (receiveLoop == null) receiveLoop = StartCoroutine(ReceiveLoop());
        Debug.Log("Sync automático iniciado");
    }

    void CapturarCodigoJoin()
    {
        foreach (char ch in Input.inputString)
        {
            // Enter
            if (ch == '\n' || ch == '\r')
            {
                if (!string.IsNullOrWhiteSpace(codigoIngresado))
                {
                    codigoSala = codigoIngresado.Trim().ToLower();
                    StartCoroutine(JoinAndStore(codigoSala));
                }
                continue;
            }

            // Backspace
            if (ch == '\b')
            {
                if (codigoIngresado.Length > 0)
                    codigoIngresado = codigoIngresado.Substring(0, codigoIngresado.Length - 1);
                continue;
            }

            // Solo letras/números
            if (char.IsLetterOrDigit(ch) && codigoIngresado.Length < 6)
                codigoIngresado += char.ToLower(ch);
        }
    }

    // ==========================
    // CREATE / JOIN
    // ==========================
    IEnumerator CreateAndStore()
    {
        string url = baseUrl + "/game/create";
        Debug.Log("GET: " + url);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Create ERROR: " + req.error);
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("Create JSON: " + json);

            JoinResponse resp = JsonUtility.FromJson<JoinResponse>(json);
            codigoSala = resp.codigo;
            miSessionId = resp.sessionId;

            // HOST = SLOT 1
            SetSlot(1);
            IniciarSyncAutomatico();

            Debug.Log($"CREADO. codigoSala={codigoSala} miSessionId={miSessionId} jugadores={resp.jugadores}");
        }
    }

    IEnumerator JoinAndStore(string code)
    {
        string url = baseUrl + "/game/join/" + code;
        Debug.Log("GET: " + url);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Join ERROR: " + req.error);
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("Join JSON: " + json);

            JoinResponse resp = JsonUtility.FromJson<JoinResponse>(json);

            if (resp.jugadores == 0)
            {
                Debug.LogError("No se pudo unir: sala no existe o código incorrecto.");
                yield break;
            }

            codigoSala = resp.codigo;
            miSessionId = resp.sessionId;

            // CLIENTE = SLOT 2
            SetSlot(2);
            IniciarSyncAutomatico();

            Debug.Log($"UNIDO. codigoSala={codigoSala} miSessionId={miSessionId} jugadores={resp.jugadores}");
        }
    }

    void SetSlot(int slot)
    {
        miSlot = slot;
        portaEnviada = false;

        RebuildObjectMapsForSlot();
        AplicarOwnershipMover();

        Debug.Log($"Slot asignado: {miSlot}. Mis objetos: {misObjetos.Count}. Remotos: {objetosRemotos.Count}");
    }

    void RebuildObjectMapsForSlot()
    {
        misObjetos.Clear();
        objetosRemotos.Clear();

        // 🔧 Limpia targets remotos para no arrastrar basura si reiniciás
        remoteTargetPos.Clear();
        remoteTargetRot.Clear();

        // Porta
        Transform miPorta = (miSlot == 1) ? porta1 : porta2;
        Transform otroPorta = (miSlot == 1) ? porta2 : porta1;

        misObjetos["PORTA"] = miPorta;
        objetosRemotos["PORTA"] = otroPorta;

        // Drones
        Transform[] misDrones = (miSlot == 1) ? dronesP1 : dronesP2;
        Transform[] dronesOtro = (miSlot == 1) ? dronesP2 : dronesP1;

        if (misDrones != null)
        {
            for (int i = 0; i < misDrones.Length; i++)
            {
                if (misDrones[i] == null) continue;
                string objId = $"DRON_{i + 1}";
                misObjetos[objId] = misDrones[i];
            }
        }

        if (dronesOtro != null)
        {
            for (int i = 0; i < dronesOtro.Length; i++)
            {
                if (dronesOtro[i] == null) continue;
                string objId = $"DRON_{i + 1}";
                objetosRemotos[objId] = dronesOtro[i];
            }
        }

        // Inicial targets de smoothing
        foreach (var kv in objetosRemotos)
        {
            if (kv.Value == null) continue;
            remoteTargetPos[kv.Key] = kv.Value.position;
            remoteTargetRot[kv.Key] = kv.Value.rotation;
        }
    }

    void RebuildObjectMapsForSlotPreview()
    {
        miSlot = 1;
        RebuildObjectMapsForSlot();

        miSlot = 0;
        misObjetos.Clear();
        objetosRemotos.Clear();
        remoteTargetPos.Clear();
        remoteTargetRot.Clear();
    }

    void AplicarOwnershipMover()
    {
        var mP1 = porta1.GetComponent<Mover>();
        var mP2 = porta2.GetComponent<Mover>();
        if (mP1 != null) mP1.isMine = (miSlot == 1);
        if (mP2 != null) mP2.isMine = (miSlot == 2);

        var rbP1 = porta1.GetComponent<Rigidbody>();
        var rbP2 = porta2.GetComponent<Rigidbody>();
        if (rbP1 != null) rbP1.isKinematic = (miSlot != 1);
        if (rbP2 != null) rbP2.isKinematic = (miSlot != 2);

        Transform[] d1 = dronesP1;
        Transform[] d2 = dronesP2;

        if (d1 != null)
        {
            for (int i = 0; i < d1.Length; i++)
            {
                if (d1[i] == null) continue;
                var m = d1[i].GetComponent<Mover>();
                if (m != null) m.isMine = (miSlot == 1);

                var rb = d1[i].GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = (miSlot != 1);
            }
        }

        if (d2 != null)
        {
            for (int i = 0; i < d2.Length; i++)
            {
                if (d2[i] == null) continue;
                var m = d2[i].GetComponent<Mover>();
                if (m != null) m.isMine = (miSlot == 2);

                var rb = d2[i].GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = (miSlot != 2);
            }
        }
    }

    IEnumerator SendLoop()
    {
        while (true)
        {
            yield return SendMoveBatchDrones();
            yield return waitIntervalo;
        }
    }

    IEnumerator ReceiveLoop()
    {
        while (true)
        {
            yield return GetStateAndApplyRemotos();
            yield return waitIntervalo;
        }
    }

    IEnumerator PlacePortaOnce()
    {
        // ✅ evita doble envío
        if (portaEnviada) yield break;

        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId))
        {
            Debug.LogWarning("No hay sala/sessionId. No se puede colocar porta.");
            yield break;
        }

        if (!misObjetos.TryGetValue("PORTA", out Transform miPorta) || miPorta == null)
        {
            Debug.LogError("No tengo mi PORTA asignado en misObjetos.");
            yield break;
        }

        string url = baseUrl + "/game/placePorta/" + codigoSala;

        PositionData data = new PositionData(miSessionId, miSlot, "PORTA", miPorta.position, miPorta.rotation);
        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("PlacePorta ERROR: " + req.error + " | " + req.downloadHandler.text);
                yield break;
            }

            // ✅ leer OK/NO del backend
            string resp = (req.downloadHandler.text ?? "").Trim();
            portaEnviada = (resp == "OK");
            Debug.Log("PlacePorta RESP: " + resp);

            if (portaEnviada)
                Debug.Log("PORTA enviado y bloqueado en server.");
            else
                Debug.LogWarning("El server no aceptó el PORTA (resp != OK).");
        }
    }

    bool DronMove (int i, Transform t)
    {
        if ((t.position - ultimaPos[i]).sqrMagnitude > minPos * minPos)
            return true;

        if (Quaternion.Angle(t.rotation, ultimaRot[i]) > minRot)
            return true;

        return false;
    }

    IEnumerator SendMoveBatchDrones()
    {
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;

        // ✅ no empieza la partida real hasta colocar porta
        if (!portaEnviada) yield break;

        Transform[] misDrones = (miSlot == 1) ? dronesP1 : dronesP2;
        if (misDrones == null || misDrones.Length == 0) yield break;

        int n = misDrones.Length;
        ultimaPos = new Vector3[n];
        ultimaRot = new Quaternion[n];
        
        PositionData[] items = new PositionData[misDrones.Length];
        int count = 0;

        for (int i = 0; i < misDrones.Length; i++)
        {
            Transform t = misDrones[i];
            if (t == null) continue;
            
            //Mandar posición solo si el dron se movió un mínimo de distancia, sino se está mandando todo el tiempo la posición de todos los drones, incluso si están quietos.
            if (!DronMove(i, t)) continue; 

            ultimaPos[i] = t.position;
            ultimaRot[i] = t.rotation;

            string objId = $"DRON_{i + 1}";
            items[count] = new PositionData(miSessionId, miSlot, objId, t.position, t.rotation);
            count++;
        }

        if (count == 0) yield break;

        if (count != items.Length)
        {
            PositionData[] trimmed = new PositionData[count];
            for (int i = 0; i < count; i++) trimmed[i] = items[i];
            items = trimmed;
        }

        MoveBatchRequest payload = new MoveBatchRequest { items = items };
        string json = JsonUtility.ToJson(payload);

        string url = baseUrl + "/game/moveBatch/" + codigoSala;

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("SendMoveBatch ERROR: " + req.error + " | " + req.downloadHandler.text);
        }
    }

    IEnumerator GetStateAndApplyRemotos()
    {
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;

        string url = baseUrl + "/game/state/" + codigoSala;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                yield break;

            string json = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(json))
                yield break;

            StateResponse st = JsonUtility.FromJson<StateResponse>(json);
            lastState = st;

            if (st == null || st.posiciones == null)
                yield break;

            for (int i = 0; i < st.posiciones.Length; i++)
            {
                PositionData p = st.posiciones[i];

                if (p.sessionId == miSessionId)
                    continue;

                if (string.IsNullOrWhiteSpace(p.objId))
                    continue;

                if (!objetosRemotos.TryGetValue(p.objId, out Transform t) || t == null)
                    continue;

                Vector3 pos = new Vector3(p.x, p.y, p.z);
                Quaternion rot = new Quaternion(p.qx, p.qy, p.qz, p.qw);

                remoteTargetPos[p.objId] = pos;
                remoteTargetRot[p.objId] = rot;
            }
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 1000, 25), "Host: C = Create | Cliente: escribí código (6) + Enter = Join | Colocar PORTA: K");
        GUI.Label(new Rect(10, 35, 900, 25), "Código tipeado: " + codigoIngresado);

        GUI.Label(new Rect(10, 60, 900, 25), "Sala: " + (string.IsNullOrWhiteSpace(codigoSala) ? "(ninguna)" : codigoSala));
        GUI.Label(new Rect(10, 85, 900, 25), "SessionId: " + (string.IsNullOrWhiteSpace(miSessionId) ? "(sin asignar)" : miSessionId));
        GUI.Label(new Rect(10, 110, 900, 25), "Slot: " + (miSlot == 0 ? "(no asignado)" : miSlot.ToString()));
        GUI.Label(new Rect(10, 135, 900, 25), "PORTA enviado: " + portaEnviada);

        if (lastState != null && lastState.vidas != null)
        {
            int y = 165;
            GUI.Label(new Rect(10, y, 900, 25), "VIDAS (server):");
            y += 20;

            for (int i = 0; i < lastState.vidas.Length; i++)
            {
                var v = lastState.vidas[i];
                if (v.sessionId != miSessionId) continue;
                GUI.Label(new Rect(10, y, 900, 20), $"{v.objId}: {v.vida}");
                y += 18;
            }
        }
    }

    // ==========================
    // DTOs
    // ==========================
    [System.Serializable]
    public class JoinResponse
    {
        public string codigo;
        public string sessionId;
        public int jugadores;
    }

    [System.Serializable]
    public class MoveBatchRequest
    {
        public PositionData[] items;
    }

    [System.Serializable]
    public class StateResponse
    {
        public PositionData[] posiciones;
        public VidaData[] vidas;
        public AmmoData[] municion;
        public ProyectilData[] proyectiles;
    }

    [System.Serializable]
    public class VidaData
    {
        public string sessionId;
        public string objId;
        public int vida;
    }

    [System.Serializable]
    public class AmmoData
    {
        public string sessionId;
        public string objId;
        public int ammo;
    }

    [System.Serializable]
    public class ProyectilData
    {
        public string id;
        public float x, y, z;
    }

    [System.Serializable]
    public struct PositionData
    {
        public string sessionId;
        public int slot;
        public string objId;

        public float x, y, z;
        public float qx, qy, qz, qw;

        public PositionData(string sid, int slot, string objId, Vector3 p, Quaternion q)
        {
            this.sessionId = sid;
            this.slot = slot;
            this.objId = objId;

            x = p.x; y = p.y; z = p.z;
            qx = q.x; qy = q.y; qz = q.z; qw = q.w;
        }
    }
}