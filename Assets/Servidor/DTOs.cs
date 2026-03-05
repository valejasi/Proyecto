using System;
using UnityEngine;
//estructuras de datos para enviar y traer del backend
public partial class Servidor
{
    [System.Serializable]
    public class JoinResponse
    {
        public string codigo;
        public string sessionId;
        public int jugadores;
        public int portaId;
        public int[] dronesIds;
    }

    [System.Serializable]
    public class MoveBatchRequest
    {
        public PositionData[] items;
    }

    [System.Serializable]
    public class StateResponse
    {
        public PositionData[] posiciones;
        public VidaData[] vidas;
        public AmmoData[] municion;
        public ProyectilData[] proyectiles;
    }

    [System.Serializable]
    public class VidaData
    {
        public int objId;
        public int vida;
    }

    [System.Serializable]
    public class AmmoData
    {
        public string sessionId;
        public int objId;
        public int municion;
    }

    [System.Serializable]
    public class ProyectilData
    {
        public string id;
        public float x, y, z;
        public float dx, dy, dz;   
        public float velocidad; 
    }

    [System.Serializable]
    public struct PositionData
    {
        public string sessionId;
        public int slot;
        public int objId;
        public string tipo;
        public float x, y, z;
        public float qx, qy, qz, qw;

        public PositionData(string sid, int slot, int objId, Vector3 p, Quaternion q)
        {
            this.sessionId = sid;
            this.slot = slot;
            this.objId = objId;
            this.tipo = "";

            x = p.x; y = p.y; z = p.z;
            qx = q.x; qy = q.y; qz = q.z; qw = q.w;
        }

        // 🔹 Constructor nuevo (opcional)
        public PositionData(string sid, int slot, int objId, Vector3 p, Quaternion q, string tipo)
            : this(sid, slot, objId, p, q)
        {
            this.tipo = tipo;
        }
    }

    [System.Serializable]
    public class DisparoRequest
    {
        public string sessionId;
        public int objIdDisparador;
        public float x, y, z;
        public float dx, dy, dz;
        public float velocidad;
        public float rangoMax;
        public int danio;
    }

    [System.Serializable]
    public class RecargaRequest
    {
        public string sessionId;
        public int objIdDisparador;
    }
}