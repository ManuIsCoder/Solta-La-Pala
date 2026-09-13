using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Detecta el interactuable mas cercano delante del jugador, le pone el borde blanco,
    // muestra el prompt en pantalla y lanza la interaccion al pulsar la tecla.
    public class InteractorJugador : MonoBehaviour
    {
        [Header("Deteccion")]
        public float alcance = 3f;
        public float radio = 0.6f;
        public LayerMask capasInteractuables;
        public KeyCode teclaInteraccion = KeyCode.E;

        [Header("Referencias")]
        public Transform transformCamara;
        public InterfazTextoInteraccion interfazTexto;

        private IInteractuable objetivoActual;
        private bool interaccionBloqueada;

        // Cada frame: busca el mejor objetivo, actualiza highlight/prompt y lee la tecla de interaccion.
        // No hace nada si la interaccion esta bloqueada (ej: durante un dialogo).
        private void Update()
        {
        }

        // Lanza un SphereCast desde la camara y devuelve el IInteractuable valido mas cercano,
        // o null si no hay ninguno en rango.
        private IInteractuable BuscarMejorObjetivo()
        {
            return null;
        }

        // Cambia el objetivo actual: quita el borde blanco al anterior,
        // se lo pone al nuevo y actualiza el texto del prompt.
        private void EstablecerObjetivoActual(IInteractuable objetivo)
        {
        }

        // Llama a Interactuar() sobre el objetivo actual si existe y se puede interactuar.
        private void IntentarInteractuar()
        {
        }

        // Bloquea o desbloquea la deteccion de interactuables.
        // Lo usa el sistema de dialogo para que no puedas interactuar mientras hablas.
        public void BloquearInteraccion(bool bloqueado)
        {
        }
    }
}
