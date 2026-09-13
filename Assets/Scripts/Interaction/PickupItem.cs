using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Objeto del mundo que se puede recoger y guardar en el inventario.
    [RequireComponent(typeof(InteractableHighlight))]
    public class PickupItem : MonoBehaviour, IInteractable
    {
        public ItemData item;

        [Tooltip("Altura del cartel de interaccion sobre el objeto.")]
        public Vector3 promptOffset = new Vector3(0f, 0.5f, 0f);

        public Transform Transform => transform;
        public Vector3 PromptOffset => promptOffset;

        // Devuelve "[E] Recoger {nombre del item}".
        public string GetInteractionPrompt()
        {
            return string.Empty;
        }

        // Solo se puede recoger si el inventario del jugador tiene algun slot libre.
        public bool CanInteract()
        {
            return false;
        }

        // Mete el item en el inventario del jugador y destruye este objeto del mundo.
        public void Interact(GameObject interactor)
        {
        }
    }
}
