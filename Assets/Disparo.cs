using UnityEngine;

public class Disparo : MonoBehaviour
{
    public GameObject balaPrefab;
    public Transform firePoint;
    public float fuerza = 20f;

    public bool isMine = false;
    public int objIdDisparador;

    private Municion municion;
    private Mover mover;

    void Start()
    {
        municion = GetComponent<Municion>();
        mover = GetComponent<Mover>();
    }

    void Update()
    {
        if (!isMine || mover == null || !mover.estaSeleccionado) return;
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

    void SpawnBala(Vector3 pos, Quaternion rot, Vector3 direccion)
    {
        GameObject bala = Instantiate(balaPrefab, pos, rot);
        Destroy(bala, 3f);
        Rigidbody rb = bala.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = direccion * fuerza;
    }
}