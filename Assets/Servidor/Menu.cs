using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using Unity.VisualScripting;

public class Menu : MonoBehaviour
{
    public TMP_InputField ingresoCodigo;
    public TextMeshProUGUI textoError;
    private string codigo;
    private Servidor srv;
    public GameObject SalaEspera;
    public GameObject lobbyManager;
    public GameObject MenuUI;
    public GameObject PanelInicio;
    public GameObject PanelControles;

    void Awake()
    {
        lobbyManager.SetActive(false);
        srv = FindFirstObjectByType<Servidor>(FindObjectsInactive.Include);
        SalaEspera.SetActive(false);
        PanelControles.SetActive(false);
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
            MenuUI.SetActive(false);
            lobbyManager.SetActive(true);
            SalaEspera.SetActive(true);
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
            MenuUI.SetActive(false);
            lobbyManager.SetActive(true);
            SalaEspera.SetActive(true);
        }
    }

    IEnumerator BuscarPartida()
    {
        srv.codigoSala = codigo;
        srv.CargarPartida();
        if (string.IsNullOrEmpty(srv.codigoSala))
        {
            textoError.text = "No se pudo unir: sala no existe o código incorrecto.";
            yield break;
        }
        else
        {
            MenuUI.SetActive(false);
            lobbyManager.SetActive(true);
            SalaEspera.SetActive(true);
        }
    }

    public void MostrarControles()
    {
        PanelControles.SetActive(true);
        PanelInicio.SetActive(false);
    }

    public void CerrarControles()
    {
        PanelControles.SetActive(false);
        PanelInicio.SetActive(true);
    }
}
