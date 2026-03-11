
using UnityEngine;
//define que pertenece al jugador local y remoto
//configuraciones fisicas y de control
public partial class Servidor
{
    void IniciarSyncAutomatico()
    {
        
        if (sendLoop == null)
        {
            //PARA EL DEBUG
            Debug.Log("SendLoop arrancó");
            sendLoop = StartCoroutine(SendLoop());
        }
        
        if (receiveLoop == null)
        {
            //PARA EL DEBUG
            Debug.Log("Sync automático iniciado");
            receiveLoop = StartCoroutine(ReceiveLoop());
        } 
        
    }

    [SerializeField] public CamaraJugador camaraJugador;  

    void SetSlot(int slot)
    {
        Debug.Log("SetSlot llamado con slot: " + slot);
        miSlot = slot;
        portaEnviada = false;
        if (camaraJugador != null)
            camaraJugador.esAereo = (miSlot == 1);
        RebuildObjectMapsForSlot();
        AplicarOwnershipMover();
        Transform[] misDrones = (miSlot == 1) ? dronesP1 : dronesP2;
        if (misDrones != null)
        {
            ultimaPos = new Vector3[misDrones.Length];
            ultimaRot = new Quaternion[misDrones.Length];
            for (int i = 0; i < misDrones.Length; i++)
            {
                if (misDrones[i] == null) continue;
                ultimaPos[i] = misDrones[i].position;
                ultimaRot[i] = misDrones[i].rotation;
            }
        }
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

        int miPortaObjId   = (miSlot == 1) ? 0 : 1;
        int otroPortaObjId = (miSlot == 1) ? 1 : 0;

        Transform miPorta   = (miSlot == 1) ? porta1 : porta2;
        Transform otroPorta = (miSlot == 1) ? porta2 : porta1;

        misObjetos[miPortaObjId]       = miPorta;
        objetosRemotos[otroPortaObjId] = otroPorta;

        Transform[] misDrones = (miSlot == 1) ? dronesP1 : dronesP2;
        int baseMio = (miSlot == 1) ? 8 : 2;

        if (misDrones != null)
        {
            for (int i = 0; i < misDrones.Length; i++)
            {
                if (misDrones[i] == null) continue;
                misObjetos[baseMio + i] = misDrones[i];
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