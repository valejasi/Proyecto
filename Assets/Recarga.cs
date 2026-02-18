using UnityEngine;

public class EstacionRecargaMunicion : MonoBehaviour
{
    public float distanciaRecarga = 2.5f;

    private Transform jugador;
    private Municion municionJugador;

    void Start()
    {
        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null)
        {
            jugador = objJugador.transform;
            municionJugador = objJugador.GetComponent<Municion>();
        }
    }

    void Update()
    {
        if (jugador == null || municionJugador == null) return;

        float dist = Vector3.Distance(transform.position, jugador.position);

        if (dist <= distanciaRecarga)
        {
            municionJugador.RecargarCompleto();
        }
    }

    // (opcional) para ver la distancia en la escena
    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, distanciaRecarga);
    }
}

