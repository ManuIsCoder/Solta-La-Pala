using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Temporizador del nivel en pantalla, arriba a la izquierda.
    //
    // PROTOTIPO: de momento es solo el numero. Mas adelante va a ser un reloj
    // dibujado; cuando ese arte exista se asigna al campo 'texto' y la generacion
    // por codigo se apaga sola (mismo criterio que InterfazInventario y MenuBase).
    public class HudTiempo : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Texto donde se pinta el tiempo. Si se deja vacio se genera por codigo.")]
        public Text texto;

        [Header("Colocacion")]
        [Tooltip("Distancia a la esquina superior izquierda, en pixeles de referencia.")]
        public Vector2 margen = new Vector2(40f, 30f);

        [Header("Aviso")]
        [Tooltip("Por debajo de estos segundos el numero se pinta en rojo.")]
        public float segundosDeAviso = 30f;

        // Se dibuja debajo de cualquier menu: con la pausa abierta el reloj no
        // tiene que taparla.
        private const int OrdenCanvas = 50;

        private EstadoPartida estadoSuscrito;

        // Ultimo valor pintado, en segundos enteros. Evita reconstruir el string
        // en cada frame cuando el numero visible no cambio.
        private int ultimoSegundoPintado = -1;

        private void Awake()
        {
            if (texto == null)
            {
                GenerarInterfaz();
            }
        }

        // La suscripcion va en OnEnable/OnDisable y no en Awake porque el
        // EstadoPartida puede crearse despues que este HUD: si en el primer intento
        // no esta, Update lo vuelve a intentar.
        private void OnEnable()
        {
            Suscribir();
        }

        private void OnDisable()
        {
            Desuscribir();
        }

        private void Update()
        {
            // EstadoPartida se recrea al cambiar de escena o de partida, asi que
            // hay que reengancharse al nuevo.
            if (estadoSuscrito != EstadoPartida.Instancia)
            {
                Desuscribir();
                Suscribir();
            }

            ActualizarVisibilidad();
        }

        // El reloj solo se ve mientras se esta jugando: en el menu principal no
        // hay nivel en curso, y en la pausa taparia el panel.
        private void ActualizarVisibilidad()
        {
            if (texto == null)
            {
                return;
            }

            bool jugando = estadoSuscrito != null
                && estadoSuscrito.PartidaEnCurso
                && (GestorMenus.Instancia == null || !GestorMenus.Instancia.HayMenuAbierto);

            if (texto.enabled != jugando)
            {
                texto.enabled = jugando;
            }
        }

        private void Suscribir()
        {
            EstadoPartida estado = EstadoPartida.Instancia;

            if (estado == null)
            {
                return;
            }

            estado.AlCambiarTiempo += Pintar;
            estadoSuscrito = estado;

            // Pintar de entrada para no mostrar el placeholder hasta el primer tick.
            Pintar(estado.TiempoRestante);
        }

        private void Desuscribir()
        {
            if (estadoSuscrito != null)
            {
                estadoSuscrito.AlCambiarTiempo -= Pintar;
                estadoSuscrito = null;
            }
        }

        // Pinta los segundos restantes como M:SS.
        private void Pintar(float segundosRestantes)
        {
            if (texto == null)
            {
                return;
            }

            // Hacia arriba: mientras quede una fraccion de segundo se sigue viendo
            // un 1, y el 0 aparece solo cuando el tiempo se agoto de verdad.
            int totalSegundos = Mathf.Max(0, Mathf.CeilToInt(segundosRestantes));

            if (totalSegundos == ultimoSegundoPintado)
            {
                return;
            }

            ultimoSegundoPintado = totalSegundos;

            texto.text = $"{totalSegundos / 60}:{totalSegundos % 60:00}";
            texto.color = totalSegundos <= segundosDeAviso
                ? ConstructorUI.ColorAcento
                : ConstructorUI.ColorTexto;
        }

        // Crea el canvas y el numero anclados arriba a la izquierda.
        private void GenerarInterfaz()
        {
            Canvas canvas = ConstructorUI.CrearCanvas("CanvasHudTiempo", transform, OrdenCanvas);

            // El HUD no recibe clicks: su raycaster solo robaria eventos a los menus.
            Destroy(canvas.GetComponent<GraphicRaycaster>());

            texto = ConstructorUI.CrearTexto("Tiempo", canvas.transform, "0:00",
                40, TextAnchor.UpperLeft, Vector2.zero, new Vector2(200f, 60f));

            RectTransform rect = texto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(margen.x, -margen.y);
        }
    }
}
