using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Dialogue
{
    // Cuadro de dialogo: rectangulo con el nombre del NPC arriba
    // y el texto de la frase escribiendose letra a letra.
    public class InterfazDialogo : MonoBehaviour
    {
        [Header("Referencias")]
        public GameObject panel;
        public Text textoNombre;
        public Text textoDialogo;
        [Tooltip("Flechita o icono que indica que se puede pasar con espacio.")]
        public GameObject indicadorContinuar;

        [Header("Ajustes")]
        public float letrasPorSegundo = 40f;

        private Coroutine rutinaEscritura;

        // True mientras el texto se esta escribiendo letra a letra.
        public bool Escribiendo { get; private set; }

        // Abre el cuadro de dialogo y pone el nombre del NPC.
        public void Abrir(string nombreNpc)
        {
        }

        // Cierra el cuadro y limpia el texto.
        public void Cerrar()
        {
        }

        // Empieza a escribir una linea letra a letra.
        public void MostrarLinea(string linea)
        {
        }

        // Corta la animacion de escritura y muestra la linea entera de golpe.
        // Es lo que pasa al pulsar espacio a media frase.
        public void CompletarLinea()
        {
        }

        // Corrutina que va anadiendo caracteres al texto segun letrasPorSegundo.
        private IEnumerator EscribirLinea(string linea)
        {
            yield break;
        }
    }
}
