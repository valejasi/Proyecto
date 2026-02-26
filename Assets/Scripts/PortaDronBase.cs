using UnityEngine;

public abstract class PortaDronBase : MonoBehaviour
{
    public int vidaMaxima;
    public int dronesMaximos;
    public bool estaSeleccionado = false;


    public GameObject prefabDron;

    protected int vidaActual;
    protected int dronesDesplegados;

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

        Instantiate(prefabDron, transform.position + transform.forward * 2f, Quaternion.identity);
        dronesDesplegados++;
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
}