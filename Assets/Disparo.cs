using UnityEngine;

public class Disparo : MonoBehaviour
{
    public GameObject balaPrefab;
    public Transform firePoint;
    public float fuerza = 20f;

    public bool isMine = false;
    public int objIdDisparador;

    [Header("Recarga automática")]
    public float distanciaRecarga = 5f;
    public float intervaloChequeoRecarga = 1f;

    private Municion municion;
    private Mover mover;
    private float timerRecarga = 0f;

    void Start()
    {
        municion = GetComponent<Municion>();
        mover = GetComponent<Mover>();
    }

    void Update()
    {
        if (!isMine) return;

        ChequearRecargaPorProximidad();

        if (mover == null || !mover.estaSeleccionado) return;
        if (municion != null && !municion.TieneMunicion()) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnBala(firePoint.position, firePoint.rotation, firePoint.forward);
            if (municion != null) municion.GastarUnaBala();

            Servidor srv = FindAnyObjectByType<Servidor>();
            if (srv != null)
                srv.StartCoroutine(srv.DispararDesdeServidor(objIdDisparador, firePoint.position, firePoint.forward, fuerza));
        }
    }

    void ChequearRecargaPorProximidad()
    {
        timerRecarga -= Time.deltaTime;
        if (timerRecarga > 0f) return;
        timerRecarga = intervaloChequeoRecarga;

        if (municion == null || municion.municionActual >= municion.municionMaxima) return;

        Servidor srv = FindAnyObjectByType<Servidor>();
        if (srv == null) return;

        Transform miPorta = srv.GetMiPorta();
        if (miPorta == null) return;

        float dist = Vector3.Distance(transform.position, miPorta.position);
        if (dist <= distanciaRecarga)
        {
            srv.StartCoroutine(srv.Recargar(objIdDisparador));
            municion.RecargarCompleto(); // feedback visual inmediato
        }
    }

    void SpawnBala(Vector3 pos, Quaternion rot, Vector3 direccion)
    {
        GameObject bala = Instantiate(balaPrefab, pos, rot);
        Destroy(bala, 3f);
        Rigidbody rb = bala.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = direccion * fuerza;
    }
}