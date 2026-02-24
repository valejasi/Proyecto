using UnityEngine;


public class CamaraJugador : MonoBehaviour
{
    public Transform objetivo;   // Cube

    //public bool modoColocacion = true;
    public Transform cam;        // Main Camera (hija del CameraRig)

    [Header("Vista Arriba")]
    public Vector3 offsetArriba = new Vector3(0f, 7f, 0f);
    public Vector3 rotArribaLocal = new Vector3(90f, 0f, 0f);

    [Header("Vista Apuntar (perfil)")]
    public Vector3 offsetApuntar = new Vector3(5f, 0f, 0f);
    public Vector3 rotApuntarLocal = new Vector3(5f, -90f, 0f);


    [Header("Vista Mapa")]
    public Vector3 offsetMapa = new Vector3(0f, 40f, 0f); //camara sigue arriba del jugador, 
                                                          //hay q definir el tamanio del mapa y centrar esto
    public Vector3 rotMapaLocal = new Vector3(90f, 0f, 0f);

    public float suavidadPos = 12f;
    public float suavidadRot = 12f;

    private bool vistaMapaActiva = false;
    
    //  ESTE REEMPLAZARIA EL Q YA ESTA
     void LateUpdate()
    {
        if (objetivo == null || cam == null) return;

        bool apuntando = Input.GetMouseButton(1);

        Vector3 offset;
        Vector3 rot;

        //TIENE Q SER IF ELSE PQ SON 3 OPCIONES
        if (vistaMapaActiva)
        {
            offset = offsetMapa;
            rot = rotMapaLocal;
        } 
        else if (apuntando)
        {
            offset = offsetApuntar;
            rot = rotApuntarLocal;
        }
        else
        {
            offset = offsetArriba;
            rot = rotArribaLocal;
        }

        Vector3 posDeseada = objetivo.position + offset;
        transform.position = Vector3.Lerp(transform.position, posDeseada, Time.deltaTime * suavidadPos);

        Quaternion rotDeseada = Quaternion.Euler(rot);
        cam.localRotation = Quaternion.Lerp(cam.localRotation, rotDeseada, Time.deltaTime * suavidadRot);
    }
    
    bool VistaGeneral()
    {
        return vistaMapaActiva;
    }
}









