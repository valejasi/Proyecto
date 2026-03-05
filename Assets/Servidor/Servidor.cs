using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Orquestador del sistema de red
//tiene la configuracion del inspectos, ciclo de vida de unity
//coordina y sincroniza
public partial class Servidor : MonoBehaviour
{
    [Header("Servidor")]
    [SerializeField] private string baseUrl = "https://proyecto-y1ud.onrender.com";

    [Header("PORTADRONES (arrastrar)")]
    [SerializeField] private Transform porta1; // host (aéreo)
    [SerializeField] private Transform porta2; // join (naval)

    [Header("DRONES P1 (AÉREO: 12)")]
    [SerializeField] private Transform[] dronesP1;

    [Header("DRONES P2 (NAVAL: 6)")]
    [SerializeField] private Transform[] dronesP2;

    [Header("Sync")]
    [SerializeField] private float intervalo = 0.1f;

    [Header("Smooth remoto")]
    [SerializeField] private float smoothPos = 12f;
    [SerializeField] private float smoothRot = 12f;

    [Header("Prefabs")]
    [SerializeField] private GameObject dronAereoPrefab;
    [SerializeField] private GameObject dronNavalPrefab;
    [SerializeField] private GameObject portaDronAereoPrefab;
    [SerializeField] private GameObject portaDronNavalPrefab;
    [SerializeField] private GameObject balaPrefabRemoto;

    private StateResponse lastState;

    // ==========================
    // Estado sala
    // ==========================
    protected string codigoSala = "";
    protected string miSessionId = "";
    protected string codigoIngresado = "";

    protected int miPortaId;


    // Slot: 1 = host (aéreo), 2 = join (naval)
    protected int miSlot = 0;

    // Coroutines
    protected Coroutine sendLoop;
    protected Coroutine receiveLoop;

    // Porta: se coloca una vez
    protected bool portaEnviada = false;

    // Remotos smoothing
    protected readonly Dictionary<int, Vector3> remoteTargetPos = new Dictionary<int, Vector3>();
    protected readonly Dictionary<int, Quaternion> remoteTargetRot = new Dictionary<int, Quaternion>();

    // Cache transforms
    protected readonly Dictionary<int, Transform> misObjetos = new Dictionary<int, Transform>();
    protected readonly Dictionary<int, Transform> objetosRemotos = new Dictionary<int, Transform>();


    protected WaitForSeconds waitIntervalo;

    [SerializeField] protected float minPos = 0.01f;
    [SerializeField] protected float minRot = 0.5f;

    protected Vector3[] ultimaPos;
    protected Quaternion[] ultimaRot;

    // ==========================
    // UNITY LIFECYCLE
    // ==========================
    void Awake()
    {
        waitIntervalo = new WaitForSeconds(intervalo);
    }

    void Start()
    {

        if (porta1 == null || porta2 == null)
        {
            Debug.LogError("Asigná porta1 y porta2 en el Inspector.");
            return;
        }
        RebuildObjectMapsForSlotPreview();
    }

    void Update()
    {
        // Host: crear sala
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            StartCoroutine(CreateAndStore());

        // Join: escribir código + Enter
        CapturarCodigoJoin();

        // Colocar PORTA
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (!portaEnviada)
                StartCoroutine(PlacePortaOnce());
            else
                Debug.Log("PORTA ya fue enviado/lockeado.");
        }

        //GUARDAR Y LEVANTAR PARTIDA
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            GuardarPartida();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2)) {
            CargarPartida();
        }

        // Aplicar smoothing a objetos remotos
        foreach (var kv in objetosRemotos)
        {
            int objId = kv.Key;
            Transform t = kv.Value;
            if (t == null) continue;

            if (remoteTargetPos.TryGetValue(objId, out Vector3 tp))
                t.position = Vector3.Lerp(t.position, tp, 1f - Mathf.Exp(-smoothPos * Time.deltaTime));

            if (remoteTargetRot.TryGetValue(objId, out Quaternion tr))
                t.rotation = Quaternion.Slerp(t.rotation, tr, 1f - Mathf.Exp(-smoothRot * Time.deltaTime));
        }
    }

    // ==========================
    // INPUT JOIN
    // ==========================
    void CapturarCodigoJoin()
    {
        foreach (char ch in Input.inputString)
        {
            // Enter
            if (ch == '\n' || ch == '\r')
            {
                if (!string.IsNullOrWhiteSpace(codigoIngresado))
                {
                    codigoSala = codigoIngresado.Trim().ToLower();
                    StartCoroutine(JoinAndStore(codigoSala));
                }
                continue;
            }

            // Backspace
            if (ch == '\b')
            {
                if (codigoIngresado.Length > 0)
                    codigoIngresado = codigoIngresado.Substring(0, codigoIngresado.Length - 1);
                continue;
            }

            // Solo letras/números
            if (char.IsLetterOrDigit(ch) && codigoIngresado.Length < 6)
                codigoIngresado += char.ToLower(ch);
        }
    }
}