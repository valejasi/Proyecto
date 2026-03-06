using UnityEngine;

public class PortaDronAereo : PortaDronBase
{
    protected override void Start()
    {
        vidaMaxima = 6;
        dronesMaximos = 12;
        base.Start();
    }

    protected override void Morir()
    {
        Debug.Log("PortaDron Aereo destruido");
        Destroy(gameObject);
    }

    void Update()
    {
        CorregirPosicion();
        DetectarInput();
    }

    void CorregirPosicion()
    {
        Vector3 pos = transform.position;

        if (pos.x < minX) 
            pos.x = minX;
        if (pos.x > maxX) 
            pos.x = maxX;
        if (pos.z > maxZ) 
            pos.z = maxZ;
        if (pos.z < minZ) 
            pos.z = minZ;

        transform.position = pos;
    }

   protected override bool EstaEnZonaValida()
    {
        Vector3 pos = transform.position;

        return pos.x >= minX &&
            pos.x <= maxX &&
            pos.z <= maxZ &&
            pos.z >= minZ;
    }
}