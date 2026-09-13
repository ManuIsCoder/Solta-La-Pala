using SoltaLaPala.Dialogue;
using UnityEngine;

namespace SoltaLaPala.NPC
{
    // Sistema de vision para NPCs vigilantes.
    // Si el jugador entra en su cono de vision y no hay muros tapandolo, dispara el dialogo Detectado.
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

        private DialogoNPC dialogoNPC;
        private Transform transformJugador;
        private float ultimoTiempoDeteccion = -10f;

        private void Awake()
        {
            dialogoNPC = GetComponent<DialogoNPC>();
            if (ojos == null) ojos = transform;
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
                return;
            }

            if (Time.time < ultimoTiempoDeteccion + tiempoEntreDetecciones) return;
            if (GestorDialogos.Instancia != null && GestorDialogos.Instancia.DialogoActivo) return;

            if (PuedeVerJugador())
            {
                ultimoTiempoDeteccion = Time.time;
                if (dialogoNPC != null)
                {
                    dialogoNPC.DispararDetectado();
                }
            }
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

            // Linecast / Raycast: si choca con un muro antes de llegar al jugador, no lo ve
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
