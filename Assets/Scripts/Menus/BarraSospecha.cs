using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    public class BarraSospecha : MonoBehaviour
    {
        private const float ProporcionVerde = 0.5f;
        private const float ProporcionAmarillo = 0.3f;
        private const float ProporcionRojo = 0.2f;

        [Header("Tamano")]
        public Vector2 tamano = new Vector2(600f, 26f);
        [Tooltip("Distancia desde el borde superior de la pantalla.")]
        public float margenSuperior = 24f;

        [Header("Colores")]
        public Color colorVerde = new Color(0.30f, 0.78f, 0.35f, 1f);
        public Color colorAmarillo = new Color(0.95f, 0.80f, 0.25f, 1f);
        public Color colorRojo = new Color(0.88f, 0.25f, 0.25f, 1f);
        public Color colorMarcador = Color.white;
        [Tooltip("Color de los tramos que el marcador todavia no alcanzo.")]
        public Color colorApagado = new Color(1f, 1f, 1f, 0.22f);

        private const int OrdenCanvas = 50;

        private RectTransform marcador;
        private Image tramoVerde;
        private Image tramoAmarillo;
        private Image tramoRojo;
        private CanvasGroup grupo;

        private EstadoPartida estadoSuscrito;

        private float ultimaFraccion = -1f;

        private void Awake()
        {
            Construir();
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

        private void Suscribir()
        {
            EstadoPartida estado = EstadoPartida.Instancia;

            if (estado == null)
            {
                return;
            }

            estado.AlCambiarContadores += AlCambiarContadores;
            estadoSuscrito = estado;

            Pintar(estado.Sospecha, estado.SospechaMaxima);
        }

        private void Desuscribir()
        {
            if (estadoSuscrito != null)
            {
                estadoSuscrito.AlCambiarContadores -= AlCambiarContadores;
                estadoSuscrito = null;
            }
        }

        private void AlCambiarContadores(int impacto, int sospecha)
        {
            Pintar(sospecha, estadoSuscrito != null ? estadoSuscrito.SospechaMaxima : 100);
        }

        private void Pintar(int sospecha, int maxima)
        {
            if (marcador == null)
            {
                return;
            }

            float fraccion = maxima > 0 ? Mathf.Clamp01((float)sospecha / maxima) : 0f;

            if (Mathf.Approximately(fraccion, ultimaFraccion))
            {
                return;
            }

            ultimaFraccion = fraccion;

            marcador.anchoredPosition = new Vector2(fraccion * tamano.x, 0f);

            tramoVerde.color = Aplicar(colorVerde, fraccion > 0f);
            tramoAmarillo.color = Aplicar(colorAmarillo, fraccion > ProporcionVerde);
            tramoRojo.color = Aplicar(colorRojo, fraccion > ProporcionVerde + ProporcionAmarillo);
        }

        private Color Aplicar(Color color, bool alcanzado)
        {
            if (alcanzado)
            {
                return color;
            }

            return new Color(color.r, color.g, color.b, colorApagado.a);
        }

        private void ActualizarVisibilidad()
        {
            if (grupo == null)
            {
                return;
            }

            bool jugando = estadoSuscrito != null
                && estadoSuscrito.PartidaEnCurso
                && (GestorMenus.Instancia == null || !GestorMenus.Instancia.HayMenuAbierto);

            float alfa = jugando ? 1f : 0f;

            if (!Mathf.Approximately(grupo.alpha, alfa))
            {
                grupo.alpha = alfa;
            }
        }

        private void Construir()
        {
            Canvas canvas = ConstructorUI.CrearCanvas("CanvasBarraSospecha", transform, OrdenCanvas);

            Destroy(canvas.GetComponent<GraphicRaycaster>());

            grupo = canvas.gameObject.AddComponent<CanvasGroup>();
            grupo.interactable = false;
            grupo.blocksRaycasts = false;

            GameObject contenedor = new GameObject("Barra");
            contenedor.transform.SetParent(canvas.transform, false);

            RectTransform rect = contenedor.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -margenSuperior);
            rect.sizeDelta = tamano;

            GameObject fondo = ConstructorUI.CrearImagen("Fondo", contenedor.transform,
                new Color(0.05f, 0.05f, 0.08f, 0.85f));
            EstirarConMargen(fondo.GetComponent<RectTransform>(), -3f);

            float x = 0f;
            tramoVerde = CrearTramo("TramoVerde", contenedor.transform, x, ProporcionVerde, colorVerde);
            x += ProporcionVerde;
            tramoAmarillo = CrearTramo("TramoAmarillo", contenedor.transform, x, ProporcionAmarillo, colorAmarillo);
            x += ProporcionAmarillo;
            tramoRojo = CrearTramo("TramoRojo", contenedor.transform, x, ProporcionRojo, colorRojo);

            marcador = CrearMarcador(contenedor.transform);
        }

        private Image CrearTramo(string nombre, Transform padre, float inicio, float ancho, Color color)
        {
            GameObject objeto = ConstructorUI.CrearImagen(nombre, padre, color);

            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(inicio * tamano.x, 0f);
            rect.sizeDelta = new Vector2(ancho * tamano.x, 0f);

            return objeto.GetComponent<Image>();
        }

        private RectTransform CrearMarcador(Transform padre)
        {
            GameObject objeto = ConstructorUI.CrearImagen("Marcador", padre, colorMarcador);

            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(5f, 10f);

            return rect;
        }

        private static void EstirarConMargen(RectTransform rect, float margen)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margen, margen);
            rect.offsetMax = new Vector2(-margen, -margen);
        }
    }
}
