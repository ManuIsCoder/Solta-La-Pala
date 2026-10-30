using SoltaLaPala.Dialogue;
using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.NPC
{
    [RequireComponent(typeof(DetectorVisionNPC))]
    public class PatrullaNPC : MonoBehaviour
    {
        [Header("Ruta")]
        [Tooltip("Recorrido a seguir. Vacio = el NPC se queda fijo en su sitio.")]
        public RutaNPC ruta;

        [Header("Movimiento")]
        public float velocidad = 1.8f;
        [Tooltip("Grados por segundo al girar hacia el siguiente punto.")]
        public float velocidadGiro = 360f;
        [Tooltip("A que distancia de la parada se considera que ya llego.")]
        public float distanciaLlegada = 0.15f;

        [Header("Esperas")]
        [Tooltip("Segundos parado en cada punto antes de seguir.")]
        public float esperaEnPunto = 1f;
        [Tooltip("Segundos que sigue mirandote despues de perderte de vista, " +
                 "antes de retomar la ruta.")]
        public float esperaTrasPerderte = 2f;

        [Header("Vigilancia")]
        [Tooltip("Velocidad de giro al mirar al jugador.")]
        public float velocidadGiroVigilando = 540f;

        private DetectorVisionNPC detector;
        private Transform transformJugador;

        private int indiceDestino;
        private int sentido = 1;

        private float esperaRestante;
        private float vigilanciaRestante;

        public bool Vigilando { get; private set; }

        private Vector3 posicionInicial;
        private Quaternion rotacionInicial;

        private void Awake()
        {
            detector = GetComponent<DetectorVisionNPC>();

            posicionInicial = transform.position;
            rotacionInicial = transform.rotation;
        }

        public void Reiniciar()
        {
            transform.SetPositionAndRotation(posicionInicial, rotacionInicial);

            Vigilando = false;
            vigilanciaRestante = 0f;
            esperaRestante = 0f;
            sentido = 1;
            indiceDestino = BuscarPuntoMasCercano();
        }

        private void Start()
        {
            BuscarJugador();

            indiceDestino = BuscarPuntoMasCercano();
        }

        private void Update()
        {
            if (GestorMenus.Instancia != null && GestorMenus.Instancia.HayMenuAbierto)
            {
                return;
            }

            if (transformJugador == null)
            {
                BuscarJugador();
            }

            if (HablandoConmigo())
            {
                MirarAlJugador();
                return;
            }

            ActualizarVigilancia();

            if (Vigilando)
            {
                MirarAlJugador();
                return;
            }

            Patrullar();
        }

        private void ActualizarVigilancia()
        {
            bool teVe = detector.Estado != DetectorVisionNPC.EstadoVision.SinVer;

            if (teVe)
            {
                Vigilando = true;
                vigilanciaRestante = esperaTrasPerderte;
                return;
            }

            if (!Vigilando)
            {
                return;
            }

            vigilanciaRestante -= Time.deltaTime;

            if (vigilanciaRestante <= 0f)
            {
                Vigilando = false;

                esperaRestante = 0f;
            }
        }

        private void MirarAlJugador()
        {
            if (transformJugador == null)
            {
                return;
            }

            Vector3 haciaJugador = transformJugador.position - transform.position;

            haciaJugador.y = 0f;

            GirarHacia(haciaJugador, velocidadGiroVigilando);
        }

        private void Patrullar()
        {
            if (ruta == null || ruta.CantidadPuntos == 0)
            {
                return;
            }

            if (esperaRestante > 0f)
            {
                esperaRestante -= Time.deltaTime;
                return;
            }

            Vector3 destino = ruta.ObtenerPosicion(indiceDestino);

            destino.y = transform.position.y;

            Vector3 haciaDestino = destino - transform.position;

            if (haciaDestino.sqrMagnitude <= distanciaLlegada * distanciaLlegada)
            {
                indiceDestino = ruta.SiguienteIndice(indiceDestino, ref sentido);
                esperaRestante = esperaEnPunto;
                return;
            }

            GirarHacia(haciaDestino, velocidadGiro);

            transform.position = Vector3.MoveTowards(
                transform.position, destino, velocidad * Time.deltaTime);
        }

        private void GirarHacia(Vector3 direccion, float gradosPorSegundo)
        {
            direccion.y = 0f;

            if (direccion.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direccion),
                gradosPorSegundo * Time.deltaTime);
        }

        private int BuscarPuntoMasCercano()
        {
            if (ruta == null || ruta.CantidadPuntos == 0)
            {
                return 0;
            }

            int mejor = 0;
            float menorDistancia = float.MaxValue;

            for (int i = 0; i < ruta.CantidadPuntos; i++)
            {
                float distancia = (ruta.ObtenerPosicion(i) - transform.position).sqrMagnitude;

                if (distancia < menorDistancia)
                {
                    menorDistancia = distancia;
                    mejor = i;
                }
            }

            return mejor;
        }

        private bool HablandoConmigo()
        {
            GestorDialogos gestor = GestorDialogos.Instancia;

            return gestor != null
                && gestor.DialogoActivo
                && gestor.NpcActual != null
                && gestor.NpcActual.gameObject == gameObject;
        }

        private void BuscarJugador()
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");

            if (jugador != null)
            {
                transformJugador = jugador.transform;
            }
        }
    }
}
