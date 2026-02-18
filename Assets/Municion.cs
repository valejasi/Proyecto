using UnityEngine;
using TMPro;

public class Municion : MonoBehaviour
{
    [Header("Munición")]
    public int municionMaxima = 5;
    public int municionActual = 5;

    [Header("UI (opcional)")]
    public TMP_Text textoMunicion;

    void Start()
    {
        municionActual = Mathf.Clamp(municionActual, 0, municionMaxima);
        ActualizarUI();
    }

    public bool TieneMunicion()
    {
        return municionActual > 0;
    }

    public void GastarUnaBala()
    {
        if (municionActual <= 0) return;
        municionActual--;
        ActualizarUI();
    }

    public void RecargarCompleto()
    {
        municionActual = municionMaxima;
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        if (textoMunicion != null)
        {
            textoMunicion.text = $"Munición: {municionActual} / {municionMaxima}";
        }
    }
}


