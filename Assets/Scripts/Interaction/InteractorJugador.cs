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
        public LayerMask capasInteractuables = ~0;
        public KeyCode teclaInteraccion = KeyCode.E;

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

            if (objetivoActual != null && Input.GetKeyDown(teclaInteraccion))
            {
                IntentarInteractuar();
            }
        }

        // Lanza un SphereCast desde la camara y devuelve el IInteractuable valido mas cercano,
        // o null si no hay ninguno en rango.
        private IInteractuable BuscarMejorObjetivo()
        {
            Transform origen = transformCamara != null ? transformCamara : transform;
            Ray ray = new Ray(origen.position, origen.forward);

            RaycastHit[] hits = Physics.SphereCastAll(ray, radio, alcance, capasInteractuables);
            IInteractuable mejor = null;
            float menorDistancia = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                IInteractuable interactuable = hit.collider.GetComponentInParent<IInteractuable>();
                if (interactuable != null && interactuable.PuedeInteractuar())
                {
                    float dist = hit.distance;
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
