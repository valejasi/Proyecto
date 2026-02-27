using UnityEngine;

public class Seleccionador : MonoBehaviour
{
    private GameObject objetoSeleccionado;
    private Color colorOriginal;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (objetoSeleccionado != null)
                {
                    Renderer rPrev = objetoSeleccionado.GetComponent<Renderer>();
                    if (rPrev != null)
                        rPrev.material.color = colorOriginal;

                    Mover moverPrev = objetoSeleccionado.GetComponent<Mover>();
                    if (moverPrev != null)
                        moverPrev.estaSeleccionado = false;

                    PortaDronBase portaPrev = objetoSeleccionado.GetComponent<PortaDronBase>();
                    if (portaPrev != null)
                        portaPrev.estaSeleccionado = false;
                }

                objetoSeleccionado = hit.collider.gameObject;

                Renderer r = objetoSeleccionado.GetComponent<Renderer>();
                if (r != null)
                {
                    colorOriginal = r.material.color;
                    r.material.color = Color.yellow;
                }

                Mover mover = objetoSeleccionado.GetComponent<Mover>();
                if (mover != null)
                    mover.estaSeleccionado = true;

                PortaDronBase porta = objetoSeleccionado.GetComponent<PortaDronBase>();
                if (porta != null)
                    porta.estaSeleccionado = true;
            }
        }

        // FIJAR CON C SOLO SI ES PORTADRON
        if (Input.GetKeyDown(KeyCode.C) && objetoSeleccionado != null)
        {
            PortaDronBase porta = objetoSeleccionado.GetComponent<PortaDronBase>();

            if (porta != null) // 👈 solo entra si es Naval o Aéreo
            {
                Mover mover = objetoSeleccionado.GetComponent<Mover>();
                if (mover != null)
                {
                    mover.estaSeleccionado = false;
                    mover.enabled = false;
                }

                Rigidbody rb = objetoSeleccionado.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }

                Debug.Log("PortaDron fijado con C");
            }
        }
    }
}