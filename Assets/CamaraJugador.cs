using UnityEngine;

public class CamaraJugador : MonoBehaviour
{
    public Transform objetivo;
    public Transform cam;

    [Header("Tipo de Dron")]
    public bool esAereo = false;

    [Header("Vista Arriba - Naval")]
    public Vector3 offsetArribaNaval = new Vector3(0f, 7f, 0f);

    [Header("Vista Arriba - Aéreo")]
    public Vector3 offsetArribaAereo = new Vector3(0f, 14f, 0f);

    public Vector3 rotArribaLocal = new Vector3(90f, 0f, 0f);

    [Header("Vista Apuntar - Naval")]
    public Vector3 offsetApuntarNaval = new Vector3(5f, 2f, 0f);

    [Header("Vista Apuntar - Aéreo")]
    public Vector3 offsetApuntarAereo = new Vector3(7f, 4f, 0f);

    public Vector3 rotApuntarLocal = new Vector3(5f, -90f, 0f);

    [Header("Vista Mapa")]
    public Vector3 offsetMapa = new Vector3(0f, 150f, 0f);
    public Vector3 rotMapaLocal = new Vector3(90f, 0f, 0f);

    public float suavidadPos = 12f;
    public float suavidadRot = 12f;

    public bool vistaMapaActiva = true;

        // referencia al servidor para controlar visibilidad
    private Servidor servidor;

    void Start()
    {
        servidor = FindAnyObjectByType<Servidor>();
    }

    void LateUpdate()
    {
        if (objetivo == null || cam == null) return;

        bool apuntando = Input.GetMouseButton(1);

        Vector3 offsetArriba  = esAereo ? offsetArribaAereo  : offsetArribaNaval;
        Vector3 offsetApuntar = esAereo ? offsetApuntarAereo : offsetApuntarNaval;

        Vector3    posDeseada;
        Quaternion rotDeseada;

        if (vistaMapaActiva)
        {
            posDeseada = objetivo.position + offsetMapa;
            rotDeseada = Quaternion.Euler(rotMapaLocal);
        }
        else if (apuntando)
        {
            posDeseada = objetivo.position
                + objetivo.right   * offsetApuntar.x
                + objetivo.up      * offsetApuntar.y
                + objetivo.forward * offsetApuntar.z;

            rotDeseada = objetivo.rotation * Quaternion.Euler(rotApuntarLocal);
        }
        else
        {
            posDeseada = objetivo.position + offsetArriba;
            rotDeseada = Quaternion.Euler(rotArribaLocal);
        }

        transform.position = Vector3.Lerp(
            transform.position, posDeseada, Time.deltaTime * suavidadPos);

        cam.localRotation = Quaternion.Lerp(
            cam.localRotation, rotDeseada, Time.deltaTime * suavidadRot);
    }

    public void ActivarVistaDron(bool aereo)
    {
        esAereo = aereo;
        vistaMapaActiva = false;
         // avisar al servidor qué dron es el activo ahora
        if (servidor != null)
            servidor.ActualizarVisibilidadEnemigos(objetivo);
        Debug.Log($"Vista dron activada – {(aereo ? "aéreo" : "naval")}");
    }

    public void VolverAMapa()
    {
        vistaMapaActiva = true;
         // ocultar todo lo remoto
        if (servidor != null)
            servidor.OcultarTodosEnemigos();
        Debug.Log("Volviendo a vista mapa");
    }
}