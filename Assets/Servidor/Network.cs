using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
//gestion de comunicacion inicial con el backend
//sala, unirse, solicitudes y almacenamiento de info de la session
public partial class Servidor
{
    public IEnumerator CreateAndStore()
    {
        string url = baseUrl + "/game/create";
        Debug.Log("GET: " + url);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Create ERROR: " + req.error);
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("Create JSON: " + json);

           JoinResponse resp = JsonUtility.FromJson<JoinResponse>(json);

            codigoSala = resp.codigo;
            miSessionId = resp.sessionId;
            miPortaId = resp.portaId;

            // HOST = SLOT 1
            SetSlot(1);

            Transform[] misDrones = (miSlot == 1) ? dronesP1 : dronesP2;

           for (int i = 0; i < misDrones.Length; i++)
            {
                if (resp.dronesIds[i] == -1) continue; // fix del bug anterior
                idPorTransformLocal[misDrones[i]] = resp.dronesIds[i];

                // Activar disparo en drones locales
                Disparo d = misDrones[i].GetComponent<Disparo>();
                Debug.Log($"[Disparo] dron {i}: componente={d != null}");
                if (d != null)
                {
                    d.isMine = true;
                    d.objIdDisparador = resp.dronesIds[i];
                }
            }
            IniciarSyncAutomatico();
            OcultarTodosEnemigos();
        }
    }

    public IEnumerator JoinAndStore(string code)
    {
        string url = baseUrl + "/game/join/" + code;
        Debug.Log("GET: " + url);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Join ERROR: " + req.error);
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("Join JSON: " + json);

            JoinResponse resp = JsonUtility.FromJson<JoinResponse>(json);
            if (resp.jugadores == 0)
            {
                Debug.LogError("No se pudo unir: sala no existe o código incorrecto.");
                yield break;
            }

            codigoSala = resp.codigo;
            miSessionId = resp.sessionId;
            miPortaId = resp.portaId;

            // CLIENTE = SLOT 2
            SetSlot(2);

            Transform[] misDrones = (miSlot == 1) ? dronesP1 : dronesP2;

            for (int i = 0; i < misDrones.Length; i++)
            {
                if (resp.dronesIds[i] == -1) continue; // fix del bug anterior
                idPorTransformLocal[misDrones[i]] = resp.dronesIds[i];

                // Activar disparo en drones locales
                Disparo d = misDrones[i].GetComponent<Disparo>();
                if (d != null)
                {
                    d.isMine = true;
                    d.objIdDisparador = resp.dronesIds[i];
                }
            }
            bool tieneEstadoGuardado = resp.dronesIds != null && 
                System.Array.Exists(resp.dronesIds, id => id != -1);

            if (tieneEstadoGuardado)
                yield return StartCoroutine(GetStateCompletoYReconstruir());

                    IniciarSyncAutomatico();
        }

        IniciarSyncAutomatico();
        OcultarTodosEnemigos();
    }

    //GUARDAR Y LEVANTAR PARTIDA
    public void GuardarPartida()
    {
        StartCoroutine(GuardarCoroutine());
    }
    IEnumerator GuardarCoroutine()
    {
        string url = baseUrl + "/game/save/" + codigoSala;

        Debug.Log($"GuardarCoroutine iniciado - codigoSala='{codigoSala}'");
        if (string.IsNullOrWhiteSpace(codigoSala))
        {
            Debug.LogError("codigoSala está vacío, no se puede guardar");
            yield break;
        }

        Debug.Log("Codigo sala: " + codigoSala);
        Debug.Log("URL FINAL: " + url);
        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("Guardado OK");
        else
            Debug.LogError("Error: " + request.error + " | Body: " + request.downloadHandler.text);
    }

     public void CargarPartida()
    {
        StartCoroutine(CargarYReconstruir());
    }

    IEnumerator CargarYReconstruir()
    {
        string url = baseUrl + "/game/loadAndCreate/" + codigoSala;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error Load: " + req.error);
                cargandoPartida = false;
                yield break;
            }
            
            string json = req.downloadHandler.text;
            JoinResponse resp = JsonUtility.FromJson<JoinResponse>(json);

            codigoSala = resp.codigo;
            miSessionId = resp.sessionId;
            miPortaId = resp.portaId;
            portaEnviada = true;
            SetSlot(1);

            Debug.Log("Sala recreada como HOST (aéreo): " + codigoSala);
        }

        foreach (var kv in new Dictionary<int, Transform>(misObjetos))
        {
            if (kv.Key >= 8) // son drones, no porta
            {
                misObjetos.Remove(kv.Key);
            }
        }

        yield return StartCoroutine(GetStateCompletoYReconstruir());
        
        // Después de reconstruir, el sync ya sabe qué objetos hay
        Debug.Log($"Reconstrucción completa. MisObjetos: {misObjetos.Count} Remotos: {objetosRemotos.Count}");
    }

    IEnumerator GetStateCompletoYReconstruir()
    {
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId))
            yield break;

        string url = baseUrl + "/game/state/" + codigoSala;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                yield break;

            string json = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(json))
                yield break;

            StateResponse st = JsonUtility.FromJson<StateResponse>(json);
            lastState = st;

            if (st == null || st.posiciones == null)
                yield break;

            // Construir un set de objIds muertos para consulta rápida
            HashSet<int> muertos = new HashSet<int>();
            if (st.vidas != null)
            {
                foreach (var v in st.vidas)
                {
                    if (v.vida <= 0)
                        muertos.Add(v.objId);
                }
            }

            foreach (PositionData p in st.posiciones)
            {
                Vector3 pos = new Vector3(p.x, p.y, p.z);
                Quaternion rot = new Quaternion(p.qx, p.qy, p.qz, p.qw);

                if (muertos.Contains(p.objId)) continue;

                if (p.sessionId == miSessionId)
                {
                    // Es mío
                    if (misObjetos.TryGetValue(p.objId, out Transform tMio) && tMio != null)
                    {
                        // Ya existe → teleportar
                        tMio.position = pos;
                        tMio.rotation = rot;
                    }
                    else
                    {
                        // No existe todavía → instanciar como propio
                        CrearObjetoPropioDesdeEstado(p);
                    }

                    if (p.tipo == "PORTA")
                    {
                        portaEnviada = true;
                        miPortaId = p.objId;
                    }

                    // Actualizar ultimaPos/ultimaRot para que DronMove no spamee updates
                    int idx = p.objId - ((miSlot == 1) ? 8 : 2);
                    if (idx >= 0 && ultimaPos != null && idx < ultimaPos.Length)
                    {
                        ultimaPos[idx] = pos;
                        ultimaRot[idx] = rot;
                    }

                    if (p.sessionId == miSessionId && p.tipo == "PORTA")
                    {
                        portaEnviada = true;
                        miPortaId = p.objId;
                    }
                }
                else
                {
                    // Es remoto
                    if (!objetosRemotos.TryGetValue(p.objId, out Transform tRemoto) || tRemoto == null)
                    {
                        CrearObjetoRemoto(p);
                    }
                    else
                    {
                        // Ya existe → teleportar directo (sin lerp)
                        tRemoto.position = pos;
                        tRemoto.rotation = rot;
                        remoteTargetPos[p.objId] = pos;
                        remoteTargetRot[p.objId] = rot;
                    }
                }
            }
        }
    }

    void CrearObjetoPropioDesdeEstado(PositionData p)
    {
        GameObject prefab = null;

        if (p.tipo == "AEREO")
            prefab = dronAereoPrefab;
        else if (p.tipo == "NAVAL")
            prefab = dronNavalPrefab;
        else if (p.tipo == "PORTA")
            prefab = (p.slot == 1) ? portaDronAereoPrefab : portaDronNavalPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"CrearObjetoPropioDesdeEstado: prefab null para tipo={p.tipo}");
            return;
        }

        Vector3 pos = new Vector3(p.x, p.y, p.z);
        Quaternion rot = new Quaternion(p.qx, p.qy, p.qz, p.qw);

        GameObject obj = Instantiate(prefab, pos, rot);

        // Es mío → puede moverse y disparar
        Mover m = obj.GetComponent<Mover>();
        if (m != null) m.isMine = true;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        Disparo d = obj.GetComponent<Disparo>();
        if (d != null)
        {
            d.isMine = true;
            d.objIdDisparador = p.objId;
        }

        // Registrar en misObjetos
        misObjetos[p.objId] = obj.transform;

        // Si es portadron, marcar como enviado para que el sync arranque
        if (p.tipo == "PORTA")
        {
            portaEnviada = true;
            miPortaId = p.objId;
        }

        // Registrar en ultimaPos/ultimaRot
        int idx = p.objId - ((miSlot == 1) ? 8 : 2);
        if (idx >= 0)
        {
            if (ultimaPos == null || idx >= ultimaPos.Length)
            {
                int newSize = idx + 1;
                Vector3[] newPos = new Vector3[newSize];
                Quaternion[] newRot = new Quaternion[newSize];
                if (ultimaPos != null)
                    for (int i = 0; i < ultimaPos.Length; i++) { newPos[i] = ultimaPos[i]; newRot[i] = ultimaRot[i]; }
                ultimaPos = newPos;
                ultimaRot = newRot;
            }
            ultimaPos[idx] = pos;
            ultimaRot[idx] = rot;
        }

        Debug.Log($"ObjetoPROPIO creado desde estado: tipo={p.tipo} objId={p.objId}");
    }

    //crear objeto remoto
    void CrearObjetoRemoto(PositionData p)
    {
        GameObject prefab = null;

        if (p.tipo == "AEREO")
            prefab = dronAereoPrefab;
        else if (p.tipo == "NAVAL")
            prefab = dronNavalPrefab;
        else if (p.tipo == "PORTA")
        {
            prefab = (p.slot == 1) ? portaDronAereoPrefab : portaDronNavalPrefab;
        }
    

        Debug.Log($"CrearObjetoRemoto -> tipo={p.tipo} objId={p.objId} prefab={prefab?.name ?? "NULL"}");

        if (prefab == null)
        {
            Debug.LogWarning("Prefab no asignado para tipo: " + p.tipo);
            return;
        }

        Vector3 pos = new Vector3(p.x, p.y, p.z);
        Quaternion rot = new Quaternion(p.qx, p.qy, p.qz, p.qw);

        GameObject obj = Instantiate(prefab, pos, rot);

        foreach (var r in obj.GetComponentsInChildren<Renderer>())
             r.enabled = false;

        // make sure remote drone is not controllable
        Mover m = obj.GetComponent<Mover>();
        if (m != null) m.isMine = false;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        objetosRemotos[p.objId] = obj.transform;
        remoteTargetPos[p.objId] = pos;
        remoteTargetRot[p.objId] = rot;
    }

    // radio en unidades de mundo = porcentajeVision del dron activo * escala
    public float escalaVision = 30f; 

    public void OcultarTodosEnemigos()
    {
        foreach (var kv in objetosRemotos)
        {
            if (kv.Value == null) continue;
            SetVisible(kv.Value, false);
        }
    }

    public void ActualizarVisibilidadEnemigos(Transform dronActivo)
    {
        if (dronActivo == null) return;

        // obtener porcentajeVision del dron seleccionado
        DronBase dron = dronActivo.GetComponent<DronBase>();
        float vision = (dron != null) ? dron.porcentajeVision * escalaVision : escalaVision;

        foreach (var kv in objetosRemotos)
        {
            if (kv.Value == null) continue;
            float dist = Vector3.Distance(dronActivo.position, kv.Value.position);
            SetVisible(kv.Value, dist <= vision);
        }
    }

    private void SetVisible(Transform t, bool visible)
    {
        // activa/desactiva todos los renderers del objeto y sus hijos
        foreach (var r in t.GetComponentsInChildren<Renderer>())
            r.enabled = visible;
    }
}