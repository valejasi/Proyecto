using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class LobbyManager : MonoBehaviour
{
    public TextMeshProUGUI textoEstado;
    public TextMeshProUGUI jugador1Texto;
    public TextMeshProUGUI jugador2Texto;
    public GameObject startButton;

    private int jugadoresConectados = 0;

    void Start()
    {
        startButton.SetActive(false);
        textoEstado.text = "Esperando jugadores...";

        // Simulación para probar
        Invoke("JugadorConectado", 3f);
        Invoke("JugadorConectado", 5f);
    }

    public void JugadorConectado()
    {
        jugadoresConectados++;

        if (jugadoresConectados == 1)
        {
            jugador1Texto.text = "Jugador 1: Conectado";
            textoEstado.text = "Esperando segundo jugador...";
        }
        else if (jugadoresConectados == 2)
        {
            jugador2Texto.text = "Jugador 2: Conectado";
            StartCoroutine(ContadorInicio());
        }
    }

    IEnumerator ContadorInicio()
    {
        for (int i = 3; i > 0; i--)
        {
            textoEstado.text = "Iniciando en " + i + "...";
            textoEstado.fontSize = 80;
            yield return new WaitForSeconds(0.3f);

            textoEstado.fontSize = 60;
            yield return new WaitForSeconds(0.7f);
        }

        textoEstado.text = "¡Comienza la partida!";
        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("SampleScene");
    }
}