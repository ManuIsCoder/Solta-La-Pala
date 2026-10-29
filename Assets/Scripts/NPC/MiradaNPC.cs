using SoltaLaPala.Dialogue;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoltaLaPala.NPC
{
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

        [Tooltip("Distancia máxima hasta la que el NPC te sigue con la mirada, en metros. " +
                 "Si es 0, usa la misma distancia en la que el ojo se pone rojo.")]
        public float distanciaMirada = 2.5f;

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

        private bool? indicadorRojo;

        private void Awake()
        {
            detector = GetComponent<DetectorVisionNPC>();
            dialogoNPC = GetComponent<DialogoNPC>();
            AsegurarCabeza();
            CrearIndicador();
        }

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

            GameObject pivote = new GameObject("Cabeza");
            pivote.transform.SetParent(transform, false);
            pivote.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            pivote.transform.localRotation = Quaternion.identity;
            cabeza = pivote.transform;

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

            float distanciaEfectiva = distanciaMirada > 0f
                ? distanciaMirada
                : (detector != null ? detector.distanciaVision * detector.fraccionDistanciaCercana : 4f);

            Vector3 origen = cabeza.position;
            Vector3 destino = transformJugador.position + Vector3.up * 0.5f;
            Vector3 direccionHaciaJugador = destino - origen;
            float distancia = direccionHaciaJugador.magnitude;

            bool hablandoConmigo = EstaHablandoConElJugador();

            estaSiguiendo = hablandoConmigo;

            if (!hablandoConmigo && distancia <= distanciaEfectiva)
            {
                Vector3 direccionPlana = new Vector3(direccionHaciaJugador.x, 0f, direccionHaciaJugador.z);
                float angulo = Vector3.Angle(transform.forward, direccionPlana);

                if (angulo <= anguloMirada / 2f)
                {
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
                Quaternion rotacionMundo = Quaternion.LookRotation(direccionHaciaJugador, Vector3.up);
                rotacionObjetivo = Quaternion.Inverse(transform.rotation) * rotacionMundo;
            }
            else
            {
                rotacionObjetivo = rotacionLocalInicial;
            }

            float dt = hablandoConmigo ? Time.unscaledDeltaTime : Time.deltaTime;
            cabeza.localRotation = Quaternion.RotateTowards(cabeza.localRotation, rotacionObjetivo, velocidadGiro * dt);
        }

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
