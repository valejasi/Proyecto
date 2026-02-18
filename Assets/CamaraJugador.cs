using UnityEngine;

public class CamaraJugador : MonoBehaviour
{
    public Transform objetivo;   // Cube
    public Transform cam;        // Main Camera (hija del CameraRig)

    [Header("Vista Arriba")]
    public Vector3 offsetArriba = new Vector3(0f, 7f, 0f);
    public Vector3 rotArribaLocal = new Vector3(90f, 0f, 0f);

    [Header("Vista Apuntar (perfil)")]
    public Vector3 offsetApuntar = new Vector3(5f, 0f, 0f);
    public Vector3 rotApuntarLocal = new Vector3(5f, -90f, 0f);

    public float suavidadPos = 12f;
    public float suavidadRot = 12f;

    void LateUpdate()
    {
        if (objetivo == null || cam == null) return;

        bool apuntando = Input.GetMouseButton(1);

        // Mover el RIG (CameraRig)
        Vector3 offset = apuntando ? offsetApuntar : offsetArriba;
        Vector3 posDeseada = objetivo.position + offset;
        transform.position = Vector3.Lerp(transform.position, posDeseada, Time.deltaTime * suavidadPos);

        // Rotar SOLO la cámara localmente (estable, no cambia al moverse)
        Vector3 rot = apuntando ? rotApuntarLocal : rotArribaLocal;
        Quaternion rotDeseada = Quaternion.Euler(rot);
        cam.localRotation = Quaternion.Lerp(cam.localRotation, rotDeseada, Time.deltaTime * suavidadRot);
    }
}









