using SoltaLaPala.Interaction;
using UnityEngine;

namespace SoltaLaPala.Sabotaje
{
    // Objeto Taza / Cafetera interactuable que abre el menu de sabotaje.
    [RequireComponent(typeof(ResaltadoInteractuable))]
    public class SabotajeTazaInteractuable : MonoBehaviour, IInteractuable
    {
        public Transform Transform => transform;

        public string ObtenerTextoInteraccion()
        {
            return "[E] Sabotear Café";
        }

        public bool PuedeInteractuar()
        {
            if (MenuSabotajeTaza.Instancia != null && MenuSabotajeTaza.Instancia.panel != null)
            {
                return !MenuSabotajeTaza.Instancia.panel.activeSelf;
            }
            return true;
        }

        public void Interactuar(GameObject quienInteractua)
        {
            if (MenuSabotajeTaza.Instancia != null)
            {
                MenuSabotajeTaza.Instancia.Abrir(gameObject);
            }
            else
            {
                Debug.LogWarning("[SabotajeTazaInteractuable] No se encontró MenuSabotajeTaza en la escena.");
            }
        }
    }
}
