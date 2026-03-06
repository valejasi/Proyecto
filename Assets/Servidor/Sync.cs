using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
//sincronizacion cliente-servidor
public partial class Servidor
{
    private Dictionary<Transform, int> idPorTransformLocal = new Dictionary<Transform, int>();
    IEnumerator SendLoop()
    {
        Debug.Log("🔥 SendLoop INICIADO");
        while (true)
        {
            yield return SendMoveBatchDrones();
            yield return waitIntervalo;
        }
    }

    IEnumerator ReceiveLoop()
    {
        while (true)
        {
            yield return GetStateAndApplyRemotos();
            yield return waitIntervalo;
        }
    }

    IEnumerator PlacePortaOnce()
    {
        // evita doble envío
        if (portaEnviada) yield break;

        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId))
        {
            Debug.LogWarning("No hay sala/sessionId. No se puede colocar porta.");
            yield break;
        }

        //el primero en conectarse es aereo, id del portadron 0
        //el segundo es naval, id del dron = 1
        int portaId = (miSlot == 1) ? 0 : 1;

        if (!misObjetos.TryGetValue(portaId, out Transform miPorta) || miPorta == null)
        {
            Debug.LogError("No tengo mi PORTA asignado en misObjetos.");
            yield break;
        }

        string url = baseUrl + "/game/placePorta/" + codigoSala;

        PositionData data = new PositionData(
            miSessionId,
            miSlot,
            portaId,
            miPorta.position,
            miPorta.rotation
        );        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("PlacePorta ERROR: " + req.error + " | " + req.downloadHandler.text);
                yield break;
            }

            // leer OK/NO del backend
            string resp = (req.downloadHandler.text ?? "").Trim();
            Debug.Log("PlacePorta RESP RAW: [" + resp + "]");
            portaEnviada = resp.ToUpper().Contains("OK");
            Debug.Log("portaEnviada ahora es: " + portaEnviada);

            Debug.Log("PlacePorta RESP: " + resp);
            var rb = miPorta.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            if (portaEnviada)
                Debug.Log("PORTA enviado y bloqueado en server.");
            else
                Debug.LogWarning("El server no aceptó el PORTA (resp != OK).");
        }
    }

    bool DronMove(int i, Transform t)
    {
        if (ultimaPos == null || i >= ultimaPos.Length) return true;
        if ((t.position - ultimaPos[i]).sqrMagnitude > minPos * minPos)
            return true;
        if (Quaternion.Angle(t.rotation, ultimaRot[i]) > minRot)
            return true;
        return false;
    }

    IEnumerator SendMoveBatchDrones()
    {
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;
        if (!portaEnviada) yield break;
        if (misObjetos.Count == 0) yield break;

        List<PositionData> items = new List<PositionData>();

        foreach (var kv in misObjetos)
        {
            int objId = kv.Key;
            Transform t = kv.Value;

            if (t == null) continue;

            // skip portadrone, it has its own send (placePorta)
            if (objId == miPortaId) continue;

            // use objId as index key for ultimaPos/ultimaRot
            int idx = objId - ((miSlot == 1) ? 8 : 2);
            if (idx < 0) continue;

            Debug.Log($"SendBatch checking objId={objId} idx={idx} DronMove={DronMove(idx, t)}");

            if (!DronMove(idx, t)) continue;

            // grow arrays if needed
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

            ultimaPos[idx] = t.position;
            ultimaRot[idx] = t.rotation;

            items.Add(new PositionData(miSessionId, miSlot, objId, t.position, t.rotation));
        }

        if (items.Count == 0) yield break;

        MoveBatchRequest payload = new MoveBatchRequest { items = items.ToArray() };
        string json = JsonUtility.ToJson(payload);
        string url = baseUrl + "/game/moveBatch/" + codigoSala;

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("SendMoveBatch ERROR: " + req.error);
        }
    }

    IEnumerator GetStateAndApplyRemotos()
    {
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;

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

            for (int i = 0; i < st.posiciones.Length; i++)
            {

                PositionData p = st.posiciones[i];

                
                int slotRemoto = (miSlot == 1) ? 2 : 1;
                if (p.slot != slotRemoto)
                    continue;


                if (!objetosRemotos.TryGetValue(p.objId, out Transform t) || t == null){
                    Debug.Log($"No encontrado en objetosRemotos, llamando CrearObjetoRemoto objId={p.objId}");
                    // si recibe id de un dron pero el dron no existe, lo crea
                    CrearObjetoRemoto(p);
                    continue;
                }

                Vector3 pos = new Vector3(p.x, p.y, p.z);
                Quaternion rot = new Quaternion(p.qx, p.qy, p.qz, p.qw);

                remoteTargetPos[p.objId] = pos;
                remoteTargetRot[p.objId] = rot;
            }
            ProcesarProyectilesRemotos(st.proyectiles);
            ProcesarVidas(st.vidas);
            ProcesarResultado(st.resultado); 
        }
    }

    IEnumerator Recargar(int objIdDisparador)
    {
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;
        if (!portaEnviada) yield break;

        string url = baseUrl + "/game/recargar/" + codigoSala;

        RecargaRequest reqBody = new RecargaRequest
        {
            sessionId = miSessionId,
            objIdDisparador = objIdDisparador
        };

        string json = JsonUtility.ToJson(reqBody);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("Recargar ERROR: " + req.error + " | " + req.downloadHandler.text);
        }
    }

    //funcion que registra un nuevo dron
    public void RegistrarDronDesplegado(GameObject dronObj, int index)
    {
        int baseId = (miSlot == 1) ? 8 : 2;
        int objId  = baseId + index;

        Disparo d = dronObj.GetComponent<Disparo>();
        if (d != null)
        {
            d.isMine = true;
            d.objIdDisparador = objId;
        }

        // Register in my objects map
        misObjetos[objId] = dronObj.transform;
        idPorTransformLocal[dronObj.transform] = objId;

        Debug.Log($"Dron registrado: objId={objId} index={index}");

        // Expand ultimaPos/ultimaRot arrays if needed
        if (ultimaPos == null || index >= ultimaPos.Length)
        {
            int newSize = index + 1;
            Vector3[]    newPos = new Vector3[newSize];
            Quaternion[] newRot = new Quaternion[newSize];

            if (ultimaPos != null)
                for (int i = 0; i < ultimaPos.Length; i++) { newPos[i] = ultimaPos[i]; newRot[i] = ultimaRot[i]; }

            ultimaPos = newPos;
            ultimaRot = newRot;
        }

        ultimaPos[index] = dronObj.transform.position;
        ultimaRot[index] = dronObj.transform.rotation;

        Debug.Log($"Dron registrado: objId={objId} index={index}");
    }

    private readonly HashSet<string> proyectilesVivos = new HashSet<string>();

    void ProcesarProyectilesRemotos(ProyectilData[] proyectiles)
    {
        if (proyectiles == null) return;

        HashSet<string> idsActuales = new HashSet<string>();

        foreach (var p in proyectiles)
        {
            idsActuales.Add(p.id);

            if (proyectilesVivos.Contains(p.id)) continue;
            
            if (p.sessionId == miSessionId)
            {
                proyectilesVivos.Add(p.id); 
                continue;
            }

            proyectilesVivos.Add(p.id);
            SpawnProyectilRemoto(p);
        }

        proyectilesVivos.IntersectWith(idsActuales);
    }

    void SpawnProyectilRemoto(ProyectilData p)
    {
        if (balaPrefabRemoto == null)
        {
            Debug.LogWarning("balaPrefabRemoto no asignado en el Inspector.");
            return;
        }

        Vector3 pos = new Vector3(p.x, p.y, p.z);
        GameObject bala = Instantiate(balaPrefabRemoto, pos, Quaternion.identity);
        Destroy(bala, 3f);

        Rigidbody rb = bala.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = new Vector3(p.dx, p.dy, p.dz) * p.velocidad;
    }

    void ProcesarVidas(VidaData[] vidas)
    {
        if (vidas == null) return;

        foreach (var v in vidas)
        {
            Transform t = null;
            misObjetos.TryGetValue(v.objId, out t);
            if (t == null) objetosRemotos.TryGetValue(v.objId, out t);
            if (t == null) continue;

            DronBase dron = t.GetComponent<DronBase>();
            if (dron != null) { 
                dron.SetVidaDesdeServidor(v.vida); 
                if (v.vida <= 0) {
                    misObjetos.Remove(v.objId);
                    objetosRemotos.Remove(v.objId);
                }
                continue; 
            }

            PortaDronBase porta = t.GetComponent<PortaDronBase>();
            if (porta != null) 
                porta.SetVidaDesdeServidor(v.vida);
        }
    }

    void ProcesarResultado(ResultadoData resultado)
    {
        if (resultado == null) return;
        PantallaResultado pantalla = FindAnyObjectByType<PantallaResultado>();
        if (pantalla != null)
            pantalla.ActualizarEstado(resultado, miSlot.ToString());
    }


    public IEnumerator DispararDesdeServidor(int objIdDisparador, Vector3 origen, Vector3 dir, float velocidad)
    {
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;
        if (!portaEnviada) yield break;

        string url = baseUrl + "/game/disparar/" + codigoSala;

        DisparoRequest reqBody = new DisparoRequest
        {
            sessionId = miSessionId,
            objIdDisparador = objIdDisparador,
            x = origen.x, y = origen.y, z = origen.z,
            dx = dir.normalized.x, dy = dir.normalized.y, dz = dir.normalized.z,
            velocidad = velocidad,
            rangoMax = 30,
            danio = 1
        };

        string json = JsonUtility.ToJson(reqBody);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("Disparar ERROR: " + req.error + " | " + req.downloadHandler.text);
        }
    }
}