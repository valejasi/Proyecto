using UnityEngine;

//muestra info de la sala, session, estado del server, etc

public partial class Servidor
{
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 1000, 25), "Host: C = Create | Cliente: escribí código (6) + Enter = Join | Colocar PORTA: K");
        GUI.Label(new Rect(10, 35, 900, 25), "Código tipeado: " + codigoIngresado);

        GUI.Label(new Rect(10, 60, 900, 25), "Sala: " + (string.IsNullOrWhiteSpace(codigoSala) ? "(ninguna)" : codigoSala));
        GUI.Label(new Rect(10, 85, 900, 25), "SessionId: " + (string.IsNullOrWhiteSpace(miSessionId) ? "(sin asignar)" : miSessionId));
        GUI.Label(new Rect(10, 110, 900, 25), "Slot: " + (miSlot == 0 ? "(no asignado)" : miSlot.ToString()));
        GUI.Label(new Rect(10, 135, 900, 25), "PORTA enviado: " + portaEnviada);

        if (lastState != null && lastState.vidas != null)
        {
            int y = 165;
            GUI.Label(new Rect(10, y, 900, 25), "VIDAS (server):");
            y += 20;

            for (int i = 0; i < lastState.vidas.Length; i++)
            {
                var v = lastState.vidas[i];
                if (v.sessionId != miSessionId) continue;
                GUI.Label(new Rect(10, y, 900, 20), $"{v.objId}: {v.vida}");
                y += 18;
            }
        }
    }
}