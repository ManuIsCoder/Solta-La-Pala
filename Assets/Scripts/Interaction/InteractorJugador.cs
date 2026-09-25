using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Detecta el interactuable mas cercano delante del jugador, le pone el borde blanco,
    // muestra el prompt en pantalla y lanza la interaccion al pulsar la tecla.
    public class InteractorJugador : MonoBehaviour
    {
        [Header("Deteccion")]
        public float alcance = 7f;
        public float radio = 0.6f;
        [Tooltip("Altura desde la que sale el rayo, sobre el origen del jugador. " +
                 "A la altura del pecho: desde los pies no llegaria a lo que hay " +
                 "encima de una mesa.")]
        public float alturaOrigen = 1.2f;
        public LayerMask capasInteractuables = ~0;

        [Header("Referencias")]
        public Transform transformCamara;
        public InterfazTextoInteraccion interfazTexto;

        private IInteractuable objetivoActual;
        private bool interaccionBloqueada;

        private void Awake()
        {
            if (transformCamara == null && Camera.main != null)
            {
                transformCamara = Camera.main.transform;
            }

            // Si no tiene asignado interfazTexto o el asignado no tiene el componente Text (ej: un GameObject vacio),
            // buscar el que si esta dentro del Canvas con el texto configurado.
            if (interfazTexto == null || interfazTexto.texto == null)
            {
                var todas = FindObjectsByType<InterfazTextoInteraccion>(FindObjectsSortMode.None);
                foreach (var candidata in todas)
                {
                    if (candidata.texto != null)
                    {
                        interfazTexto = candidata;
                        break;
                    }
                }
            }
        }

        // Cada frame: busca el mejor objetivo, actualiza highlight/prompt y lee la tecla de interaccion.
        // No hace nada si la interaccion esta bloqueada (ej: durante un dialogo).
        private void Update()
        {
            if (interaccionBloqueada)
            {
                if (objetivoActual != null) EstablecerObjetivoActual(null);
                return;
            }

            IInteractuable mejor = BuscarMejorObjetivo();
            if (mejor != objetivoActual)
            {
                EstablecerObjetivoActual(mejor);
            }

            // La tecla se lee desde ControlesJuego para que se pueda reasignar
            // desde el menu de configuracion.
            if (objetivoActual != null && ControlesJuego.Pulsada(AccionJuego.Interactuar))
            {
                IntentarInteractuar();
            }
        }

        // Busca el IInteractuable valido mas cercano delante del jugador, o null
        // si no hay ninguno en rango.
        //
        // El rayo apunta hacia donde mira la camara, pero sale del jugador y no de
        // la camara: en tercera persona la camara esta metros por detras, y medir
        // desde ahi gastaba el alcance en el hueco entre camara y PJ. Por eso solo
        // se podia interactuar en primera persona.
        private IInteractuable BuscarMejorObjetivo()
        {
            Vector3 origen = transform.position + Vector3.up * alturaOrigen;

            Vector3 direccion = transformCamara != null
                ? transformCamara.forward
                : transform.forward;

            RaycastHit[] hits = Physics.SphereCastAll(
                new Ray(origen, direccion), radio, alcance, capasInteractuables);

            IInteractuable mejor = null;
            float menorDistancia = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                IInteractuable interactuable = hit.collider.GetComponentInParent<IInteractuable>();
                if (interactuable != null && interactuable.PuedeInteractuar())
                {
                    // Distancia real al jugador: hit.distance mide desde el origen
                    // del rayo, y un objeto pegado al PJ pero a un lado podria dar
                    // una distancia enganosa.
                    float dist = Vector3.Distance(origen, hit.point);
                    if (dist < menorDistancia)
                    {
                        menorDistancia = dist;
                        mejor = interactuable;
                    }
                }
            }
            return mejor;
        }

        // Cambia el objetivo actual: quita el borde blanco al anterior,
        // se lo pone al nuevo y actualiza el texto del prompt.
        private void EstablecerObjetivoActual(IInteractuable objetivo)
        {
            if (objetivoActual != null)
            {
                var comp = objetivoActual as Component;
                if (comp != null)
                {
                    var resaltado = comp.GetComponent<ResaltadoInteractuable>();
                    if (resaltado != null) resaltado.Resaltar(false);
                }
            }

            objetivoActual = objetivo;

            if (objetivoActual != null)
            {
                var comp = objetivoActual as Component;
                if (comp != null)
                {
                    var resaltado = comp.GetComponent<ResaltadoInteractuable>();
                    if (resaltado != null) resaltado.Resaltar(true);
                }

                if (interfazTexto != null)
                {
                    interfazTexto.Mostrar(objetivoActual.ObtenerTextoInteraccion());
                }
            }
            else
            {
                if (interfazTexto != null)
                {
                    interfazTexto.Ocultar();
                }
            }
        }

        // Llama a Interactuar() sobre el objetivo actual si existe y se puede interactuar.
        private void IntentarInteractuar()
        {
            if (objetivoActual != null && objetivoActual.PuedeInteractuar())
            {
                objetivoActual.Interactuar(gameObject);
            }
        }

        // Bloquea o desbloquea la deteccion de interactuables.
        // Lo usa el sistema de dialogo para que no puedas interactuar mientras hablas.
        public void BloquearInteraccion(bool bloqueado)
        {
            interaccionBloqueada = bloqueado;
            if (bloqueado && objetivoActual != null)
            {
                EstablecerObjetivoActual(null);
            }
        }
    }
}
