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
        float limiteDerecho = anchoMapa / 6f;

        if (transform.position.x < limiteDerecho)
        {
            Vector3 nuevaPos = transform.position;
            nuevaPos.x = limiteDerecho;
            transform.position = nuevaPos;
        }
    }

   protected override bool EstaEnZonaValida()
    {
        float limiteDerecho = anchoMapa / 6f;
        float margen = 0.1f;

        return transform.position.x >= limiteDerecho - margen;
    }
}