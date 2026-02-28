using UnityEngine;

public class CamaraJugador : MonoBehaviour
{
    public Transform objetivo;   // Dron seleccionado
    public Transform cam;        // Main Camera 

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

        if (vistaMapaActiva)
        {
            // Vista global tipo mapa
            posDeseada = objetivo.position + offsetMapa;
            rotDeseada = Quaternion.Euler(rotMapaLocal);
        }
        else if (apuntando)
        {
            // Vista perfil SIEMPRE del lado derecho del dron
            posDeseada = objetivo.position
                + objetivo.right * offsetApuntar.x
                + objetivo.up * offsetApuntar.y
                + objetivo.forward * offsetApuntar.z;

            rotDeseada = objetivo.rotation * Quaternion.Euler(rotApuntarLocal);
        }
        else
        {
            // Vista superior normal
            posDeseada = objetivo.position + offsetArriba;
            rotDeseada = Quaternion.Euler(rotArribaLocal);
        }

        //muevo la camara
        transform.position = Vector3.Lerp(
            transform.position,
            posDeseada,
            Time.deltaTime * suavidadPos
        );

        //acomodo la rotacion de la camara
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