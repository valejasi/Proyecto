using UnityEngine;

public class Disparo : MonoBehaviour
{
    public GameObject balaPrefab;
    public Transform firePoint;
    public float fuerza = 20f;

    private Municion municion;

    void Start()
    {
        municion = GetComponent<Municion>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Si hay componente Municion, chequea que tenga
            if (municion != null && !municion.TieneMunicion())
                return;

            Disparar();

            if (municion != null)
                municion.GastarUnaBala();
        }
    }

    void Disparar()
    {
        GameObject bala = Instantiate(balaPrefab, firePoint.position, firePoint.rotation);
        Destroy(bala, 3f);

        Rigidbody rb = bala.GetComponent<Rigidbody>();
        rb.linearVelocity = firePoint.forward * fuerza;
    }
}



