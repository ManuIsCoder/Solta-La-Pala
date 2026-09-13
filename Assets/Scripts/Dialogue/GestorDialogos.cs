using System.Collections.Generic;
using SoltaLaPala.Interaction;
using SoltaLaPala.Player;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    // Controla la conversacion activa: carga las lineas, las va pasando
    // y bloquea al jugador mientras dura el dialogo.
    // Con ESPACIO se completa la linea que se esta escribiendo o se pasa a la siguiente.
    public class GestorDialogos : MonoBehaviour
    {
        public static GestorDialogos Instancia { get; private set; }

        [Header("Referencias")]
        public InterfazDialogo interfaz;
        public MovimientoJugador movimientoJugador;
        public CamaraTerceraPersona camaraJugador;
        public InteractorJugador interactorJugador;

        [Header("Ajustes")]
        public KeyCode teclaAvanzar = KeyCode.Space;

        private DialogoNPC npcActual;
        private TipoDialogo tipoActual;
        private List<string> lineasActuales;
        private int indiceLinea;

        // True mientras hay una conversacion en marcha.
        public bool DialogoActivo { get; private set; }

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
        public void EmpezarDialogo(DialogoNPC npc, TipoDialogo tipo)
        {
        }

        // Avanza a la siguiente linea. Si la linea actual todavia se esta escribiendo,
        // la completa de golpe en vez de pasar. Si no quedan lineas, termina la conversacion.
        private void AvanzarDialogo()
        {
        }

        // Manda la linea indicada a la interfaz para que la escriba.
        private void MostrarLinea(int indice)
        {
        }

        // Cierra el cuadro, devuelve el control al jugador y avisa al NPC
        // para que aplique la transicion de tipo de dialogo.
        private void TerminarDialogo()
        {
        }

        // Bloquea o desbloquea movimiento, camara e interaccion del jugador,
        // y libera el cursor del raton mientras dura el dialogo.
        private void BloquearJugador(bool bloqueado)
        {
        }
    }
}
