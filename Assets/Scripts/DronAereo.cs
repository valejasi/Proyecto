using UnityEngine;

public class DronAereo : DronBase
{
    [Header("Armas")]
    public int bombas = 1;

    protected override void Start()
    {
        porcentajeVision = 1f;       // 100%
        porcentajeVelocidad = 0.8f;  // 80%
        base.Start();
    }

    protected override void Morir()
    {
        Debug.Log("Dron Aereo destruido");
        Destroy(gameObject);
    }
}