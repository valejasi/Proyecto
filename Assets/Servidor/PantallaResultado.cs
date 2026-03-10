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

        Debug.Log($"ActualizarEstado: estado={resultado.estado} miSlot={miSlot} slotSinPorta={resultado.slotSinPorta}");

        switch (resultado.estado)
        {
            case "JUGANDO":
                panelResultado.SetActive(false);
                if (textoCuenta != null) textoCuenta.text = "";
                break;

            case "CUENTA_REGRESIVA":
                panelResultado.SetActive(false);
                if (textoCuenta != null)
                {
                    textoCuenta.gameObject.SetActive(true);
                    if (int.Parse(miSlot) == resultado.slotSinPorta)
                        textoCuenta.text = "Tu portadron fue destruido! " + resultado.segundosRestantes + "s";
                    else
                        textoCuenta.text = "Portadron enemigo destruido! " + resultado.segundosRestantes + "s";
                }
                break;

            case "VICTORIA_HOST":
                panelResultado.SetActive(true);
                if (textoCuenta != null) textoCuenta.text = "";
                if (textoResultado != null){
                    textoResultado.text = (miSlot == "1") ? "¡VICTORIA!" : "DERROTA";
                    textoResultado.color = (miSlot == "1") ? Color.green : Color.red;
                }
                break;

            case "VICTORIA_JOIN":
                panelResultado.SetActive(true);
                if (textoCuenta != null) textoCuenta.text = "";
                if (textoResultado != null){
                    textoResultado.text = (miSlot == "2") ? "¡VICTORIA!" : "DERROTA";
                    textoResultado.color = (miSlot == "2") ? Color.green : Color.red;
                }
                break;

            case "EMPATE":
                panelResultado.SetActive(true);
                if (textoCuenta != null) textoCuenta.text = "";
                if (textoResultado != null){
                    textoResultado.text = "EMPATE";
                    textoResultado.color = Color.yellow;
                }
                break;
        }
    }
}