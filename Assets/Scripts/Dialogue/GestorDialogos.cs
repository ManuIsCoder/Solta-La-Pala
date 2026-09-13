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
        private List<string> lineasActuales = new List<string>();
        private int indiceLinea;

        // True mientras hay una conversacion en marcha.
        public bool DialogoActivo { get; private set; }

        // Registra el singleton y busca referencias si no fueron asignadas.
        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            if (interfaz == null)
                interfaz = FindFirstObjectByType<InterfazDialogo>();
            if (movimientoJugador == null)
                movimientoJugador = FindFirstObjectByType<MovimientoJugador>();
            if (camaraJugador == null)
                camaraJugador = FindFirstObjectByType<CamaraTerceraPersona>();
            if (interactorJugador == null)
                interactorJugador = FindFirstObjectByType<InteractorJugador>();
        }

        // Si hay dialogo activo, lee la tecla de avance (espacio) para saltar/avanzar.
        private void Update()
        {
            if (!DialogoActivo) return;

            if (Input.GetKeyDown(teclaAvanzar))
            {
                AvanzarDialogo();
            }
        }

        // Empieza una conversacion: carga las lineas del NPC para ese tipo,
        // bloquea movimiento e interaccion, abre el cuadro y muestra la primera linea.
        public void EmpezarDialogo(DialogoNPC npc, TipoDialogo tipo)
        {
            if (DialogoActivo || npc == null) return;

            npcActual = npc;
            tipoActual = tipo;

            lineasActuales = CargadorDialogos.CargarLineas(npc.id, tipo);
            if (lineasActuales == null || lineasActuales.Count == 0)
            {
                // Si no hay líneas para ese tipo, intentar caer a Perpetuo si no es Perpetuo
                if (tipo != TipoDialogo.Perpetuo)
                {
                    tipoActual = TipoDialogo.Perpetuo;
                    lineasActuales = CargadorDialogos.CargarLineas(npc.id, TipoDialogo.Perpetuo);
                }

                if (lineasActuales == null || lineasActuales.Count == 0)
                {
                    Debug.LogWarning($"[GestorDialogos] El NPC {npc.nombre} ({npc.id}) no tiene líneas de diálogo disponibles.");
                    return;
                }
            }

            DialogoActivo = true;
            indiceLinea = 0;

            BloquearJugador(true);

            if (interfaz != null)
            {
                interfaz.Abrir(npc.nombre);
            }

            MostrarLinea(indiceLinea);
        }

        // Avanza a la siguiente linea. Si la linea actual todavia se esta escribiendo,
        // la completa de golpe en vez de pasar. Si no quedan lineas, termina la conversacion.
        private void AvanzarDialogo()
        {
            if (interfaz != null && interfaz.Escribiendo)
            {
                interfaz.CompletarLinea();
                return;
            }

            indiceLinea++;
            if (indiceLinea < lineasActuales.Count)
            {
                MostrarLinea(indiceLinea);
            }
            else
            {
                TerminarDialogo();
            }
        }

        // Manda la linea indicada a la interfaz para que la escriba.
        private void MostrarLinea(int indice)
        {
            if (indice >= 0 && indice < lineasActuales.Count && interfaz != null)
            {
                interfaz.MostrarLinea(lineasActuales[indice]);
            }
        }

        // Cierra el cuadro, devuelve el control al jugador y avisa al NPC
        // para que aplique la transicion de tipo de dialogo.
        private void TerminarDialogo()
        {
            DialogoActivo = false;

            if (interfaz != null)
            {
                interfaz.Cerrar();
            }

            BloquearJugador(false);

            if (npcActual != null)
            {
                npcActual.AlTerminarDialogo(tipoActual);
                npcActual = null;
            }
        }

        // Bloquea o desbloquea movimiento, camara e interaccion del jugador,
        // y libera el cursor del raton mientras dura el dialogo.
        private void BloquearJugador(bool bloqueado)
        {
            if (movimientoJugador != null)
            {
                movimientoJugador.BloquearMovimiento(bloqueado);
            }

            if (interactorJugador != null)
            {
                interactorJugador.BloquearInteraccion(bloqueado);
            }

            Cursor.lockState = bloqueado ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = bloqueado;
        }
    }
}
