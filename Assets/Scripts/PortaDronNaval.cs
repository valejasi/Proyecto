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
        float limiteIzquierdo = -anchoMapa / 6f;

        if (transform.position.x > limiteIzquierdo)
        {
            Vector3 nuevaPos = transform.position;
            nuevaPos.x = limiteIzquierdo;
            transform.position = nuevaPos;
        }
    }

   protected override bool EstaEnZonaValida()
    {
        float limiteIzquierdo = -anchoMapa / 6f;
        float margen = 0.1f;

        return transform.position.x <= limiteIzquierdo + margen;
    }
}