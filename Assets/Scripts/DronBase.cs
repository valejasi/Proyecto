using UnityEngine;

public abstract class DronBase : MonoBehaviour
{
    [Header("Stats Base")]
    public int vidaMaxima = 1;
    protected int vidaActual;

    public int daño = 1;

    [Header("Movimiento y Vision")]
    public float porcentajeVision;
    public float porcentajeVelocidad;

    protected virtual void Start()
    {
        vidaActual = vidaMaxima;
    }

    public virtual void RecibirImpacto(int dañoRecibido)
    {
        vidaActual -= dañoRecibido;

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    protected abstract void Morir();
}