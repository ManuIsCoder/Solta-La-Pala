using SoltaLaPala.Dialogue;
using UnityEngine;
using UnityEngine.Rendering;

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

        [Header("Indicador visible")]
        [Tooltip("Pelota pegada a la cabeza que gira con ella, para que se vea en el juego " +
                 "hacia donde mira el NPC. Roja cuando te sigue, blanca cuando mira al frente.")]
        public bool mostrarIndicador = true;

        [Tooltip("Donde flota la pelota respecto a la cabeza. Z = hacia adelante.")]
        public Vector3 offsetIndicador = new Vector3(0f, 0f, 0.6f);

        [Tooltip("Diametro de la pelota.")]
        public float tamanoIndicador = 0.3f;

        public Color colorSiguiendo = new Color(0.9f, 0.15f, 0.15f, 1f);
        public Color colorAlFrente = Color.white;

        [Header("Gizmos")]
        public bool dibujarGizmos = true;

        private DetectorVisionNPC detector;
        private DialogoNPC dialogoNPC;
        private Transform transformJugador;
        private Quaternion rotacionLocalInicial = Quaternion.identity;
        private bool estaSiguiendo = false;
        private Renderer renderIndicador;

        // Ultimo color pintado, para no tocar el material cada frame.
        private bool? indicadorRojo;

        private void Awake()
        {
            detector = GetComponent<DetectorVisionNPC>();
            dialogoNPC = GetComponent<DialogoNPC>();
            AsegurarCabeza();
            CrearIndicador();
        }

        // Crea la pelota como hija de la cabeza. Sin collider ni sombra: es solo una
        // marca visual, no debe empujar al jugador ni ser apuntable.
        private void CrearIndicador()
        {
            if (!mostrarIndicador || cabeza == null)
            {
                return;
            }

            GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            esfera.name = "IndicadorMirada";

            Collider colisionador = esfera.GetComponent<Collider>();
            if (colisionador != null)
            {
                Destroy(colisionador);
            }

            // El tamano es en metros: se compensa la escala del NPC para que
            // no salga gigante o diminuta segun el modelo.
            esfera.transform.SetParent(cabeza, false);
            Vector3 escalaPadre = cabeza.lossyScale;
            esfera.transform.localScale = new Vector3(
                tamanoIndicador / Mathf.Max(escalaPadre.x, 0.0001f),
                tamanoIndicador / Mathf.Max(escalaPadre.y, 0.0001f),
                tamanoIndicador / Mathf.Max(escalaPadre.z, 0.0001f));
            esfera.transform.localPosition = new Vector3(
                offsetIndicador.x / Mathf.Max(escalaPadre.x, 0.0001f),
                offsetIndicador.y / Mathf.Max(escalaPadre.y, 0.0001f),
                offsetIndicador.z / Mathf.Max(escalaPadre.z, 0.0001f));

            renderIndicador = esfera.GetComponent<Renderer>();
            renderIndicador.shadowCastingMode = ShadowCastingMode.Off;
            renderIndicador.receiveShadows = false;

            PintarIndicador(false);
        }

        // Pinta la pelota solo cuando cambia el estado. El material por defecto de
        // la esfera ya es el del pipeline del proyecto, asi que no hace falta
        // buscar un shader a mano.
        private void PintarIndicador(bool siguiendo)
        {
            if (renderIndicador == null || indicadorRojo == siguiendo)
            {
                return;
            }

            indicadorRojo = siguiendo;
            renderIndicador.material.color = siguiendo ? colorSiguiendo : colorAlFrente;
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

            // Si el jugador esta hablando con ESTE NPC, lo mira siempre, sin importar
            // desde donde le hable ni a que distancia: es su interlocutor.
            bool hablandoConmigo = EstaHablandoConElJugador();

            estaSiguiendo = hablandoConmigo;

            if (!hablandoConmigo && distancia <= distanciaEfectiva)
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

            PintarIndicador(estaSiguiendo);

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

            // Giro suave. Durante un dialogo el juego esta congelado (timeScale 0) y
            // Time.deltaTime vale 0: la cabeza no giraria hacia el jugador.
            float dt = hablandoConmigo ? Time.unscaledDeltaTime : Time.deltaTime;
            cabeza.localRotation = Quaternion.RotateTowards(cabeza.localRotation, rotacionObjetivo, velocidadGiro * dt);
        }

        // True si hay un dialogo en marcha y es con este NPC.
        private bool EstaHablandoConElJugador()
        {
            GestorDialogos gestor = GestorDialogos.Instancia;

            return dialogoNPC != null
                && gestor != null
                && gestor.DialogoActivo
                && gestor.NpcActual == dialogoNPC;
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