using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Disparo : MonoBehaviour
{
    public GameObject balaPrefab;
    public Transform firePoint;
    public float fuerza = 20f;

    // Llenar desde el Servidor (igual que isMine en Mover)
    public bool isMine = true;
    public string sessionId;
    public string codigoSala;
    public string baseUrl = "https://proyecto-y1ud.onrender.com";
    public int objIdDisparador; // el id del dron que dispara

    private Municion municion;
    private Mover mover;

    void Start()
    {
        municion = GetComponent<Municion>();
        mover = GetComponent<Mover>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            Debug.Log($"[Disparo] clic. isMine={isMine} seleccionado={mover?.estaSeleccionado}");

        if (!isMine || mover == null || !mover.estaSeleccionado) return;

        if (municion != null && !municion.TieneMunicion()) return;

        if (Input.GetMouseButtonDown(0))
        {
            SpawnBala(firePoint.position, firePoint.rotation, firePoint.forward);
            StartCoroutine(EnviarDisparo());
            if (municion != null) municion.GastarUnaBala();
        }
    }

    void SpawnBala(Vector3 pos, Quaternion rot, Vector3 direccion)
    {
        GameObject bala = Instantiate(balaPrefab, pos, rot);
        Destroy(bala, 3f);
        Rigidbody rb = bala.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = direccion * fuerza;
    }

    IEnumerator EnviarDisparo()
    {
        string url = baseUrl + "/game/disparar/" + codigoSala;

        // Armar el JSON manualmente (sin Newtonsoft)
        Vector3 pos = firePoint.position;
        Vector3 dir = firePoint.forward;

        string json = $@"{{
            ""sessionId"": ""{sessionId}"",
            ""objIdDisparador"": {objIdDisparador},
            ""x"": {pos.x}, ""y"": {pos.y}, ""z"": {pos.z},
            ""dx"": {dir.x}, ""dy"": {dir.y}, ""dz"": {dir.z},
            ""velocidad"": {fuerza},
            ""rangoMax"": 30,
            ""danio"": 1
        }}";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogError("Disparo ERROR: " + req.error);
        }
    }
}