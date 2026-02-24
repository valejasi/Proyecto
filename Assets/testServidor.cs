using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class testServidor : MonoBehaviour
{
    [Header("Servidor")]
    [SerializeField] private string baseUrl = "https://proyecto-y1ud.onrender.com";

    [Header("Prefab jugador")]
    [SerializeField] private GameObject playerCubePrefab;
    [SerializeField] private Transform spawnOrigin;

    [Header("Sync")]
    [SerializeField] private float intervalo = 0.1f;

    [Header("Smooth remoto")]
    [SerializeField] private float smoothPos = 12f;
    [SerializeField] private float smoothRot = 12f;

    // Sala / sesión
    private string codigoSala = "";
    private string miSessionId = "";
    private string codigoIngresado = "";
    private Coroutine loopSync;

    // sessionId -> objeto en escena
    private readonly Dictionary<string, NetPlayer> players = new Dictionary<string, NetPlayer>();

    // ====== DATA STRUCTS ======
    private class NetPlayer
    {
        public string sessionId;
        public Transform tf;
        public bool isMine;

        public Vector3 targetPos;
        public Quaternion targetRot;
        public bool hasTarget;
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
        public string sessionId; // IMPORTANTE: para que el server sepa quién manda (si lo soporta)
        public float x, y, z;
        public float qx, qy, qz, qw;

        public PositionData() {}

        public PositionData(string sid, Vector3 p, Quaternion q)
        {
            sessionId = sid;
            x = p.x; y = p.y; z = p.z;
            qx = q.x; qy = q.y; qz = q.z; qw = q.w;
        }
    }

    // ====== UNITY ======
    void Start()
    {
        if (spawnOrigin == null) spawnOrigin = this.transform;
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

        // Smooth remotos
        foreach (var kv in players)
        {
            NetPlayer p = kv.Value;
            if (p == null || p.tf == null) continue;
            if (p.isMine) continue; // mi cubo no se “lerpea” desde server
            if (!p.hasTarget) continue;

            p.tf.position = Vector3.Lerp(
                p.tf.position,
                p.targetPos,
                1f - Mathf.Exp(-smoothPos * Time.deltaTime)
            );

            p.tf.rotation = Quaternion.Slerp(
                p.tf.rotation,
                p.targetRot,
                1f - Mathf.Exp(-smoothRot * Time.deltaTime)
            );
        }
    }

    // ====== INPUT JOIN ======
    void CapturarCodigoJoin()
    {
        foreach (char ch in Input.inputString)
        {
            if (ch == '\n' || ch == '\r')
            {
                if (!string.IsNullOrWhiteSpace(codigoIngresado))
                {
                    codigoSala = codigoIngresado.Trim().ToLower();
                    StartCoroutine(JoinAndStore(codigoSala));
                }
                continue;
            }

            if (ch == '\b')
            {
                if (codigoIngresado.Length > 0)
                    codigoIngresado = codigoIngresado.Substring(0, codigoIngresado.Length - 1);
                continue;
            }

            if (char.IsLetterOrDigit(ch) && codigoIngresado.Length < 6)
                codigoIngresado += char.ToLower(ch);
        }
    }

    // ====== ROOM ======
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

            EnsurePlayerExists(miSessionId, isMine: true);

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

            EnsurePlayerExists(miSessionId, isMine: true);

            Debug.Log($"UNIDO. codigoSala={codigoSala} miSessionId={miSessionId} jugadores={resp.jugadores}");
        }
    }

    // ====== SYNC ======
    IEnumerator SyncLoop()
    {
        Debug.Log("Sync iniciado (P para detener).");

        while (true)
        {
            yield return SendMove();
            yield return GetStateAndApplyAll();
            yield return new WaitForSeconds(intervalo);
        }
    }

    IEnumerator SendMove()
    {
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;

        NetPlayer me = EnsurePlayerExists(miSessionId, isMine: true);
        if (me.tf == null) yield break;

        string url = baseUrl + "/game/move/" + codigoSala;

        var data = new PositionData(miSessionId, me.tf.position, me.tf.rotation);
        string json = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

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

    IEnumerator GetStateAndApplyAll()
    {
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;

        string url = baseUrl + "/game/state/" + codigoSala;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;

            string json = req.downloadHandler.text;

            // Parse “a mano”: { "session1": {x..}, "session2": {x..} }
            var dict = ExtractAllPlayerPositions(json);
            if (dict == null) yield break;

            foreach (var kv in dict)
            {
                string sid = kv.Key;
                PositionData pd = kv.Value;

                NetPlayer p = EnsurePlayerExists(sid, isMine: sid == miSessionId);

                if (!p.isMine)
                {
                    p.targetPos = new Vector3(pd.x, pd.y, pd.z);
                    p.targetRot = new Quaternion(pd.qx, pd.qy, pd.qz, pd.qw);
                    p.hasTarget = true;
                }
                // Si es el mío, no lo piso (predicción local).
            }
        }
    }

    // ====== SPAWN / REGISTRY ======
    NetPlayer EnsurePlayerExists(string sessionId, bool isMine)
    {
        if (players.TryGetValue(sessionId, out var existing) && existing != null && existing.tf != null)
        {
            existing.isMine = isMine; // por si cambia
            return existing;
        }

        if (playerCubePrefab == null)
        {
            Debug.LogError("Asigná playerCubePrefab en el Inspector.");
            return new NetPlayer { sessionId = sessionId, isMine = isMine, tf = null };
        }

        Vector3 spawnPos = spawnOrigin != null ? spawnOrigin.position : Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        // Para que no spawneen encima, separo un poco por hash
        float offset = (Mathf.Abs(sessionId.GetHashCode()) % 10) * 1.5f;
        spawnPos += new Vector3(offset, 0f, 0f);

        GameObject go = Instantiate(playerCubePrefab, spawnPos, spawnRot);
        go.name = isMine ? $"Player(ME)_{sessionId}" : $"Player_{sessionId}";

        var p = new NetPlayer
        {
            sessionId = sessionId,
            tf = go.transform,
            isMine = isMine,
            targetPos = go.transform.position,
            targetRot = go.transform.rotation,
            hasTarget = false
        };

        players[sessionId] = p;
        return p;
    }

    // ====== PARSER SIMPLE ======
    Dictionary<string, PositionData> ExtractAllPlayerPositions(string json)
    {
        // Espera algo tipo:
        // { "sid1": {"x":..,"y":..,"z":..,"qx":..,"qy":..,"qz":..,"qw":..}, "sid2": {...} }
        if (!json.Contains("\"x\":")) return null;

        var result = new Dictionary<string, PositionData>();

        int idx = 0;
        while (true)
        {
            int keyStart = json.IndexOf("\"", idx);
            if (keyStart < 0) break;

            int keyEnd = json.IndexOf("\"", keyStart + 1);
            if (keyEnd < 0) break;

            string sessionId = json.Substring(keyStart + 1, keyEnd - keyStart - 1);

            int objStart = json.IndexOf("{", keyEnd);
            int objEnd = json.IndexOf("}", objStart);
            if (objStart < 0 || objEnd < 0) break;

            string obj = json.Substring(objStart, objEnd - objStart + 1);

            float x  = FindNumber(obj, "\"x\":");
            float y  = FindNumber(obj, "\"y\":");
            float z  = FindNumber(obj, "\"z\":");
            float qx = FindNumber(obj, "\"qx\":");
            float qy = FindNumber(obj, "\"qy\":");
            float qz = FindNumber(obj, "\"qz\":");
            float qw = FindNumber(obj, "\"qw\":");

            if (!(float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z) ||
                  float.IsNaN(qx) || float.IsNaN(qy) || float.IsNaN(qz) || float.IsNaN(qw)))
            {
                result[sessionId] = new PositionData
                {
                    sessionId = sessionId,
                    x = x, y = y, z = z,
                    qx = qx, qy = qy, qz = qz, qw = qw
                };
            }

            idx = objEnd + 1;
        }

        return result;
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
        GUI.Label(new Rect(10, 35, 700, 25), "Cliente: escribí el código (6 chars) y Enter = Join");
        GUI.Label(new Rect(10, 60, 600, 25), "Código tipeado: " + codigoIngresado);

        GUI.Label(new Rect(10, 90, 600, 25), "Mi sala actual: " + (string.IsNullOrWhiteSpace(codigoSala) ? "(ninguna)" : codigoSala));
        GUI.Label(new Rect(10, 115, 900, 25), "Mi sessionId: " + (string.IsNullOrWhiteSpace(miSessionId) ? "(sin asignar)" : miSessionId));
        GUI.Label(new Rect(10, 140, 900, 25), "Players en escena: " + players.Count);
    }
}