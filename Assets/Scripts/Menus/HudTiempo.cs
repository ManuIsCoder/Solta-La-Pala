using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
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

        private const int OrdenCanvas = 50;

        private EstadoPartida estadoSuscrito;

        private int ultimoSegundoPintado = -1;

        private void Awake()
        {
            if (texto == null)
            {
                GenerarInterfaz();
            }
        }

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
            if (estadoSuscrito != EstadoPartida.Instancia)
            {
                Desuscribir();
                Suscribir();
            }

            ActualizarVisibilidad();
        }

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

        private void Pintar(float segundosRestantes)
        {
            if (texto == null)
            {
                return;
            }

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

        private void GenerarInterfaz()
        {
            Canvas canvas = ConstructorUI.CrearCanvas("CanvasHudTiempo", transform, OrdenCanvas);

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
