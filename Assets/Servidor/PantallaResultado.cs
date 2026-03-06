using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PantallaResultado : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject panelResultado;
    [SerializeField] private TMP_Text textoResultado;
    [SerializeField] private TMP_Text textoCuenta;

    private string miSessionId => FindAnyObjectByType<Servidor>()?.GetSessionId();

    void Start()
    {
        panelResultado.SetActive(false);
    }

    public void ActualizarEstado(Servidor.ResultadoData resultado, string miSlot)
    {
        if (resultado == null) return;

        switch (resultado.estado)
        {
            case "JUGANDO":
                panelResultado.SetActive(false);
                textoCuenta.text = "";
                break;

            case "CUENTA_REGRESIVA":
                panelResultado.SetActive(false);
                textoCuenta.text = "⚠️ Portadron destruido! " + resultado.segundosRestantes + "s";
                break;

            case "VICTORIA_HOST":
                panelResultado.SetActive(true);
                textoResultado.text = (miSlot == "1") ? "¡VICTORIA!" : "DERROTA";
                textoCuenta.text = "";
                break;

            case "VICTORIA_JOIN":
                panelResultado.SetActive(true);
                textoResultado.text = (miSlot == "2") ? "¡VICTORIA!" : "DERROTA";
                textoCuenta.text = "";
                break;

            case "EMPATE":
                panelResultado.SetActive(true);
                textoResultado.text = "EMPATE";
                textoCuenta.text = "";
                break;
        }
    }
}