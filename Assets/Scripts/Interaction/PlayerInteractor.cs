using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Detecta el interactuable mas cercano delante del jugador, le pone el borde blanco,
    // muestra el prompt en pantalla y lanza la interaccion al pulsar la tecla.
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Deteccion")]
        public float interactionRange = 3f;
        public float interactionRadius = 0.6f;
        public LayerMask interactableLayers;
        public KeyCode interactKey = KeyCode.E;

        [Header("Referencias")]
        public Transform cameraTransform;
        public InteractionPromptUI promptUI;

        private IInteractable currentTarget;
        private bool interactionLocked;

        // Cada frame: busca el mejor objetivo, actualiza highlight/prompt y lee la tecla de interaccion.
        // No hace nada si la interaccion esta bloqueada (ej: durante un dialogo).
        private void Update()
        {
        }

        // Lanza un SphereCast desde la camara y devuelve el IInteractable valido mas cercano,
        // o null si no hay ninguno en rango.
        private IInteractable FindBestTarget()
        {
            return null;
        }

        // Cambia el objetivo actual: quita el borde blanco al anterior,
        // se lo pone al nuevo y actualiza el texto del prompt.
        private void SetCurrentTarget(IInteractable target)
        {
        }

        // Llama a Interact() sobre el objetivo actual si existe y se puede interactuar.
        private void TryInteract()
        {
        }

        // Bloquea o desbloquea la deteccion de interactuables.
        // Lo usa el sistema de dialogo para que no puedas interactuar mientras hablas.
        public void SetInteractionLocked(bool locked)
        {
        }
    }
}
