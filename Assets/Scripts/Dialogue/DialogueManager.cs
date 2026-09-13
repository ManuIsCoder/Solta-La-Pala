using System.Collections.Generic;
using SoltaLaPala.Interaction;
using SoltaLaPala.Player;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    // Controla la conversacion activa: carga las lineas, las va pasando
    // y bloquea al jugador mientras dura el dialogo.
    // Con ESPACIO se completa la linea que se esta escribiendo o se pasa a la siguiente.
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Referencias")]
        public DialogueUI dialogueUI;
        public PlayerMovement playerMovement;
        public ThirdPersonCamera playerCamera;
        public PlayerInteractor playerInteractor;

        [Header("Ajustes")]
        public KeyCode advanceKey = KeyCode.Space;

        private NPCDialogue currentNpc;
        private DialogueType currentType;
        private List<string> currentLines;
        private int currentLineIndex;

        // True mientras hay una conversacion en marcha.
        public bool IsDialogueActive { get; private set; }

        // Registra el singleton.
        private void Awake()
        {
        }

        // Si hay dialogo activo, lee la tecla de avance (espacio) para saltar/avanzar.
        private void Update()
        {
        }

        // Empieza una conversacion: carga las lineas del NPC para ese tipo,
        // bloquea movimiento e interaccion, abre el cuadro y muestra la primera linea.
        public void StartDialogue(NPCDialogue npc, DialogueType type)
        {
        }

        // Avanza a la siguiente linea. Si la linea actual todavia se esta escribiendo,
        // la completa de golpe en vez de pasar. Si no quedan lineas, termina la conversacion.
        private void AdvanceDialogue()
        {
        }

        // Manda la linea indicada a la UI para que la escriba.
        private void ShowLine(int lineIndex)
        {
        }

        // Cierra el cuadro, devuelve el control al jugador y avisa al NPC
        // para que aplique la transicion de tipo de dialogo.
        private void EndDialogue()
        {
        }

        // Bloquea o desbloquea movimiento, camara e interaccion del jugador,
        // y libera el cursor del raton mientras dura el dialogo.
        private void SetPlayerLocked(bool locked)
        {
        }
    }
}
