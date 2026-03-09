using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Networking;


public class LobbyManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string escenaJuego = "SampleScene";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textoCodigoSala;
    [SerializeField] private TextMeshProUGUI textoJugador1;
    [SerializeField] private TextMeshProUGUI textoJugador2;
    [SerializeField] private TextMeshProUGUI textoEstado;
    [SerializeField] private Button          btnIniciar;
    [SerializeField] private TextMeshProUGUI textoBtnIniciar;

    private Servidor srv;
    private bool clienteListo = false;
    private bool iniciando    = false;

    private readonly Color colorAereo = new Color(0.40f, 0.78f, 1.00f);
    private readonly Color colorNaval = new Color(0.20f, 0.45f, 0.85f);
    private readonly Color colorGris  = new Color(0.55f, 0.55f, 0.55f);


    void Start()
    {
        srv = FindFirstObjectByType<Servidor>(FindObjectsInactive.Include);

        btnIniciar.interactable = false;
        btnIniciar.onClick.AddListener(OnIniciarClick);
        if (textoBtnIniciar != null) textoBtnIniciar.text = "Esperando jugadores...";

        if (srv == null)
        {
            Debug.LogWarning("Servidor no encontrado");
            textoCodigoSala.text = "Código: ?";
            SetSlotUI(textoJugador1, "Equipo Aereo", "Esperando...", colorGris);
            SetSlotUI(textoJugador2, "Equipo Naval", "Esperando...", colorGris);
            textoEstado.text = "Error: no se encontró el servidor.";
            return;
        }

        textoCodigoSala.text = "Código: " + srv.codigoSala.ToUpper();


        if (srv.miSlot == 1)
        {
            // SOY HOST
            SetSlotUI(textoJugador1, "Equipo Aereo", "Conectado", colorAereo);
            SetSlotUI(textoJugador2, "Equipo Naval", "Esperando...", colorGris);
            textoEstado.text = "Esperando al jugador naval...";
            StartCoroutine(EsperarCliente());
        }
        else
        {
            // SOY CLIENTE NAVAL
            SetSlotUI(textoJugador1, "Equipo Aereo", "Conectado", colorAereo);
            SetSlotUI(textoJugador2, "Equipo Naval", "Conectado", colorNaval);
            textoEstado.text = "Conectado. Esperando que el host inicie...";
            clienteListo = true;
            StartCoroutine(EsperarInicioHost());
        }
    }

    // El host pollea cada 1.5s hasta que el Naval se une (vidas.Length >= 2)
    IEnumerator EsperarCliente()
    {
        string url = srv.baseUrl + "/game/state/" + srv.codigoSala;

        while (!clienteListo)
        {
            yield return new WaitForSeconds(1.5f);

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) continue;

                Servidor.StateResponse state = JsonUtility.FromJson<Servidor.StateResponse>(req.downloadHandler.text);
                
                if (state?.vidas == null) continue;



                if (state.vidas.Length >= 2)
                {
                    clienteListo = true;
                    SetSlotUI(textoJugador2, "Equipo Naval", "Conectado", colorNaval);
                    textoEstado.text        = "¡Sala completa! Podés iniciar la partida.";
                    btnIniciar.interactable = true;
                    if (textoBtnIniciar != null) textoBtnIniciar.text = "¡INICIAR PARTIDA!";
                }
            }
        }
    }

    // El Naval pollea cada 0.5s esperando que el host mande posiciones
    // (el host empieza a mandar posiciones cuando coloca el porta en SampleScene)
    IEnumerator EsperarInicioHost()
    {
        string url = srv.baseUrl + "/game/state/" + srv.codigoSala;

        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) continue;

                Servidor.StateResponse state = JsonUtility.FromJson<Servidor.StateResponse>(req.downloadHandler.text);
                Debug.Log("State recibido: " + req.downloadHandler.text);
                if (state?.posiciones == null) continue;

                if (state.vidas != null && state.vidas.Length >= 2)
                {
                    Debug.Log("Posiciones: " + state.posiciones.Length);
                    Debug.Log("Host inició partida, entrando al juego");
                    SceneManager.LoadScene(escenaJuego);
                    yield break;
                }
            }
        }
    }

    void OnIniciarClick()
    {
        if (srv.miSlot != 1 || !clienteListo || iniciando) return;
        iniciando = true;
        btnIniciar.interactable = false;
        StartCoroutine(ContadorInicio());
    }

    IEnumerator ContadorInicio()
    {
        for (int i = 3; i > 0; i--)
        {
            textoEstado.text     = "Iniciando en " + i + "...";
            textoEstado.fontSize = 72;
            yield return new WaitForSeconds(0.3f);
            textoEstado.fontSize = 54;
            yield return new WaitForSeconds(0.7f);
        }
        textoEstado.text = "¡Comienza la partida!";
        yield return new WaitForSeconds(0.8f);
        SceneManager.LoadScene(escenaJuego);
    }


    void SetSlotUI(TextMeshProUGUI texto, string equipo, string estado, Color color)
    {
        texto.text  = equipo + "\n<size=70%>" + estado + "</size>";
        texto.color = color;
    }
}