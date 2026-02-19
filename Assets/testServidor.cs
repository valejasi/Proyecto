using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class testServidor : MonoBehaviour
{
    [Header("Servidor")]
    [SerializeField] private string baseUrl = "http://localhost:8080";
    [SerializeField] private string codigoSala = ""; // se llena solo con C

    [Header("Objetos")]
    [SerializeField] private Transform objetoQueSeMueve;
    [SerializeField] private Transform objetoRemoto;

    void Start()
    {
        Debug.Log("testServidor activo");
    }

    void Update()
    {
        // C = crear sala y guardar código
        if (Input.GetKeyDown(KeyCode.C))
            StartCoroutine(CreateGameAndStoreCode());

        // M = mandar posición a /game/move/{codigo}
        if (Input.GetKeyDown(KeyCode.M))
            StartCoroutine(SendMove());

        // S = pedir estado /game/state/{codigo} y aplicarlo al remoto
        if (Input.GetKeyDown(KeyCode.S))
            StartCoroutine(GetStateAndApply());
    }

    IEnumerator CreateGameAndStoreCode()
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

            string text = req.downloadHandler.text;
            Debug.Log("Create OK: " + text);

            // Respuesta típica: "Sala creada. Código: a74315"
            int idx = text.LastIndexOf(":");
            if (idx >= 0 && idx + 1 < text.Length)
            {
                codigoSala = text.Substring(idx + 1).Trim();
                Debug.Log("codigoSala actualizado a: " + codigoSala);
            }
            else
            {
                Debug.LogWarning("No pude extraer el código de la respuesta.");
            }
        }
    }

    IEnumerator SendMove()
    {
        if (objetoQueSeMueve == null)
        {
            Debug.LogError("No asignaste objetoQueSeMueve");
            yield break;
        }
        if (string.IsNullOrWhiteSpace(codigoSala))
        {
            Debug.LogError("codigoSala está vacío. Apretá C para crear sala primero.");
            yield break;
        }

        string url = baseUrl + "/game/move/" + codigoSala;
        Vector3 pos = objetoQueSeMueve.position;

        PositionData data = new PositionData(pos.x, pos.y, pos.z);
        string json = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        Debug.Log("POST: " + url + " | Posición: " + pos);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogError("Move ERROR: " + req.error);
            else
                Debug.Log("Move OK: " + req.downloadHandler.text);
        }
    }

    IEnumerator GetStateAndApply()
    {
        if (objetoRemoto == null)
        {
            Debug.LogError("No asignaste objetoRemoto");
            yield break;
        }
        if (string.IsNullOrWhiteSpace(codigoSala))
        {
            Debug.LogError("codigoSala está vacío. Apretá C para crear sala primero.");
            yield break;
        }

        string url = baseUrl + "/game/state/" + codigoSala;
        Debug.Log("GET: " + url);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("State ERROR: " + req.error);
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("State: " + json);

            // Si el server devolvió "Sala no existe." lo detectamos
            if (json.Contains("Sala no existe"))
            {
                Debug.LogError("El server dice que la sala no existe. Apretá C para crear una nueva.");
                yield break;
            }

            PositionData pos = ExtractFirstPosition(json);
            if (pos == null)
            {
                Debug.LogWarning("No pude leer posición del state (¿todavía no mandaste M?)");
                yield break;
            }

            objetoRemoto.position = new Vector3(pos.x, pos.y, pos.z);
            Debug.Log("Apliqué al remoto: " + objetoRemoto.position);
        }
    }

    // --- Parser simple: saca el primer x/y/z del JSON ---
    PositionData ExtractFirstPosition(string json)
    {
        float x = FindNumber(json, "\"x\":");
        float y = FindNumber(json, "\"y\":");
        float z = FindNumber(json, "\"z\":");

        if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z))
            return null;

        return new PositionData(x, y, z);
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

    [System.Serializable]
    public class PositionData
    {
        public float x, y, z;
        public PositionData(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
}



