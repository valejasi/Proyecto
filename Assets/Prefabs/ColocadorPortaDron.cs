using UnityEngine;

public class ColocadorPortaDron : MonoBehaviour
{
    public GameObject prefabPortaDron;
    public LayerMask capaSuelo;

    private GameObject instanciaActual;
    private bool yaColocado = false;

    public GameObject dronPrefab;

    public CamaraJugador camaraJugador;

    private int vContadorDrones = 0;
    private int cMaxDrones = 3;

    public int cId;

    void Start()
    {
        // Crear instancia al iniciar
        instanciaActual = Instantiate(prefabPortaDron);
    }

    void Update()
    {
         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
         RaycastHit hit;
        if (yaColocado)
        {
            Debug.Log("Estoy en modo colocado");

            //si hago click derecho, pongo un dron
            if (Input.GetMouseButtonDown(1))
            {
                if (vContadorDrones >= cMaxDrones)
                {
                    Debug.Log("Máximo de drones alcanzado");
                    return;
                }
                //detecta donde esta el mouse 
                Ray vRayDron = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit vHitDron;

                if (Physics.Raycast(vRayDron, out vHitDron, 1000f, capaSuelo))
                {
                    //pone el dron donde esta el mouse
                    GameObject VNuevoDron = Instantiate(dronPrefab, vHitDron.point + new Vector3(0, 0.5f, 0),
                            Quaternion.identity); // un poquito arriba del suelo
                    VNuevoDron.SetActive(true);
                    
                    vContadorDrones++;

                    // Asignar ID
                    Dron scriptDron = VNuevoDron.GetComponent<Dron>();
                    scriptDron.id = vContadorDrones;

                    Debug.Log("Dron creado con ID: " + vContadorDrones);
                }
            }

            //si hago click izquierdo entro al dron
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform.gameObject == dronPrefab)
                    {
                        camaraJugador.objetivo = dronPrefab.transform;
                        camaraJugador.modoColocacion = false;
                    }
                }
            }

            return;
        }

        if (Physics.Raycast(ray, out hit, 1000f, capaSuelo))
        {
            // Seguir el mouse
            instanciaActual.transform.position = hit.point;

            // Confirmar colocación
            if (Input.GetMouseButtonDown(0))
            {
                if (PosicionValida(hit.point))
                {
                    yaColocado = true;
                }
            }
        }
    }

    bool PosicionValida(Vector3 posicion)
    {
        // Ejemplo: mapa dividido por X
        // Ajustá estos valores según tu mapa real

        float limiteIzquierdo = -30f;
        float limiteCentro = 0f;
        float limiteDerecho = 30f;

        // EJEMPLO: naval solo en izquierda
        if (posicion.x < limiteCentro)
        {
            return true;
        }

        return false;
    }

}
