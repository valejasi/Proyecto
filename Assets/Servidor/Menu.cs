using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class Menu : MonoBehaviour
{
    public TMP_InputField ingresoCodigo;
    public TextMeshProUGUI textoError;
    private string codigo;
    private Servidor srv;

    void Awake()
    {
        srv = FindFirstObjectByType<Servidor>(FindObjectsInactive.Include);
    }

    public void CrearPartida()
    {
        StartCoroutine(CrearPartidaCR());
    }

    IEnumerator CrearPartidaCR()
    {
        yield return StartCoroutine(srv.CreateAndStore());
        if (!string.IsNullOrEmpty(srv.codigoSala))
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
        yield return StartCoroutine(srv.JoinAndStore(codigo));
        if (string.IsNullOrEmpty(srv.codigoSala))
            StartCoroutine(BuscarPartida());
        else
        {
            SceneManager.LoadScene("SalaEspera");
        }
    }

    IEnumerator BuscarPartida()
    {
        string url = srv.baseUrl + "/game/load/" + codigo;
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success || req.downloadHandler.text == "Sala no existe.")
            {
                textoError.text = "No se pudo unir: sala no existe o código incorrecto.";
                srv.codigoSala = string.Empty;
                yield break;
            }
            else
                SceneManager.LoadScene("SalaEspera");
        }
    }
}
