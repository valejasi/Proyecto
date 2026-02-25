using UnityEngine;

public class DronNaval : DronBase
{
    [Header("Armas")]
    public int misiles = 2;

    protected override void Start()
    {
        porcentajeVision = 0.5f;     // 50%
        porcentajeVelocidad = 1f;    // 100%
        base.Start();
    }

    protected override void Morir()
    {
        Debug.Log("Dron Naval destruido");
        Destroy(gameObject);
    }
}