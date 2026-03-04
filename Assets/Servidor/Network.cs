using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
//gestion de comunicacion inicial con el backend
//sala, unirse, solicitudes y almacenamiento de info de la session
public partial class Servidor
{
    IEnumerator CreateAndStore()
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

            Transform[] misDrones = (miSlot == 1) ? dronesP1 : dronesP2;

            for (int i = 0; i < misDrones.Length; i++)
            {
                idPorTransformLocal[misDrones[i]] = resp.dronesIds[i];
            }
            // HOST = SLOT 1
            SetSlot(1);
            IniciarSyncAutomatico();

            Debug.Log($"CREADO. codigoSala={codigoSala} miSessionId={miSessionId} jugadores={resp.jugadores}");
        }
    }

    IEnumerator JoinAndStore(string code)
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

            Transform[] misDrones = (miSlot == 1) ? dronesP1 : dronesP2;

            for (int i = 0; i < misDrones.Length; i++)
            {
                idPorTransformLocal[misDrones[i]] = resp.dronesIds[i];
            }

            // CLIENTE = SLOT 2
            SetSlot(2);
            IniciarSyncAutomatico();

            Debug.Log($"UNIDO. codigoSala={codigoSala} miSessionId={miSessionId} jugadores={resp.jugadores}");
        }
    }

    //GUARDAR Y LEVANTAR PARTIDA
    public void GuardarPartida()
    {
        StartCoroutine(GuardarCoroutine());
    }
    IEnumerator GuardarCoroutine()
    {
        string url = baseUrl + "/game/save/" + codigoSala;
        Debug.Log("Codigo sala: " + codigoSala);
        Debug.Log("URL FINAL: " + url);
        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("Guardado OK");
        else
            Debug.LogError(request.error);
    }

     public void CargarPartida()
    {
        StartCoroutine(CargarYReconstruir());
    }

    IEnumerator CargarYReconstruir()
    {
        string url = baseUrl + "/game/load/" + codigoSala;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error Load: " + req.error);
                yield break;
            }
        }

        // 🔥 después del load traemos estado
        yield return StartCoroutine(GetStateCompletoYReconstruir());
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

            foreach (PositionData p in st.posiciones)
            {
                if (p.sessionId == miSessionId)
                    continue;

                if (!objetosRemotos.TryGetValue(p.objId, out Transform t) || t == null)
                {
                    CrearObjetoRemoto(p);
                    continue;
                }

                Vector3 pos = new Vector3(p.x, p.y, p.z);
                Quaternion rot = new Quaternion(p.qx, p.qy, p.qz, p.qw);

                remoteTargetPos[p.objId] = pos;
                remoteTargetRot[p.objId] = rot;
            }
        }
    }

    //crear objeto remoto
      void CrearObjetoRemoto(PositionData p)
    {
        GameObject prefab = null;

        if (p.tipo == "DRON")
            prefab = dronPrefab;
        else if (p.tipo == "PORTA")
            prefab = portaDronPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("Prefab no asignado para tipo: " + p.tipo);
            return;
        }

        Vector3 pos = new Vector3(p.x, p.y, p.z);
        Quaternion rot = new Quaternion(p.qx, p.qy, p.qz, p.qw);

        GameObject obj = Instantiate(prefab, pos, rot);

        objetosRemotos[p.objId] = obj.transform;
        remoteTargetPos[p.objId] = pos;
        remoteTargetRot[p.objId] = rot;
    }
}