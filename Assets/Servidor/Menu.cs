using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using Unity.VisualScripting;
using System.Diagnostics;

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
    private bool join;
    private bool load;
    private bool encontrado;


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
        codigo = ingresoCodigo.text.Trim().ToLower();
        srv.codigoSala = codigo;
        yield return StartCoroutine(CheckJoin());

        if (join)
            StartCoroutine(BuscarPartida());
        else if (encontrado)
        {
            StartCoroutine(BuscarPartida()); 
        }
        else
        {
            yield return StartCoroutine(srv.CargarYReconstruir());
            yield return StartCoroutine(CheckLoad());
            if (load)
            {
                MenuUI.SetActive(false);
                lobbyManager.SetActive(true);
                SalaEspera.SetActive(true);
                textoError.text = "Entra 1";
            }
            else
            {
                textoError.text = "No se pudo unir: sala no existe o código incorrecto.";
                yield break;
            }
        }
    }

    IEnumerator BuscarPartida()
    {
        yield return StartCoroutine(srv.JoinAndStore(codigo));
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
            textoError.text = "Entra 2";
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

    IEnumerator CheckJoin()
    {
        join = false;
        encontrado = false;
        string url = srv.baseUrl + "/game/state/" + codigo;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                yield break;

            string json = req.downloadHandler.text;
            
            if (string.IsNullOrWhiteSpace(json) || json == "Sala no existe.") //Es un asco, pero sino da error parseando el json
                yield break;

            Servidor.StateResponse st = JsonUtility.FromJson<Servidor.StateResponse>(json);

            if (st == null)
                yield break;

            if (st.posiciones.Length == 0 && st.vidas != null)//Unirse a partida nueva
            {
                join = true;
                yield break;
            } 
                
             bool haySlot1 = false, haySlot2 = false;
            foreach (var p in st.posiciones)
            {
                if (p.slot == 1) haySlot1 = true;
                if (p.slot == 2) haySlot2 = true;
            }

            // Solo hacer join si ya hay un slot 2 activo (partida en curso)
            // o si hay slot1 Y slot2 — significa ambos ya estaban conectados
            if (haySlot1 && haySlot2)
                join = true;
            else if (haySlot1)
            {
                encontrado = true; 
            }
            // Si solo hay slot1 → es partida guardada esperando al 2do → join = false → va por CargarYReconstruir
        
        }
    }

    IEnumerator CheckLoad()
    {
        load = false;
        string url = srv.baseUrl + "/game/state/" + codigo;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                yield break;

            string json = req.downloadHandler.text;
            
            if (string.IsNullOrWhiteSpace(json) || json == "Sala no existe.")
                yield break;

            Servidor.StateResponse st = JsonUtility.FromJson<Servidor.StateResponse>(json);

            if (st == null || st.posiciones.Length == 0)
                yield break;

            load = true;
        }
    }
}
