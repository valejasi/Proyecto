using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Seleccionador : MonoBehaviour
{
    private GameObject objetoSeleccionado;
    public CamaraJugador camaraJugador;
    private Color colorOriginal;

    [Header("UI")]
    public Slider uiSlider;
    public TextMeshProUGUI uiTextoMunicion;

    void Start()
    {
        if (uiTextoMunicion != null) uiTextoMunicion.gameObject.SetActive(false);
        if (uiSlider != null) uiSlider.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickeado = hit.collider.gameObject;

                // Si tiene Mover pero no es mío, es del enemigo → ignorar completamente
                Mover moverCheck = clickeado.GetComponent<Mover>();
                if (moverCheck != null && !moverCheck.isMine)
                    return;

                // Deseleccionar objeto anterior
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

                objetoSeleccionado = clickeado;
                Debug.Log("Seleccioné: " + objetoSeleccionado.name);

                DronBase dron = hit.collider.GetComponentInParent<DronBase>();
                if (dron != null && camaraJugador != null)
                {
                    camaraJugador.objetivo = dron.transform;
                    camaraJugador.ActivarVistaDron(dron.EsAereo);
                     // UI: conectar al dron seleccionado
                    Municion municion = dron.GetComponent<Municion>();
                    Combustible combustible = dron.GetComponent<Combustible>();

                    if (municion != null) {
                        uiTextoMunicion.gameObject.SetActive(true);
                        municion.textoMunicion = uiTextoMunicion;
                        municion.ActualizarUI();
                    } 
                    if (combustible != null){
                        uiSlider.gameObject.SetActive(true);
                        combustible.barraCombustible = uiSlider;
                    }
                    Debug.Log("Es un dron, cambiando cámara");
                }

                Renderer r = objetoSeleccionado.GetComponent<Renderer>();
                if (r != null && dron == null)
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

        // FIJAR CON V SOLO SI ES PORTADRON
        if (Input.GetKeyDown(KeyCode.V) && objetoSeleccionado != null)
        {
            PortaDronBase porta = objetoSeleccionado.GetComponent<PortaDronBase>();
            if (porta != null)
            {
                Mover mover = objetoSeleccionado.GetComponent<Mover>();
                if (mover != null)
                {
                    mover.estaSeleccionado = false;
                    mover.enabled = false;
                }

                Rigidbody rb = objetoSeleccionado.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.isKinematic = true;

                Debug.Log("PortaDron fijado con V");
            }
        }

        if (Input.GetKeyDown(KeyCode.M) && camaraJugador != null)
            camaraJugador.VolverAMapa();
    }
}