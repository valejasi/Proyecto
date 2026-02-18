using UnityEngine;

public class Mover : MonoBehaviour
{
    public float velocidadHorizontal = 5f;
    public float velocidadVertical = 5f; // para volar

    private Rigidbody rb;
    private Combustible combustible;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Para volar
        rb.useGravity = false;

        // Agarramos el componente de combustible si existe
        combustible = GetComponent<Combustible>();
    }

    void FixedUpdate()
    {
        // Si hay sistema de combustible y se quedó sin, se frena
        if (combustible != null && !combustible.TieneCombustible())
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        float x = Input.GetAxisRaw("Horizontal"); // A/D
        float z = Input.GetAxisRaw("Vertical");   // W/S

        float y = 0f;
        if (Input.GetKey(KeyCode.Space)) y = 1f;        // subir
        if (Input.GetKey(KeyCode.LeftShift)) y = -1f;   // bajar

        Vector3 velocidad = new Vector3(
            x * velocidadHorizontal,
            y * velocidadVertical,
            z * velocidadHorizontal
        );

        rb.linearVelocity = velocidad;
    }
}


