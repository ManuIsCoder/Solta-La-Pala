using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Dialogue
{
    // Cuadro de dialogo: rectangulo con el nombre del NPC arriba
    // y el texto de la frase escribiendose letra a letra.
    public class DialogueUI : MonoBehaviour
    {
        [Header("Referencias")]
        public GameObject panelRoot;
        public Text nameLabel;
        public Text dialogueLabel;
        [Tooltip("Flechita o icono que indica que se puede pasar con espacio.")]
        public GameObject continueIndicator;

        [Header("Ajustes")]
        public float charactersPerSecond = 40f;

        private Coroutine typingRoutine;

        // True mientras el texto se esta escribiendo letra a letra.
        public bool IsTyping { get; private set; }

        // Abre el cuadro de dialogo y pone el nombre del NPC.
        public void Open(string npcName)
        {
        }

        // Cierra el cuadro y limpia el texto.
        public void Close()
        {
        }

        // Empieza a escribir una linea letra a letra.
        public void ShowLine(string line)
        {
        }

        // Corta la animacion de escritura y muestra la linea entera de golpe.
        // Es lo que pasa al pulsar espacio a media frase.
        public void CompleteLine()
        {
        }

        // Corrutina que va anadiendo caracteres al label segun charactersPerSecond.
        private IEnumerator TypeLine(string line)
        {
            yield break;
        }
    }
}
