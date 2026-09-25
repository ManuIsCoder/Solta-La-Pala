using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Barra de sospecha centrada arriba. Se divide en tres tramos fijos:
    //
    //   verde    50%  -> vas tranquilo
    //   amarillo 30%  -> estan sospechando
    //   rojo     20%  -> a punto de que te pillen
    //
    // Los tramos son siempre del mismo ancho: lo que se mueve es el marcador, que
    // avanza segun la sospecha acumulada. Al llegar al extremo derecho se pierde
    // el nivel (lo decide EstadoPartida, esta barra solo lo pinta).
    //
    // PROTOTIPO: generada por codigo, como el resto de la UI. Cuando haya arte se
    // sustituye por un prefab.
    public class BarraSospecha : MonoBehaviour
    {
        // Proporcion de cada tramo. Suman 1.
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

        // Debajo de los menus (100+) y a la par del reloj.
        private const int OrdenCanvas = 50;

        private RectTransform marcador;
        private Image tramoVerde;
        private Image tramoAmarillo;
        private Image tramoRojo;
        private CanvasGroup grupo;

        private EstadoPartida estadoSuscrito;

        // Ultima fraccion pintada, para no recolocar el marcador cada frame.
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
            // EstadoPartida se recrea entre partidas: hay que reengancharse.
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

        // El evento trae impacto y sospecha; aqui solo interesa la segunda.
        private void AlCambiarContadores(int impacto, int sospecha)
        {
            Pintar(sospecha, estadoSuscrito != null ? estadoSuscrito.SospechaMaxima : 100);
        }

        // Coloca el marcador y apaga los tramos que todavia no se alcanzaron.
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

            // El marcador se mueve sobre el ancho total: anclado a la izquierda,
            // avanza hasta el borde derecho cuando la sospecha llega al maximo.
            marcador.anchoredPosition = new Vector2(fraccion * tamano.x, 0f);

            // Un tramo se enciende cuando el marcador entro en el: asi se lee de un
            // vistazo en que zona estas, no solo donde cae la aguja.
            tramoVerde.color = Aplicar(colorVerde, fraccion > 0f);
            tramoAmarillo.color = Aplicar(colorAmarillo, fraccion > ProporcionVerde);
            tramoRojo.color = Aplicar(colorRojo, fraccion > ProporcionVerde + ProporcionAmarillo);
        }

        // Devuelve el color vivo si el tramo esta alcanzado, o apagado si no.
        private Color Aplicar(Color color, bool alcanzado)
        {
            if (alcanzado)
            {
                return color;
            }

            // Se conserva el tono para que se siga leyendo cual es cada tramo,
            // solo baja la opacidad.
            return new Color(color.r, color.g, color.b, colorApagado.a);
        }

        // La barra solo se ve jugando: en el menu principal no hay nivel, y en la
        // pausa taparia el panel.
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

        // Arma el canvas, los tres tramos y el marcador.
        private void Construir()
        {
            Canvas canvas = ConstructorUI.CrearCanvas("CanvasBarraSospecha", transform, OrdenCanvas);

            // El HUD no recibe clicks: su raycaster solo robaria eventos a los menus.
            Destroy(canvas.GetComponent<GraphicRaycaster>());

            grupo = canvas.gameObject.AddComponent<CanvasGroup>();
            grupo.interactable = false;
            grupo.blocksRaycasts = false;

            // Contenedor centrado arriba. Los tramos se anclan a su izquierda,
            // asi las posiciones se calculan en un solo sentido.
            GameObject contenedor = new GameObject("Barra");
            contenedor.transform.SetParent(canvas.transform, false);

            RectTransform rect = contenedor.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -margenSuperior);
            rect.sizeDelta = tamano;

            // Fondo oscuro detras de los tramos, para que la barra se despegue
            // del escenario cuando el fondo es claro.
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

        // Un tramo de color, colocado por fraccion del ancho total.
        private Image CrearTramo(string nombre, Transform padre, float inicio, float ancho, Color color)
        {
            GameObject objeto = ConstructorUI.CrearImagen(nombre, padre, color);

            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(inicio * tamano.x, 0f);
            // sizeDelta.y a 0 con los anclajes estirados = alto completo del padre.
            rect.sizeDelta = new Vector2(ancho * tamano.x, 0f);

            return objeto.GetComponent<Image>();
        }

        // Linea vertical que marca cuanta sospecha llevas.
        private RectTransform CrearMarcador(Transform padre)
        {
            GameObject objeto = ConstructorUI.CrearImagen("Marcador", padre, colorMarcador);

            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            // Sobresale un poco por arriba y por abajo para que se vea sobre
            // cualquiera de los tres colores.
            rect.sizeDelta = new Vector2(5f, 10f);

            return rect;
        }

        // Estira un rect sobre todo el padre, con un margen (negativo lo agranda).
        private static void EstirarConMargen(RectTransform rect, float margen)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margen, margen);
            rect.offsetMax = new Vector2(-margen, -margen);
        }
    }
}
