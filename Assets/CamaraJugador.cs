using UnityEngine;

public class CamaraJugador : MonoBehaviour
{
    public Transform objetivo;   // Dron seleccionado
    public Transform cam;        // Main Camera 

    [Header("Tipo Jugador")]
    public bool esAereo = false;   // <- activar si es jugador aéreo

    [Header("Vista Arriba")]
    public Vector3 offsetArriba = new Vector3(0f, 7f, 0f);
    public Vector3 rotArribaLocal = new Vector3(90f, 0f, 0f);

    [Header("Vista Apuntar (perfil - lado derecho)")]
    public Vector3 offsetApuntar = new Vector3(5f, 2f, 0f); 
    public Vector3 rotApuntarLocal = new Vector3(5f, -90f, 0f);

    [Header("Vista Mapa")]
    public Vector3 offsetMapa = new Vector3(0f, 150f, 0f);
    public Vector3 rotMapaLocal = new Vector3(90f, 0f, 0f);

    public float suavidadPos = 12f;
    public float suavidadRot = 12f;

    private bool vistaMapaActiva = true;
    private bool vistaDron = false;

    void LateUpdate()
    {
        if (objetivo == null || cam == null) return;

        bool apuntando = Input.GetMouseButton(1);

        Vector3 posDeseada;
        Quaternion rotDeseada;

        //  factor 50% más si es aéreo
        float factor = esAereo ? 1.5f : 1f;

        if (vistaMapaActiva)
        {
            posDeseada = objetivo.position + offsetMapa * factor;
            rotDeseada = Quaternion.Euler(rotMapaLocal);
        }
        else if (apuntando)
        {
            posDeseada = objetivo.position
                + objetivo.right * (offsetApuntar.x * factor)
                + objetivo.up * (offsetApuntar.y * factor)
                + objetivo.forward * (offsetApuntar.z * factor);

            rotDeseada = objetivo.rotation * Quaternion.Euler(rotApuntarLocal);
        }
        else
        {
            posDeseada = objetivo.position + offsetArriba * factor;
            rotDeseada = Quaternion.Euler(rotArribaLocal);
        }

        transform.position = Vector3.Lerp(
            transform.position,
            posDeseada,
            Time.deltaTime * suavidadPos
        );

        cam.localRotation = Quaternion.Lerp(
            cam.localRotation,
            rotDeseada,
            Time.deltaTime * suavidadRot
        );
    }

    public void ActivarVistaDron()
    {
        vistaMapaActiva = false;
        vistaDron = true;
        Debug.Log("Cambie de camara");
    }
}