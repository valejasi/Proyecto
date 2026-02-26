using UnityEngine;

public class PortaDronNaval : PortaDronBase
{
    protected override void Start()
    {
        vidaMaxima = 3;
        dronesMaximos = 6;
        base.Start();
    }

    protected override void Morir()
    {
        Debug.Log("PortaDron Naval destruido");
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
        if (pos.x > 0) 
            pos.x = 0;
        if (pos.x < -10) 
            pos.x = -10;
        if (pos.z > 5)
            pos.z = 5;
        if (pos.z < -5) 
            pos.z = -5;
       
        transform.position = pos;
    }

   protected override bool EstaEnZonaValida()
    {
        Vector3 pos = transform.position;

        return pos.x <= 0 &&
            pos.x >= -10 &&
            pos.z <= 5 &&
            pos.z >= -5;
    }
}