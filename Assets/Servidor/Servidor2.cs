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

    private WaitForSeconds waitIntervalo;

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
        public float x;
        public float y;
        public float z;
        public float rx;
        public float ry;
        public float rz;
        public float rw;
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

        if (!autoCrearEnStart && !string.IsNullOrWhiteSpace(codigoSala) && !string.IsNullOrWhiteSpace(miSessionId))
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

    public void SetBaseUrl(string url) => baseUrl = url;
    public void SetPortas(Transform mi, Transform remoto) { miPorta = mi; portaRemoto = remoto; }
    public void SetCodigoParaUnirse(string codigo) => codigoParaUnirse = codigo;

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
            catch
            {
                Debug.LogError("CREATE ERROR: No pude parsear JoinResponse");
                yield break;
            }

            if (resp == null || string.IsNullOrWhiteSpace(resp.codigo) || string.IsNullOrWhiteSpace(resp.sessionId))
            {
                Debug.LogError("CREATE ERROR: respuesta inválida");
                yield break;
            }

            codigoSala = resp.codigo;
            miSessionId = resp.sessionId;

            Debug.Log($"SALA CREADA ✅  codigo={codigoSala}  sessionId={miSessionId}  jugadores={resp.cantidad}");
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
            catch
            {
                Debug.LogError("JOIN ERROR: No pude parsear JoinResponse");
                yield break;
            }

            if (resp == null || string.IsNullOrWhiteSpace(resp.codigo) || string.IsNullOrWhiteSpace(resp.sessionId))
            {
                Debug.LogError("JOIN ERROR: respuesta inválida");
                yield break;
            }

            codigoSala = resp.codigo;
            miSessionId = resp.sessionId;

            Debug.Log($"ME UNI ✅  codigo={codigoSala}  sessionId={miSessionId}  jugadores={resp.cantidad}");
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
            yield return GetStateAndAplicarPortaRemoto();
            yield return waitIntervalo;
        }
    }

    IEnumerator GetStateAndAplicarPortaRemoto()
    {
        if (string.IsNullOrWhiteSpace(codigoSala))
            yield break;

        string url = $"{baseUrl}/game/state/{codigoSala}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                yield break;

            string json = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(json) || json.StartsWith("\""))
                yield break;

            RespuestaEstado estado;
            try { estado = JsonUtility.FromJson<RespuestaEstado>(json); }
            catch { yield break; }

            if (estado == null || estado.posiciones == null)
                yield break;

            for (int i = 0; i < estado.posiciones.Length; i++)
            {
                Position p = estado.posiciones[i];
                if (p == null) continue;
                if (p.tipo != "PORTA") continue;
                if (!string.IsNullOrWhiteSpace(miSessionId) && p.sessionId == miSessionId) continue;
                if (portaRemoto == null) continue;

                portaRemoto.position = new Vector3(p.x, p.y, p.z);
                portaRemoto.rotation = Quaternion.Euler(p.rx, p.ry, p.rz);
                break;
            }
        }
    }
}