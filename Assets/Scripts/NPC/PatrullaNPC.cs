using SoltaLaPala.Dialogue;
using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.NPC
{
    // Mueve al NPC por una RutaNPC en bucle, y lo frena para que te mire fijo
    // cuando te pilla.
    //
    // Sin ruta asignada el NPC se queda quieto donde este, pero sigue girandose
    // hacia el jugador si lo ve: asi los NPC estaticos usan el mismo componente
    // que los que patrullan.
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
        [Tooltip("Velocidad de giro al clavarte la mirada. Mas rapido que el " +
                 "giro normal: te acaba de pillar.")]
        public float velocidadGiroVigilando = 540f;

        private DetectorVisionNPC detector;
        private Transform transformJugador;

        // Indice de la parada a la que se dirige ahora.
        private int indiceDestino;
        // 1 yendo hacia adelante por la ruta, -1 de vuelta. Solo lo usa IdaYVuelta.
        private int sentido = 1;

        // Cuenta atras de la espera en el punto actual.
        private float esperaRestante;
        // Cuenta atras desde que dejo de verte.
        private float vigilanciaRestante;

        // True mientras esta plantado mirandote en vez de patrullando.
        public bool Vigilando { get; private set; }

        private void Awake()
        {
            detector = GetComponent<DetectorVisionNPC>();
        }

        private void Start()
        {
            BuscarJugador();

            // Se arranca yendo al punto mas cercano y no al primero: si no, un NPC
            // colocado a mitad de ruta cruza el mapa en linea recta al empezar.
            indiceDestino = BuscarPuntoMasCercano();
        }

        private void Update()
        {
            // Con un menu abierto el juego esta pausado. Time.deltaTime ya vale 0,
            // pero se sale antes para no gastar el resto del calculo.
            if (GestorMenus.Instancia != null && GestorMenus.Instancia.HayMenuAbierto)
            {
                return;
            }

            if (transformJugador == null)
            {
                BuscarJugador();
            }

            // Si el dialogo es con este NPC se queda quieto mirando a quien le
            // habla: seguir la ruta lo haria irse caminando a mitad de frase.
            // Los demas NPC siguen con lo suyo.
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

        // Decide si el NPC esta en modo vigilancia y lleva la cuenta atras de
        // cuanto le queda para volver a lo suyo.
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

            // Dejo de verte: no vuelve a la ruta de inmediato, se queda un rato
            // mirando hacia donde estabas.
            vigilanciaRestante -= Time.deltaTime;

            if (vigilanciaRestante <= 0f)
            {
                Vigilando = false;

                // Al retomar, la espera del punto se salta: ya estuvo parado de sobra.
                esperaRestante = 0f;
            }
        }

        // Gira hacia el jugador sin moverse del sitio.
        private void MirarAlJugador()
        {
            if (transformJugador == null)
            {
                return;
            }

            Vector3 haciaJugador = transformJugador.position - transform.position;

            // Se anula la componente vertical: el NPC gira sobre si mismo, no se
            // inclina para mirar al suelo o al techo.
            haciaJugador.y = 0f;

            GirarHacia(haciaJugador, velocidadGiroVigilando);
        }

        // Avanza hacia la parada actual y encadena con la siguiente al llegar.
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

            // La altura la marca el suelo donde esta el NPC, no el punto: asi las
            // marcas se pueden colocar a ojo sin que el NPC flote o se hunda.
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

        // Rota suavemente hacia una direccion horizontal.
        private void GirarHacia(Vector3 direccion, float gradosPorSegundo)
        {
            direccion.y = 0f;

            // Una direccion casi nula da una rotacion indefinida y hace temblar al NPC.
            if (direccion.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direccion),
                gradosPorSegundo * Time.deltaTime);
        }

        // Parada mas cercana a donde esta el NPC ahora.
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

        // True si la conversacion en marcha es con este NPC.
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
