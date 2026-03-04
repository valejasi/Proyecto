using UnityEngine;

public class DronAereo : DronBase
{
    [Header("Armas")]
    public int bombas = 1;

    public override bool EsAereo => true;  

    protected override void Start()
    {
        porcentajeVision = 1f;
        porcentajeVelocidad = 0.8f;
        base.Start();
    }

    protected override void Morir()
    {
        Debug.Log("Dron Aereo destruido");
        Destroy(gameObject);
    }
}