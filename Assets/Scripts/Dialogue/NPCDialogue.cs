using SoltaLaPala.Interaction;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    // Componente que va en cada NPC. Guarda que tipo de dialogo le toca decir ahora
    // y se encarga de la transicion entre tipos cuando una conversacion termina.
    // Es tambien el punto de interaccion del NPC ("[E] Hablar con ...").
    [RequireComponent(typeof(InteractableHighlight))]
    public class NPCDialogue : MonoBehaviour, IInteractable
    {
        [Header("Identidad")]
        [Tooltip("Nombre que sale en el cuadro de dialogo.")]
        public string npcName;
        [Tooltip("Nombre de la carpeta del NPC dentro de Resources/Nivel/Dialogos/.")]
        public string npcId;

        [Header("Estado")]
        [Tooltip("Tipo de dialogo con el que arranca el NPC en esta escena.")]
        public DialogueType currentType = DialogueType.Charla;

        public Transform Transform => transform;
        public string NpcName => npcName;
        public string NpcId => npcId;
        public DialogueType CurrentType => currentType;

        // Devuelve "[E] Hablar con {npcName}".
        public string GetInteractionPrompt()
        {
            return string.Empty;
        }

        // Se puede hablar si el NPC tiene alguna linea cargada y no hay otro dialogo activo.
        public bool CanInteract()
        {
            return false;
        }

        // Arranca la conversacion pidiendole al DialogueManager que muestre
        // el dialogo del tipo actual.
        public void Interact(GameObject interactor)
        {
        }

        // Fuerza el dialogo de tipo Detectado (lo llama el sistema de vision del NPC).
        // No cambia el tipo actual: al acabar se vuelve al que estaba.
        public void TriggerDetected()
        {
        }

        // Lo llama el DialogueManager cuando termina una conversacion.
        // Aplica la transicion de tipos:
        // - Mision  -> Charla si existe, si no Perpetuo
        // - Charla  -> Perpetuo
        // - Perpetuo -> se queda en Perpetuo (bucle)
        // - Detectado -> no cambia nada
        public void OnDialogueFinished(DialogueType finishedType)
        {
        }

        // Calcula a que tipo de dialogo hay que pasar despues del que acaba de terminar.
        private DialogueType GetNextType(DialogueType finishedType)
        {
            return DialogueType.Perpetuo;
        }
    }
}
