using UnityEngine;

public class Mover : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadMovimiento = 10f;
    public float velocidadVertical = 6f;
    public float sensibilidadMouse = 120f;
    public float suavizadoMovimiento = 8f;

    public bool estaSeleccionado = false;

    [Header("Multiplayer")]
    public bool isMine = true;

    private Rigidbody rb;
    private Combustible combustible;

    // Inputs guardados (se leen en Update)
    private float forwardInput;
    private float strafeInput;
    private float verticalInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        combustible = GetComponent<Combustible>();

        if (rb == null)
        {
            Debug.LogError($"[{name}] No tiene Rigidbody.");
            enabled = false;
            return;
        }

        rb.useGravity = false;

        // Solo rota en Y (estable tipo dron)
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        // MUY IMPORTANTE para suavidad visual
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        if (!isMine || !estaSeleccionado) return;

        // ===== ROTACIÓN CON MOUSE =====
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadMouse * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // ===== INPUT MOVIMIENTO =====
        forwardInput = Input.GetAxis("Vertical");     // W / S
        strafeInput = Input.GetAxis("Horizontal");    // A / D

        verticalInput = 0f;
        if (Input.GetKey(KeyCode.R)) verticalInput = 1f;
        if (Input.GetKey(KeyCode.F)) verticalInput = -1f;
    }

    void FixedUpdate()
    {
        if (!isMine) return;

        if (!estaSeleccionado)
        {
            rb.linearVelocity  = Vector3.zero;
            return;
        }

        if (combustible != null && !combustible.TieneCombustible())
        {
            rb.linearVelocity  = Vector3.zero;
            return;
        }

        // Dirección relativa al dron
        Vector3 direccion =
            transform.forward * forwardInput +
            transform.right * strafeInput +
            Vector3.up * verticalInput;

        Vector3 velocidadDeseada = direccion * velocidadMovimiento;

        // Suavizado (aceleración progresiva)
        rb.linearVelocity  = Vector3.Lerp(
            rb.linearVelocity ,
            velocidadDeseada,
            Time.fixedDeltaTime * suavizadoMovimiento
        );
    }
}