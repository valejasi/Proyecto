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
        public string sessionId;
        public string objId;
        public int vida;
    }

    [System.Serializable]
    public class AmmoData
    {
        public string sessionId;
        public string objId;
        public int ammo;
    }

    [System.Serializable]
    public class ProyectilData
    {
        public string id;
        public float x, y, z;
    }

    [System.Serializable]
    public struct PositionData
    {
        public string sessionId;
        public int slot;
        public string objId;

        public float x, y, z;
        public float qx, qy, qz, qw;

        public PositionData(string sid, int slot, string objId, Vector3 p, Quaternion q)
        {
            this.sessionId = sid;
            this.slot = slot;
            this.objId = objId;

            x = p.x; y = p.y; z = p.z;
            qx = q.x; qy = q.y; qz = q.z; qw = q.w;
        }
    }
}