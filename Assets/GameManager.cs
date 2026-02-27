using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static bool juegoIniciado = false;

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