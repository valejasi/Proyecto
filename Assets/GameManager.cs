using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static bool juegoIniciado = false;

    void Awake()
    {
        Servidor srv = FindFirstObjectByType<Servidor>(FindObjectsInactive.Include);
        GameObject porta1Aux = GameObject.FindGameObjectWithTag("PortaAereo");
        GameObject porta2Aux = GameObject.FindGameObjectWithTag("PortaNaval");
        srv.camaraJugador = FindFirstObjectByType<CamaraJugador>(FindObjectsInactive.Include);
        
        if (porta1Aux != null) 
            srv.porta1 = porta1Aux.transform;
        if (porta2Aux != null)
            srv.porta2 = porta2Aux.transform;
    }

    void Update()
    {
        // Para probar: apretando Enter empieza el juego
        if (Input.GetKeyDown(KeyCode.Return))
        {
            juegoIniciado = true;
            Debug.Log("Juego iniciado - ahora pueden invadir");
        }
    }
}