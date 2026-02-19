using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class testServidor : MonoBehaviour
{
    [Header("Servidor")]
    [SerializeField] private string baseUrl = "http://localhost:8080";

    [Header("Objetos")]
    [SerializeField] private Transform objetoLocal;   // tu cubo que controlás
    [SerializeField] private Transform objetoRemoto;  // cubo del otro

    [Header("Sync")]
    [SerializeField] private float intervalo = 0.1f;

    private string codigoSala = "";
    private string miSessionId = "";
    private string codigoIngresado = ""; // lo que tipeás para join
    private Coroutine loopSync;

    void Start()
    {
        Debug.Log("testServidor activo");
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

            // Solo letras/números (códigos tipo ad4768)
            if (char.IsLetterOrDigit(ch) && codigoIngresado.Length < 6)
            {
                codigoIngresado += char.ToLower(ch);
            }
        }
    }

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

            // Si sala no existe, jugadores viene 0 (según tu controller)
            if (resp.jugadores == 0)
            {
                Debug.LogError("No se pudo unir: sala no existe o código incorrecto.");
                yield break;
            }

            codigoSala = resp.codigo;
            miSessionId = resp.sessionId;

            Debug.Log($"UNIDO. codigoSala={codigoSala} miSessionId={miSessionId} jugadores={resp.jugadores}");
        }
    }

    IEnumerator SyncLoop()
    {
        Debug.Log("Sync iniciado (P para detener).");

        while (true)
        {
            yield return SendMove();
            yield return GetStateAndApplyOther();
            yield return new WaitForSeconds(intervalo);
        }
    }

    IEnumerator SendMove()
    {
        if (objetoLocal == null) yield break;
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;

        string url = baseUrl + "/game/move/" + codigoSala;

        Vector3 pos = objetoLocal.position;
        PositionData data = new PositionData(pos.x, pos.y, pos.z);

        string json = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();
        }
    }

    IEnumerator GetStateAndApplyOther()
    {
        if (objetoRemoto == null) yield break;
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;

        string url = baseUrl + "/game/state/" + codigoSala;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;

            string json = req.downloadHandler.text;
            PositionData other = ExtractOtherPlayerPosition(json, miSessionId);
            if (other == null) yield break;

            objetoRemoto.position = new Vector3(other.x, other.y, other.z);
        }
    }

    PositionData ExtractOtherPlayerPosition(string json, string myId)
    {
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

            if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z)) return null;
            return new PositionData(x, y, z);
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

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 600, 25), "Host: C = Create sala | Sync: P");
        GUI.Label(new Rect(10, 35, 600, 25), "Cliente: escribí el código (6 chars) y Enter = Join");
        GUI.Label(new Rect(10, 60, 600, 25), "Código tipeado: " + codigoIngresado);

        GUI.Label(new Rect(10, 90, 600, 25), "Mi sala actual: " + (string.IsNullOrWhiteSpace(codigoSala) ? "(ninguna)" : codigoSala));
        GUI.Label(new Rect(10, 115, 800, 25), "Mi sessionId: " + (string.IsNullOrWhiteSpace(miSessionId) ? "(sin asignar)" : miSessionId));
    }

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
        public float x, y, z;
        public PositionData(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
}





