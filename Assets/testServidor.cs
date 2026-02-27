using System.Collections;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Networking;

public class testServidor : MonoBehaviour
{
    [Header("Servidor")]
    [SerializeField] private string baseUrl = "https://proyecto-y1ud.onrender.com";

    [Header("CUBOS / DRONES EN LA ESCENA (ARRASTRAR ACÁ)")]
    [SerializeField] private Transform cubo1; // Player 1
    [SerializeField] private Transform cubo2; // Player 2

    [Header("Sync")]
    [SerializeField] private float intervalo = 0.1f;

    [Header("Smooth remoto")]
    [SerializeField] private float smoothPos = 12f;
    [SerializeField] private float smoothRot = 12f;

    // Estado salagit git
    private string codigoSala = "";
    private string miSessionId = "";
    private string codigoIngresado = ""; // lo que tipeas para join
    private Coroutine loopSync;

    // Slot: 1 = cubo1, 2 = cubo2
    private int miSlot = 0;
    private Transform miCubo;
    private Transform otroCubo;

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

        if (cubo1 == null || cubo2 == null)
        {
            Debug.LogError("Asigná cubo1 y cubo2 en el Inspector (dos objetos en escena).");
            return;
        }

        // default targets
        remoteTargetPos = cubo2.position;
        remoteTargetRot = cubo2.rotation;
    }

    void Update()
    {
        // Host: crear sala
        if (Input.GetKeyDown(KeyCode.C))
            StartCoroutine(CreateAndStore());

        // Join: escribir código + Enter
        CapturarCodigoJoin();

        // Sync toggle
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (loopSync == null) loopSync = StartCoroutine(SyncLoop());
            else { StopCoroutine(loopSync); loopSync = null; Debug.Log("Sync detenido"); }
        }

        // Smooth SOLO al cubo remoto
        if (otroCubo != null && remoteHasTarget)
        {
            otroCubo.position = Vector3.Lerp(otroCubo.position, remoteTargetPos, 1f - Mathf.Exp(-smoothPos * Time.deltaTime));
            otroCubo.rotation = Quaternion.Slerp(otroCubo.rotation, remoteTargetRot, 1f - Mathf.Exp(-smoothRot * Time.deltaTime));
        }
    }

    // ==========================
    // JOIN INPUT
    // ==========================
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

            // Solo letras/números (códigos tipo ad4768)
            if (char.IsLetterOrDigit(ch) && codigoIngresado.Length < 6)
            {
                codigoIngresado += char.ToLower(ch);
            }
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

            // si sala no existe (según tu controller)
            if (resp.jugadores == 0)
            {
                Debug.LogError("No se pudo unir: sala no existe o código incorrecto.");
                yield break;
            }

            codigoSala = resp.codigo;
            miSessionId = resp.sessionId;

            // CLIENTE = SLOT 2
            SetSlot(2);

            Debug.Log($"UNIDO. codigoSala={codigoSala} miSessionId={miSessionId} jugadores={resp.jugadores}");
        }
    }

    void SetSlot(int slot)
    {
        miSlot = slot;

        if (miSlot == 1)
        {
            miCubo = cubo1;
            otroCubo = cubo2;
        }
        else
        {
            miCubo = cubo2;
            otroCubo = cubo1;
        }

        // Importantísimo: solo mi cubo lee input
        AplicarOwnershipMover();

        // Inicializa smooth target
        if (otroCubo != null)
        {
            remoteTargetPos = otroCubo.position;
            remoteTargetRot = otroCubo.rotation;
            remoteHasTarget = true;
        }
    }

    void AplicarOwnershipMover()
    {
        // Si tus drones/cubos tienen el script Mover, acá hacemos que SOLO el mío se mueva con teclado.
        // Si no tienen Mover, no pasa nada.
        var m1 = cubo1.GetComponent<Mover>();
        var m2 = cubo2.GetComponent<Mover>();

        if (m1 != null) m1.isMine = (miSlot == 1);
        if (m2 != null) m2.isMine = (miSlot == 2);

        // Recomendado: el remoto kinematic para que no lo afecte la física local
        var rb1 = cubo1.GetComponent<Rigidbody>();
        var rb2 = cubo2.GetComponent<Rigidbody>();

        if (rb1 != null) rb1.isKinematic = (miSlot != 1);
        if (rb2 != null) rb2.isKinematic = (miSlot != 2);
    }

    // ==========================
    // SYNC LOOP
    // ==========================
    IEnumerator SyncLoop()
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

    IEnumerator SendMove()
    {
        if (miCubo == null) yield break;
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;

        string url = baseUrl + "/game/move/" + codigoSala;

        Vector3 pos = miCubo.position;
        Quaternion rot = miCubo.rotation;

        // Mandamos sessionId + slot por si tu backend lo quiere usar.
        PositionData data = new PositionData(miSessionId, miSlot, pos, rot);
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
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("SendMove ERROR: " + req.error);
        }
    }

    IEnumerator GetStateAndApplyOther()
    {
        if (otroCubo == null) yield break;
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;

        string url = baseUrl + "/game/state/" + codigoSala;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;

            string json = req.downloadHandler.text;

            // Tomamos el estado del "otro" (distinto a miSessionId)
            PositionData other = ExtractOtherPlayerPosition(json, miSessionId);
            if (other == null) yield break;

            remoteTargetPos = new Vector3(other.x, other.y, other.z);
            remoteTargetRot = new Quaternion(other.qx, other.qy, other.qz, other.qw);
            remoteHasTarget = true;
        }
    }

    // ==========================
    // PARSER (como lo tenías)
    // ==========================
    PositionData ExtractOtherPlayerPosition(string json, string myId)
    {
        if (string.IsNullOrEmpty(json)) return null;
        if (!json.Contains("\"x\":")) return null;

        int idx = 0;
        while (true)
        {
            int keyStart = json.IndexOf("\"", idx);
            if (keyStart < 0) return null;
            int keyEnd = json.IndexOf("\"", keyStart + 1);
            if (keyEnd < 0) return null;

            string sessionId = json.Substring(keyStart + 1, keyEnd - keyStart - 1);

            int objStart = json.IndexOf("{", keyEnd);
            int objEnd = json.IndexOf("}", objStart);
            if (objStart < 0 || objEnd < 0) return null;

            string obj = json.Substring(objStart, objEnd - objStart + 1);

            if (sessionId == myId)
            {
                idx = objEnd + 1;
                continue;
            }

            float x = FindNumber(obj, "\"x\":");
            float y = FindNumber(obj, "\"y\":");
            float z = FindNumber(obj, "\"z\":");

            float qx = FindNumber(obj, "\"qx\":");
            float qy = FindNumber(obj, "\"qy\":");
            float qz = FindNumber(obj, "\"qz\":");
            float qw = FindNumber(obj, "\"qw\":");

            if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z) ||
                float.IsNaN(qx) || float.IsNaN(qy) || float.IsNaN(qz) || float.IsNaN(qw))
                return null;

            // OJO: acá usamos el constructor “de lectura” (sin sid obligatorio)
            return new PositionData(x, y, z, qx, qy, qz, qw);
        }
    }

    float FindNumber(string text, string key)
    {
        int i = text.IndexOf(key);
        if (i < 0) return float.NaN;

        i += key.Length;
        while (i < text.Length && text[i] == ' ') i++;

        int start = i;
        while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '-' || text[i] == '.' || text[i] == 'E' || text[i] == 'e' || text[i] == '+'))
            i++;

        string num = text.Substring(start, i - start);
        if (float.TryParse(num, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val))
            return val;

        return float.NaN;
    }

    // ==========================
    // GUI
    // ==========================
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 800, 25), "Host: C = Create sala | Sync: P");
        GUI.Label(new Rect(10, 35, 900, 25), "Cliente: escribí el código (6 chars) y Enter = Join");
        GUI.Label(new Rect(10, 60, 700, 25), "Código tipeado: " + codigoIngresado);

        GUI.Label(new Rect(10, 90, 700, 25), "Mi sala actual: " + (string.IsNullOrWhiteSpace(codigoSala) ? "(ninguna)" : codigoSala));
        GUI.Label(new Rect(10, 115, 900, 25), "Mi sessionId: " + (string.IsNullOrWhiteSpace(miSessionId) ? "(sin asignar)" : miSessionId));
        GUI.Label(new Rect(10, 140, 900, 25), "Mi slot: " + (miSlot == 0 ? "(no asignado)" : miSlot.ToString()));
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
    public class PositionData
    {
        // Para que el backend pueda distinguir
        public string sessionId;
        public int slot; // 1 o 2 (por si lo querés usar en server)

        public float x, y, z;
        public float qx, qy, qz, qw;

        // ✅ Constructor NUEVO (evita el CS7036)
        public PositionData(string sid, int slot, Vector3 p, Quaternion q)
        {
            this.sessionId = sid;
            this.slot = slot;
            x = p.x; y = p.y; z = p.z;
            qx = q.x; qy = q.y; qz = q.z; qw = q.w;
        }

        // ✅ Constructor viejo (por compatibilidad, por si te quedó algo llamándolo así)
        public PositionData(Vector3 p, Quaternion q)
        {
            sessionId = "";
            slot = 0;
            x = p.x; y = p.y; z = p.z;
            qx = q.x; qy = q.y; qz = q.z; qw = q.w;
        }

        // ✅ Para LEER del state (server -> local)
        public PositionData(float x, float y, float z, float qx, float qy, float qz, float qw)
        {
            sessionId = "";
            slot = 0;
            this.x = x; this.y = y; this.z = z;
            this.qx = qx; this.qy = qy; this.qz = qz; this.qw = qw;
        }
    }
}