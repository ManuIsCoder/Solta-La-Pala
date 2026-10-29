using SoltaLaPala.Dialogue;
using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.NPC
{
    [RequireComponent(typeof(DialogoNPC))]
    public class DetectorVisionNPC : MonoBehaviour
    {
        [Header("Cono de Vision")]
        public float distanciaVision = 10f;
        [Range(0f, 180f)]
        public float anguloVision = 70f;
        public LayerMask capasObstaculos = ~0;

        [Header("Ajustes")]
        public Transform ojos;
        public float tiempoEntreDetecciones = 5f;

        [Header("Alerta")]
        [Tooltip("Fraccion de distanciaVision por debajo de la cual se considera " +
                 "que el jugador esta 'muy cerca'. 0.4 = el 40% mas cercano del cono.")]
        [Range(0.05f, 1f)]
        public float fraccionDistanciaCercana = 0.4f;

        [Tooltip("Sospecha que suma cada vez que te ve de lejos. 0 lo desactiva.")]
        public int sospechaAlVerteLejos = 3;

        [Tooltip("Sospecha que suma cada vez que te ve de cerca.")]
        public int sospechaAlVerteCerca = 8;

        public enum EstadoVision
        {
            SinVer,
            VeLejos,
            VeCerca
        }

        private DialogoNPC dialogoNPC;
        private Transform transformJugador;
        private float ultimoTiempoDeteccion = -10f;

        public EstadoVision Estado { get; private set; } = EstadoVision.SinVer;

        private void Awake()
        {
            dialogoNPC = GetComponent<DialogoNPC>();
            if (ojos == null) ojos = transform;
            if (GetComponent<MiradaNPC>() == null)
            {
                gameObject.AddComponent<MiradaNPC>();
            }
        }

        private void Start()
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
            {
                transformJugador = jugador.transform;
            }
        }

        private void Update()
        {
                       if (transformJugador == null)
            {
                GameObject jugador = GameObject.FindGameObjectWithTag("Player");
                if (jugador != null) transformJugador = jugador.transform;
                Estado = EstadoVision.SinVer;
                return;
            }
            Estado = CalcularEstado();
        }

        private void SumarSospechaPorDeteccion()
        {
            int cantidad = Estado == EstadoVision.VeCerca
                ? sospechaAlVerteCerca
                : sospechaAlVerteLejos;

            if (cantidad <= 0 || EstadoPartida.Instancia == null)
            {
                return;
            }

            EstadoPartida.Instancia.RegistrarSabotaje(0, cantidad);
        }

        private EstadoVision CalcularEstado()
        {
            if (!PuedeVerJugador())
            {
                return EstadoVision.SinVer;
            }

            Vector3 origen = ojos.position + Vector3.up * 0.5f;
            Vector3 destino = transformJugador.position + Vector3.up * 0.5f;

            float distancia = Vector3.Distance(origen, destino);

            return distancia <= distanciaVision * fraccionDistanciaCercana
                ? EstadoVision.VeCerca
                : EstadoVision.VeLejos;
        }

        public bool PuedeVerJugador()
        {
            Vector3 origen = ojos.position + Vector3.up * 0.5f;
            Vector3 destino = transformJugador.position + Vector3.up * 0.5f;

            if (Vector3.Distance(origen, destino) > distanciaVision)
                return false;

            Vector3 direccion = (destino - origen).normalized;
            float angulo = Vector3.Angle(transform.forward, direccion);

            if (angulo > anguloVision / 2f)
                return false;

            if (Physics.Linecast(origen, destino, out RaycastHit hit, capasObstaculos))
            {
                if (hit.collider.CompareTag("Player") || hit.transform == transformJugador)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origen = (ojos != null ? ojos.position : transform.position) + Vector3.up * 0.5f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origen, distanciaVision);

            Vector3 forward = transform.forward;
            Vector3 dirIzq = Quaternion.Euler(0, -anguloVision / 2f, 0) * forward * distanciaVision;
            Vector3 dirDer = Quaternion.Euler(0, anguloVision / 2f, 0) * forward * distanciaVision;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(origen, dirIzq);
            Gizmos.DrawRay(origen, dirDer);
        }
    }
}
