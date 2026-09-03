using UnityEngine;

public class DetectionHud : MonoBehaviour
{
    void OnGUI()
    {
        Guard[] guards = FindObjectsByType<Guard>(FindObjectsSortMode.None);
        bool seen = false;
        for (int i = 0; i < guards.Length; i++)
        {
            if (guards[i].IsSeeingPlayer)
            {
                seen = true;
                break;
            }
        }

        GUI.color = seen ? Color.red : Color.white;
        GUI.Label(new Rect(20f, 20f, 300f, 40f), seen ? "DETECTADO" : "Oculto");
    }
}
