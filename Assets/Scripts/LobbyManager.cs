using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Networking;

/// <summary>
/// Va en la escena "Sala de Espera".
/// Asume que el Servidor ya tiene codigoSala y miSessionId cargados
/// (se conectaron en la pantalla principal).
/// </summary>
public class LobbyManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string escenaJuego = "SampleScene";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textoCodigoSala;
    [SerializeField] private TextMeshProUGUI textoJugador1;    // Equipo Aéreo
    [SerializeField] private TextMeshProUGUI textoJugador2;    // Equipo Naval
    [SerializeField] private TextMeshProUGUI textoEstado;
    [SerializeField] private Button          btnIniciar;
    [SerializeField] private TextMeshProUGUI textoBtnIniciar;

    private Servidor srv;
    private bool clienteListo = false;
    private bool iniciando    = false;

    private readonly Color colorAereo = new Color(0.40f, 0.78f, 1.00f);
    private readonly Color colorNaval = new Color(0.20f, 0.45f, 0.85f);
    private readonly Color colorGris  = new Color(0.55f, 0.55f, 0.55f);

    // ══════════════════════════════════════════════════════════════════════
    void Start()
    {
        srv = FindFirstObjectByType<Servidor>();
        if (srv == null)
        {
            Debug.LogWarning("Servidor no encontrado - modo preview");
            // valores de prueba para ver la UI
            textoCodigoSala.text = "Código: FB24D7";
            SetSlotUI(textoJugador1, "✈  Equipo Aéreo", "Conectado ✓", colorAereo);
            SetSlotUI(textoJugador2, "⚓  Equipo Naval", "Esperando...", colorGris);
            textoEstado.text = "Esperando al jugador naval...";
            btnIniciar.interactable = false;
            return;
        }

        btnIniciar.interactable = false;
        btnIniciar.onClick.AddListener(OnIniciarClick);
        if (textoBtnIniciar != null) textoBtnIniciar.text = "Esperando jugadores...";

        // Mostrar código de sala (ya lo tiene el Servidor)
        textoCodigoSala.text = "Código: " + srv.codigoSala.ToUpper();

        // El jugador local ya está conectado — mostrar según su slot
        if (srv.miSlot == 1)
        {
            // soy host (Aéreo)
            SetSlotUI(textoJugador1, "✈  Equipo Aéreo", "Conectado ✓", colorAereo);
            SetSlotUI(textoJugador2, "⚓  Equipo Naval", "Esperando...", colorGris);
            textoEstado.text = "Sala creada. Esperando al jugador naval...";
            StartCoroutine(EsperarCliente());
        }
        else
        {
            // soy cliente (Naval)
            SetSlotUI(textoJugador1, "✈  Equipo Aéreo", "Conectado ✓", colorAereo);
            SetSlotUI(textoJugador2, "⚓  Equipo Naval", "Conectado ✓", colorNaval);
            textoEstado.text = "Conectado. Esperando que el host inicie...";
            clienteListo = true;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // POLLING: el host pregunta cada 1.5s si ya se unió el cliente
    // ══════════════════════════════════════════════════════════════════════

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
                if (state?.posiciones == null) continue;

                foreach (var p in state.posiciones)
                {
                    if (p.slot == 2)
                    {
                        clienteListo = true;
                        SetSlotUI(textoJugador2, "⚓  Equipo Naval", "Conectado ✓", colorNaval);
                        textoEstado.text        = "¡Sala completa! Podés iniciar la partida.";
                        btnIniciar.interactable = true;
                        if (textoBtnIniciar != null) textoBtnIniciar.text = "¡INICIAR PARTIDA!";
                        break;
                    }
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // INICIAR
    // ══════════════════════════════════════════════════════════════════════

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

    // ══════════════════════════════════════════════════════════════════════
    void SetSlotUI(TextMeshProUGUI texto, string equipo, string estado, Color color)
    {
        texto.text  = equipo + "\n<size=70%>" + estado + "</size>";
        texto.color = color;
    }
}