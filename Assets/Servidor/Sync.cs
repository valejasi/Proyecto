using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public partial class Servidor
{
    IEnumerator SendLoop() 
    { 
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

        if (!misObjetos.TryGetValue("PORTA", out Transform miPorta) || miPorta == null)
        {
            Debug.LogError("No tengo mi PORTA asignado en misObjetos.");
            yield break;
        }

        string url = baseUrl + "/game/placePorta/" + codigoSala;

        PositionData data = new PositionData(miSessionId, miSlot, "PORTA", miPorta.position, miPorta.rotation);
        string json = JsonUtility.ToJson(data);

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
            portaEnviada = (resp == "OK");
            Debug.Log("PlacePorta RESP: " + resp);

            if (portaEnviada)
                Debug.Log("PORTA enviado y bloqueado en server.");
            else
                Debug.LogWarning("El server no aceptó el PORTA (resp != OK).");
        }
    }

    bool DronMove(int i, Transform t) 
    { 
        if ((t.position - ultimaPos[i]).sqrMagnitude > minPos * minPos)
            return true;

        if (Quaternion.Angle(t.rotation, ultimaRot[i]) > minRot)
            return true;

        return false;
    }

    IEnumerator SendMoveBatchDrones() 
    { 
        if (string.IsNullOrWhiteSpace(codigoSala) || string.IsNullOrWhiteSpace(miSessionId)) yield break;

        // no empieza la partida real hasta colocar porta
        if (!portaEnviada) yield break;

        Transform[] misDrones = (miSlot == 1) ? dronesP1 : dronesP2;
        if (misDrones == null || misDrones.Length == 0) yield break;

        int n = misDrones.Length;
        ultimaPos = new Vector3[n];
        ultimaRot = new Quaternion[n];
        
        PositionData[] items = new PositionData[misDrones.Length];
        int count = 0;

        for (int i = 0; i < misDrones.Length; i++)
        {
            Transform t = misDrones[i];
            if (t == null) continue;
            
            //Mandar posición solo si el dron se movió un mínimo de distancia, sino se está mandando todo el tiempo la posición de todos los drones, incluso si están quietos.
            if (!DronMove(i, t)) continue; 

            ultimaPos[i] = t.position;
            ultimaRot[i] = t.rotation;

            string objId = $"DRON_{i + 1}";
            items[count] = new PositionData(miSessionId, miSlot, objId, t.position, t.rotation);
            count++;
        }

        if (count == 0) yield break;

        if (count != items.Length)
        {
            PositionData[] trimmed = new PositionData[count];
            for (int i = 0; i < count; i++) trimmed[i] = items[i];
            items = trimmed;
        }

        MoveBatchRequest payload = new MoveBatchRequest { items = items };
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
                Debug.LogWarning("SendMoveBatch ERROR: " + req.error + " | " + req.downloadHandler.text);
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

                if (p.sessionId == miSessionId)
                    continue;

                if (string.IsNullOrWhiteSpace(p.objId))
                    continue;

                if (!objetosRemotos.TryGetValue(p.objId, out Transform t) || t == null)
                    continue;

                Vector3 pos = new Vector3(p.x, p.y, p.z);
                Quaternion rot = new Quaternion(p.qx, p.qy, p.qz, p.qw);

                remoteTargetPos[p.objId] = pos;
                remoteTargetRot[p.objId] = rot;
            }
        }
    }
}