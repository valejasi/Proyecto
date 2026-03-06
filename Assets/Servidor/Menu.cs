using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class Menu : Servidor
{
    public TMP_InputField ingresoCodigo;
    public TextMeshProUGUI textoError;
    private string codigo;

    public void CrearPartida()
    {
        StartCoroutine(CrearPartidaCR());
    }

    IEnumerator CrearPartidaCR()
    {
        yield return StartCoroutine(CreateAndStore());
        if (!string.IsNullOrEmpty(codigoSala))
        {
            SceneManager.LoadScene("SalaEspera");
        }
        else
            textoError.text = "No se pudo crear la sala";
    }

    public void JoinPartida()
    {
        StartCoroutine(JoinPartidaCR());
    }

    IEnumerator JoinPartidaCR()
    {
        //codigoSala = string.Empty;
        codigo = ingresoCodigo.text.Trim().ToLower();
        yield return StartCoroutine(JoinAndStore(codigo));
        if (string.IsNullOrEmpty(codigoSala))
            StartCoroutine(BuscarPartida());
        else
        {
            SceneManager.LoadScene("SalaEspera");
        }
    }

    IEnumerator BuscarPartida()
    {
        string url = baseUrl + "/game/load/" + codigo;
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success || req.downloadHandler.text == "Sala no existe.")
            {
                textoError.text = "No se pudo unir: sala no existe o código incorrecto.";
                codigoSala = string.Empty;
                yield break;
            }
            else
                SceneManager.LoadScene("SalaEspera");
        }
    }
}
