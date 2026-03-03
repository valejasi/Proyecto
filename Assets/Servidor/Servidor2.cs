using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class Servidor2 : MonoBehaviour
{
    [Header("Servidor")]
    [SerializeField] private string baseUrl = "https://proyecto-y1ud.onrender.com";
    [SerializeField] private float intervalo = 0.1f;

    [Header("Sala")]
    [SerializeField] private string codigoSala = "";
    [SerializeField] private string miSessionId = "";
    [SerializeField] private bool autoCrearEnStart = true;

    [Header("Join por teclado")]
    [SerializeField] private bool habilitarJoinConEnter = true;
    [SerializeField] private string codigoParaUnirse = "";

    [Header("Portadrones")]
    [SerializeField] private Transform miPorta;
    [SerializeField] private Transform portaRemoto;
    [SerializeField] private int miObjIdPorta = 0;
    [SerializeField] private bool colocarPortaAlIniciar = true;

    private WaitForSeconds waitIntervalo;
    private bool portaColocada = false;

    [System.Serializable]
    private class JoinResponse
    {
        public string codigo;
        public string sessionId;
        public int cantidad;
    }

    [System.Serializable]
    private class Position
    {
        public string sessionId;
        public int slot;
        public string tipo;
        public int objId;

        public float x, y, z;
        public float qx, qy, qz, qw;
    }

    [System.Serializable]
    private class RespuestaEstado
    {
        public Position[] posiciones;
    }

    void Awake()
    {
        waitIntervalo = new WaitForSeconds(intervalo);
    }

    void Start()
    {
        if (autoCrearEnStart)
            StartCoroutine(CrearSala());
        else if (!string.IsNullOrWhiteSpace(codigoSala) && !string.IsNullOrWhiteSpace(miSessionId))
            IniciarLoops();
    }

    void Update()
    {
        if (!habilitarJoinConEnter) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!string.IsNullOrWhiteSpace(codigoParaUnirse))
            {
                StopAllCoroutines();
                StartCoroutine(UnirseSala(codigoParaUnirse.Trim()));
            }
        }
    }

    IEnumerator CrearSala()
    {
        string url = $"{baseUrl}/game/create";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"CREATE ERROR: {req.error}");
                yield break;
            }

            JoinResponse resp;
            try { resp = JsonUtility.FromJson<JoinResponse>(req.downloadHandler.text); }
            catch { Debug.LogError("CREATE ERROR: parse JoinResponse"); yield break; }

            codigoSala = resp.codigo;
            miSessionId = resp.sessionId;

            Debug.Log($"SALA CREADA ✅ codigo={codigoSala} sessionId={miSessionId} jugadores={resp.cantidad}");

            IniciarLoops();
        }
    }

    IEnumerator UnirseSala(string codigo)
    {
        string url = $"{baseUrl}/game/join/{codigo}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"JOIN ERROR: {req.error}");
                yield break;
            }

            JoinResponse resp;
            try { resp = JsonUtility.FromJson<JoinResponse>(req.downloadHandler.text); }
            catch { Debug.LogError("JOIN ERROR: parse JoinResponse"); yield break; }

            codigoSala = resp.codigo;
            miSessionId = resp.sessionId;

            Debug.Log($"ME UNI ✅ codigo={codigoSala} sessionId={miSessionId} jugadores={resp.cantidad}");

            IniciarLoops();
        }
    }

    public void IniciarLoops()
    {
        StopAllCoroutines();
        StartCoroutine(SyncLoop());
    }

    IEnumerator SyncLoop()
    {
        while (true)
        {
            if (colocarPortaAlIniciar && !portaColocada)
                yield return PlacePortaOnce();

            yield return SendMiPortaMove();
            yield return GetStateAndAplicarPortaRemoto();

            yield return waitIntervalo;
        }
    }

    IEnumerator PlacePortaOnce()
    {
        if (portaColocada) yield break;
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;
        if (miPorta == null) yield break;
        if (miObjIdPorta <= 0) yield break;

        Position pos = new Position();
        pos.sessionId = miSessionId;
        pos.objId = miObjIdPorta;
        pos.x = miPorta.position.x;
        pos.y = miPorta.position.y;
        pos.z = miPorta.position.z;

        Quaternion q = miPorta.rotation;
        pos.qx = q.x; pos.qy = q.y; pos.qz = q.z; pos.qw = q.w;

        string json = JsonUtility.ToJson(pos);
        string url = $"{baseUrl}/game/placePorta/{codigoSala}";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                yield break;

            if (req.downloadHandler.text == "OK")
            {
                portaColocada = true;
                Debug.Log("PORTA COLOCADO ✅");
            }
        }
    }

    IEnumerator SendMiPortaMove()
    {
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;
        if (miPorta == null) yield break;
        if (!portaColocada) yield break;

        Position pos = new Position();
        pos.sessionId = miSessionId;
        pos.objId = miObjIdPorta;
        pos.x = miPorta.position.x;
        pos.y = miPorta.position.y;
        pos.z = miPorta.position.z;

        Quaternion q = miPorta.rotation;
        pos.qx = q.x; pos.qy = q.y; pos.qz = q.z; pos.qw = q.w;

        string json = "{\"items\":[" + JsonUtility.ToJson(pos) + "]}";
        string url = $"{baseUrl}/game/moveBatch/{codigoSala}";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
        }
    }

    IEnumerator GetStateAndAplicarPortaRemoto()
    {
        if (string.IsNullOrWhiteSpace(codigoSala)) yield break;

        string url = $"{baseUrl}/game/state/{codigoSala}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success) yield break;

            string json = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(json) || json.StartsWith("\"")) yield break;

            RespuestaEstado estado;
            try { estado = JsonUtility.FromJson<RespuestaEstado>(json); }
            catch { yield break; }

            if (estado == null || estado.posiciones == null) yield break;

            for (int i = 0; i < estado.posiciones.Length; i++)
            {
                Position p = estado.posiciones[i];
                if (p == null) continue;
                if (p.tipo != "PORTA") continue;
                if (!string.IsNullOrWhiteSpace(miSessionId) && p.sessionId == miSessionId) continue;
                if (portaRemoto == null) continue;

                portaRemoto.position = new Vector3(p.x, p.y, p.z);
                portaRemoto.rotation = new Quaternion(p.qx, p.qy, p.qz, p.qw);
                break;
            }
        }
    }
}