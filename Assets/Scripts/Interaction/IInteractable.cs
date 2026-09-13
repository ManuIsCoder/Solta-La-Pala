using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Contrato que implementa cualquier cosa con la que el jugador pueda interactuar
    // (objetos del suelo, puertas, NPCs...).
    public interface IInteractable
    {
        // Transform del objeto, para calcular distancia y posicionar el prompt.
        Transform Transform { get; }

        // Texto que se muestra abajo a la derecha, encima de los slots del inventario.
        // Ej: "[E] Recoger pala", "[E] Hablar con Manolo".
        string GetInteractionPrompt();

        // True si ahora mismo se puede interactuar (ej: un cofre ya abierto devolveria false).
        bool CanInteract();

        // Ejecuta la interaccion. Lo llama PlayerInteractor cuando el jugador pulsa la tecla.
        void Interact(GameObject interactor);
    }
}
