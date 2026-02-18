using UnityEngine;
using UnityEngine.UI;

public class Combustible : MonoBehaviour
{
    [Header("Combustible")]
    public float combustibleMaximo = 100f;
    public float combustibleActual = 100f;

    [Header("Consumo")]
    public float consumoPorMetro = 25f; // cuánto gasta por cada 1 metro recorrido

    [Header("Recarga")]
    public float demoraParaRecargar = 2f;     // segundos quieto antes de recargar
    public float recargaPorSegundo = 20f;      // cuánto recarga por segundo

    [Header("UI (opcional)")]
    public Slider barraCombustible;

    private Vector3 ultimaPosicion;
    private float tiempoQuieto = 0f;

    void Start()
    {
        combustibleActual = Mathf.Clamp(combustibleActual, 0f, combustibleMaximo);
        ultimaPosicion = transform.position;
        ActualizarUI();
    }

    void FixedUpdate()
    {
        float distancia = Vector3.Distance(transform.position, ultimaPosicion);
        ultimaPosicion = transform.position;

        bool seMovio = distancia > 0.0005f;

        if (seMovio)
        {
            combustibleActual -= distancia * consumoPorMetro;
            if (combustibleActual < 0f) combustibleActual = 0f;

            tiempoQuieto = 0f;
        }
        else
        {
            tiempoQuieto += Time.fixedDeltaTime;

            if (tiempoQuieto >= demoraParaRecargar && combustibleActual < combustibleMaximo)
            {
                combustibleActual += recargaPorSegundo * Time.fixedDeltaTime;
                if (combustibleActual > combustibleMaximo) combustibleActual = combustibleMaximo;
            }
        }

        ActualizarUI();
    }

    public bool TieneCombustible()
    {
        return combustibleActual > 0.01f;
    }

    private void ActualizarUI()
    {
        if (barraCombustible != null)
        {
            barraCombustible.maxValue = combustibleMaximo;
            barraCombustible.value = combustibleActual;
        }
    }
}

