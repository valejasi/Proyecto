using UnityEngine;

public abstract class PortaDronBase : MonoBehaviour
{
    public int vidaMaxima;
    public int dronesMaximos;
    public bool estaSeleccionado = false;

    public GameObject prefabDron;

    protected int vidaActual;
    protected int dronesDesplegados;

    public Servidor servidor;

    protected virtual void Start()
    {
        Debug.Log("PortaDronBase Start ejecutado");
        vidaActual = vidaMaxima;
    }

    public void DesplegarDron()
    {
        Debug.Log("Intentando desplegar");

        if (!EstaEnZonaValida())
        {
            Debug.Log("No está en zona válida");
            return;
        }

        if (dronesDesplegados >= dronesMaximos)
        {
            Debug.Log("Ya alcanzó el máximo de drones");
            return;
        }

        Debug.Log("Desplegando dron");

        GameObject nuevoDron = Instantiate(prefabDron, transform.position + transform.forward * 2f, Quaternion.identity);

        // ← marcar el dron como mío al desplegarlo
        Mover m = nuevoDron.GetComponent<Mover>();
        if (m != null)
            m.isMine = true;

        dronesDesplegados++;

        Servidor srv = FindAnyObjectByType<Servidor>();
        if (srv != null)
            srv.RegistrarDronDesplegado(nuevoDron, dronesDesplegados - 1);
    }

    protected abstract bool EstaEnZonaValida();

    void Update()
    {
        DetectarInput();
    }

    protected void DetectarInput()
    {
        if (!estaSeleccionado)
            return;

        if (Input.GetMouseButtonDown(1))
        {
            DesplegarDron();
        }
    }

    protected abstract void Morir();

    [Header("Limites del Mapa")]
    public float anchoMapa = 60f;

    public virtual void SetVidaDesdeServidor(int nuevaVida)
    {
        vidaActual = nuevaVida;
        if (vidaActual <= 0)
            Morir();
    }
}