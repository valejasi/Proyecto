using UnityEngine;

public class LineaDeApunte : MonoBehaviour
{
    public LineRenderer linea;
    public Transform origen;          // el firePoint (o el cubo si no tenés)
    public float largo = 30f;
    public LayerMask capas = ~0;      // todo

    void Start()
    {
        if (linea == null) linea = GetComponent<LineRenderer>();
        linea.enabled = false;
        linea.positionCount = 2;
    }

    void Update()
    {
        bool apuntando = Input.GetMouseButton(1); // click derecho

        linea.enabled = apuntando;
        if (!apuntando) return;

        Vector3 inicio = origen != null ? origen.position : transform.position;
        Vector3 dir = origen != null ? origen.forward : transform.forward;

        Vector3 fin = inicio + dir * largo;

        // Si querés que choque con el piso/objetos y corte ahí
        if (Physics.Raycast(inicio, dir, out RaycastHit hit, largo, capas))
            fin = hit.point;

        linea.SetPosition(0, inicio);
        linea.SetPosition(1, fin);
    }
}

