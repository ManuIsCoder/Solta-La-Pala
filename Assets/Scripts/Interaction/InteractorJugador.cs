using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.Interaction
{
    public class InteractorJugador : MonoBehaviour
    {
        [Header("Deteccion")]
        public float alcance = 7f;
        public float radio = 0.6f;
        [Tooltip("Altura desde la que sale el rayo, sobre el origen del jugador.")]
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

            if (objetivoActual != null && ControlesJuego.Pulsada(AccionJuego.Interactuar))
            {
                IntentarInteractuar();
            }
        }

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
                if (interactuable != null
                    && EstaDentroDelAlcance(interactuable)
                    && interactuable.PuedeInteractuar())
                {
                    float dist = Vector3.Distance(transform.position, interactuable.Transform.position);
                    if (dist < menorDistancia)
                    {
                        menorDistancia = dist;
                        mejor = interactuable;
                    }
                }
            }
            return mejor;
        }

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

        private void IntentarInteractuar()
        {
            if (objetivoActual != null
                && EstaDentroDelAlcance(objetivoActual)
                && objetivoActual.PuedeInteractuar())
            {
                objetivoActual.Interactuar(gameObject);
            }
        }

        private bool EstaDentroDelAlcance(IInteractuable objetivo)
        {
            return objetivo != null
                && objetivo.Transform != null
                && Vector3.Distance(transform.position, objetivo.Transform.position) <= alcance;
        }

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
