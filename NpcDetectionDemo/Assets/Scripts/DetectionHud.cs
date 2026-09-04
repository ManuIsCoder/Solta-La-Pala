using UnityEngine;

public class DetectionHud : MonoBehaviour
{
    void OnGUI()
    {
        Npc[] npcs = FindObjectsByType<Npc>(FindObjectsSortMode.None);
        bool visto = false;
        for (int i = 0; i < npcs.Length; i++)
        {
            if (npcs[i].EstaViendoJugador)
            {
                visto = true;
                break;
            }
        }

        GUI.color = visto ? Color.red : Color.white;
        GUI.Label(new Rect(20f, 20f, 300f, 40f), visto ? "DETECTADO" : "Oculto");
    }
}
