using UnityEngine;

public class Mover : MonoBehaviour
{
    public float velocidadHorizontal = 5f;
    public float velocidadVertical = 5f; // para volar
    public bool estaSeleccionado = false;

    [Header("Multiplayer")]
    public bool isMine = true; // el NetworkManager lo setea

    private Rigidbody rb;
    private Combustible combustible;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        combustible = GetComponent<Combustible>();

        if (rb == null)
        {
            Debug.LogError($"[{name}] No tiene Rigidbody. Agregalo al prefab del dron.");
            enabled = false;
            return;
        }

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // evitar que se caiga o gire
    }

    void FixedUpdate()
    {
        if (!isMine) return;

        if (!estaSeleccionado) 
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (combustible != null && !combustible.TieneCombustible())
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        float y = 0f;
        if (Input.GetKey(KeyCode.Space)) y = 1f;
        if (Input.GetKey(KeyCode.LeftShift)) y = -1f;

        Vector3 velocidad = new Vector3(
            x * velocidadHorizontal,
            y * velocidadVertical,
            z * velocidadHorizontal
        );

        rb.linearVelocity = velocidad;
    }
}