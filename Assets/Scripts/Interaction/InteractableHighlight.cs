using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Pinta un borde blanco en el contorno del objeto cuando el jugador lo esta mirando.
    // Se hace duplicando el material con un shader de outline (o activando su keyword).
    public class InteractableHighlight : MonoBehaviour
    {
        [Header("Outline")]
        public Color outlineColor = Color.white;
        public float outlineWidth = 2f;
        [Tooltip("Si se deja vacio se cogen todos los Renderer hijos al arrancar.")]
        public Renderer[] targetRenderers;

        private bool isHighlighted;

        // Recoge los renderers y prepara los materiales de outline (instanciandolos para no tocar el asset).
        private void Awake()
        {
        }

        // Enciende o apaga el borde blanco.
        public void SetHighlighted(bool highlighted)
        {
        }

        // Aplica color y grosor del borde a los materiales de outline.
        private void ApplyOutlineSettings()
        {
        }
    }
}
