using UnityEngine;

namespace SoltaLaPala.NPC
{
    // Hace que el NPC gire su cabeza hacia el jugador cuando este se encuentra
    // cerca y dentro de su angulo frontal (~60 grados), sin rotar el cuerpo.
    [RequireComponent(typeof(DetectorVisionNPC))]
    public class MiradaNPC : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Transform que rota (cabeza/ojos). Si está vacío, busca o crea el pivote automáticamente.")]
        public Transform cabeza;

        [Header("Ajustes de Mirada")]
        [Tooltip("Ángulo frontal total (en grados) dentro del cual el NPC seguirá al jugador.")]
        [Range(20f, 120f)]
        public float anguloMirada = 60f;

        [Tooltip("Distancia máxima de mirada. Si es 0, usa la misma distancia en la que el ojo se pone rojo.")]
        public float distanciaMirada = 0f;

        [Tooltip("Velocidad de giro de la cabeza en grados por segundo.")]
        public float velocidadGiro = 240f;

        [Header("Gizmos")]
        public bool dibujarGizmos = true;

        private DetectorVisionNPC detector;
        private Transform transformJugador;
        private Quaternion rotacionLocalInicial = Quaternion.identity;
        private bool estaSiguiendo = false;

        private void Awake()
        {
            detector = GetComponent<DetectorVisionNPC>();
            AsegurarCabeza();
        }

        private void Start()
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
            {
                transformJugador = jugador.transform;
            }
        }

        // Si no hay cabeza asignada, busca 'Cabeza', o crea el pivote y le emparenta 'Frente'
        private void AsegurarCabeza()
        {
            if (cabeza != null)
            {
                rotacionLocalInicial = cabeza.localRotation;
                return;
            }

            Transform hijoCabeza = transform.Find("Cabeza");
            if (hijoCabeza != null)
            {
                cabeza = hijoCabeza;
                rotacionLocalInicial = cabeza.localRotation;
                return;
            }

            // Crear pivote en la parte superior del NPC
            GameObject pivote = new GameObject("Cabeza");
            pivote.transform.SetParent(transform, false);
            pivote.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            pivote.transform.localRotation = Quaternion.identity;
            cabeza = pivote.transform;

            // Si el NPC tiene el cubo 'Frente', emparentarlo a la Cabeza para que gire con ella
            Transform hijoFrente = transform.Find("Frente");
            if (hijoFrente != null)
            {
                hijoFrente.SetParent(cabeza, true);
            }

            rotacionLocalInicial = Quaternion.identity;
        }

        private void LateUpdate()
        {
            if (cabeza == null) return;

            if (transformJugador == null)
            {
                GameObject jugador = GameObject.FindGameObjectWithTag("Player");
                if (jugador != null) transformJugador = jugador.transform;
                VolverAlFrente();
                return;
            }

            // Distancia de activación: por defecto, la misma distancia del ojo rojo
            float distanciaEfectiva = distanciaMirada > 0f
                ? distanciaMirada
                : (detector != null ? detector.distanciaVision * detector.fraccionDistanciaCercana : 4f);

            Vector3 origen = cabeza.position;
            Vector3 destino = transformJugador.position + Vector3.up * 0.5f;
            Vector3 direccionHaciaJugador = destino - origen;
            float distancia = direccionHaciaJugador.magnitude;

            estaSiguiendo = false;

            if (distancia <= distanciaEfectiva)
            {
                // Comprobar ángulo frontal horizontal
                Vector3 direccionPlana = new Vector3(direccionHaciaJugador.x, 0f, direccionHaciaJugador.z);
                float angulo = Vector3.Angle(transform.forward, direccionPlana);

                if (angulo <= anguloMirada / 2f)
                {
                    // Comprobar que no haya un muro tapando
                    if (detector == null || detector.PuedeVerJugador())
                    {
                        estaSiguiendo = true;
                    }
                }
            }

            Quaternion rotacionObjetivo;

            if (estaSiguiendo)
            {
                // Rotación relativa al cuerpo del NPC
                Quaternion rotacionMundo = Quaternion.LookRotation(direccionHaciaJugador, Vector3.up);
                rotacionObjetivo = Quaternion.Inverse(transform.rotation) * rotacionMundo;
            }
            else
            {
                // Mirar al frente
                rotacionObjetivo = rotacionLocalInicial;
            }

            // Giro suave
            cabeza.localRotation = Quaternion.RotateTowards(cabeza.localRotation, rotacionObjetivo, velocidadGiro * Time.deltaTime);
        }

        private void VolverAlFrente()
        {
            if (cabeza != null)
            {
                cabeza.localRotation = Quaternion.RotateTowards(cabeza.localRotation, rotacionLocalInicial, velocidadGiro * Time.deltaTime);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!dibujarGizmos) return;
            Transform origen = cabeza != null ? cabeza : transform;
            Gizmos.color = estaSiguiendo ? Color.red : Color.green;
            Gizmos.DrawRay(origen.position, origen.forward * 2f);
        }
    }
}