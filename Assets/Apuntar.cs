using UnityEngine;

public class Apuntar : MonoBehaviour
{
    public Transform aimPivot;

    public bool bloquearCursor = true; // <-- cambiá esto en el Inspector

    public float sensibilidadYaw = 180f;
    public float sensibilidadPitch = 120f;
    public float minPitch = -45f;
    public float maxPitch = 45f;

    public float pitchActual = 0f;

    void Update()
    {
        bool apuntando = Input.GetMouseButton(1);

        if (bloquearCursor)
        {
            if (Input.GetMouseButtonDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (Input.GetMouseButtonUp(1))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        if (!apuntando) return;  //mhmmmmmm
        if (aimPivot == null) return;

        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        transform.Rotate(0f, mx * sensibilidadYaw * Time.deltaTime, 0f);

        pitchActual -= my * sensibilidadPitch * Time.deltaTime;
        pitchActual = Mathf.Clamp(pitchActual, minPitch, maxPitch);

        aimPivot.localRotation = Quaternion.Euler(pitchActual, 0f, 0f);
    }
}











