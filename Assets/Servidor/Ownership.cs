using UnityEngine;

//define que pertenece al jugador local y remoto
//configuraciones fisicas y de control

public partial class Servidor
{
    void IniciarSyncAutomatico()
    {
        if (sendLoop == null) sendLoop = StartCoroutine(SendLoop());
        if (receiveLoop == null) receiveLoop = StartCoroutine(ReceiveLoop());
        Debug.Log("Sync automático iniciado");
    }

    void SetSlot(int slot)
    {
        miSlot = slot;
        portaEnviada = false;

        RebuildObjectMapsForSlot();
        AplicarOwnershipMover();

        Debug.Log($"Slot asignado: {miSlot}. Mis objetos: {misObjetos.Count}. Remotos: {objetosRemotos.Count}");
    }

    void RebuildObjectMapsForSlotPreview()
    {
        miSlot = 1;
        RebuildObjectMapsForSlot();

        miSlot = 0;
        misObjetos.Clear();
        objetosRemotos.Clear();
        remoteTargetPos.Clear();
        remoteTargetRot.Clear();
    }

    void RebuildObjectMapsForSlot()
    {
        misObjetos.Clear();
        objetosRemotos.Clear();
        remoteTargetPos.Clear();
        remoteTargetRot.Clear();

        Transform miPorta = (miSlot == 1) ? porta1 : porta2;
        Transform otroPorta = (miSlot == 1) ? porta2 : porta1;

        misObjetos["PORTA"] = miPorta;
        objetosRemotos["PORTA"] = otroPorta;

        Transform[] misDrones = (miSlot == 1) ? dronesP1 : dronesP2;
        Transform[] dronesOtro = (miSlot == 1) ? dronesP2 : dronesP1;

        if (misDrones != null)
        {
            for (int i = 0; i < misDrones.Length; i++)
            {
                if (misDrones[i] == null) continue;
                string objId = $"DRON_{i + 1}";
                misObjetos[objId] = misDrones[i];
            }
        }

        if (dronesOtro != null)
        {
            for (int i = 0; i < dronesOtro.Length; i++)
            {
                if (dronesOtro[i] == null) continue;
                string objId = $"DRON_{i + 1}";
                objetosRemotos[objId] = dronesOtro[i];
            }
        }

        foreach (var kv in objetosRemotos)
        {
            if (kv.Value == null) continue;
            remoteTargetPos[kv.Key] = kv.Value.position;
            remoteTargetRot[kv.Key] = kv.Value.rotation;
        }
    }

    void AplicarOwnershipMover()
    {
        var mP1 = porta1.GetComponent<Mover>();
        var mP2 = porta2.GetComponent<Mover>();

        if (mP1 != null) mP1.isMine = (miSlot == 1);
        if (mP2 != null) mP2.isMine = (miSlot == 2);

        var rbP1 = porta1.GetComponent<Rigidbody>();
        var rbP2 = porta2.GetComponent<Rigidbody>();

        if (rbP1 != null) rbP1.isKinematic = (miSlot != 1);
        if (rbP2 != null) rbP2.isKinematic = (miSlot != 2);

        Transform[] d1 = dronesP1;
        Transform[] d2 = dronesP2;

        if (d1 != null)
        {
            for (int i = 0; i < d1.Length; i++)
            {
                if (d1[i] == null) continue;

                var m = d1[i].GetComponent<Mover>();
                if (m != null) m.isMine = (miSlot == 1);

                var rb = d1[i].GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = (miSlot != 1);
            }
        }

        if (d2 != null)
        {
            for (int i = 0; i < d2.Length; i++)
            {
                if (d2[i] == null) continue;

                var m = d2[i].GetComponent<Mover>();
                if (m != null) m.isMine = (miSlot == 2);

                var rb = d2[i].GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = (miSlot != 2);
            }
        }
    }
}