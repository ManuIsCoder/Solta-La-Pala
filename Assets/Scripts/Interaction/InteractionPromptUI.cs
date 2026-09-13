using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Interaction
{
    // Cartel abajo a la derecha, justo encima de los slots del inventario,
    // que dice de que forma se puede interactuar con el objeto que estas mirando.
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("Referencias")]
        public GameObject root;
        public Text promptLabel;

        // Muestra el cartel con el texto indicado (ej: "[E] Recoger pala").
        public void Show(string prompt)
        {
        }

        // Oculta el cartel cuando no hay ningun interactuable delante.
        public void Hide()
        {
        }
    }
}
