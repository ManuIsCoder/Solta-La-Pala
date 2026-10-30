using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Al empezar cada nivel muestra arriba al centro en que puesto de la empresa
    // estas. Si venis justo de pasar el nivel anterior, primero se ve la bajada
    // de rango (CEO -> Director, ...) y despues el puesto nuevo. A los pocos
    // segundos se desvanece.
    //
    // Las hojas son del pack escalera_rangos (Resources/Sprites/Rangos): celdas
    // cuadradas en 8 columnas a 20 fps. Se cortan en sprites al usarlas y se
    // liberan al terminar, porque cada hoja ocupa varios MB en memoria.
    public class IndicadorRango : MonoBehaviour
    {
        // Jerarquia de la empresa, de arriba hacia abajo. El nivel 1 es CEO,
        // el 2 Director, y asi. Son los prefijos de los archivos de las hojas.
        private static readonly string[] Rangos =
            { "ceo", "director", "gerente", "supervisor", "administrativo", "pasante" };

        private const string CarpetaHojas = "Sprites/Rangos/";
        private const int Columnas = 8;
        private const int FramesReposo = 48;
        private const int FramesBajada = 44;
        private const float Fps = 20f;

        [Tooltip("Lado del dibujo en pantalla, en pixeles de una pantalla de 1920x1080.")]
        public float tamano = 240f;
        [Tooltip("Separacion desde el borde de arriba de la pantalla.")]
        public float margenSuperior = 10f;
        [Tooltip("Segundos que se ve el puesto (despues de la bajada, si la hay).")]
        public float duracionReposo = 2.5f;
        [Tooltip("Segundos que tarda en desvanecerse al final del reposo.")]
        public float duracionFundido = 0.5f;

        // Ultimo nivel mostrado en esta sesion. La bajada solo se ve si se viene
        // del nivel anterior: al reintentar o al continuar un guardado se muestra
        // el puesto directamente.
        private static int ultimoNivelMostrado;

        private Image imagen;
        private CanvasGroup grupo;
        private Coroutine animacion;
        private EstadoPartida estadoSuscrito;

        private readonly List<Texture2D> hojasCargadas = new List<Texture2D>();
        private readonly List<Sprite> spritesCreados = new List<Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstadoEstatico()
        {
            ultimoNivelMostrado = 0;
        }

        private void Awake()
        {
            CrearInterfaz();
            Ocultar();
        }

        // En Start y no en OnEnable: EstadoPartida lo crea ArranqueMenus en el
        // mismo frame y puede no tener Instancia todavia.
        private void Start()
        {
            estadoSuscrito = EstadoPartida.Instancia;
            if (estadoSuscrito != null)
            {
                estadoSuscrito.AlEmpezarNivel += AlEmpezarNivel;
            }
        }

        private void OnDestroy()
        {
            if (estadoSuscrito != null)
            {
                estadoSuscrito.AlEmpezarNivel -= AlEmpezarNivel;
            }

            Liberar();
        }

        private void AlEmpezarNivel()
        {
            if (animacion != null)
            {
                StopCoroutine(animacion);
                Liberar();
            }

            animacion = StartCoroutine(Mostrar(ProgresoNiveles.NivelActual));
        }

        private IEnumerator Mostrar(int nivel)
        {
            int rango = Mathf.Clamp(nivel, 1, Rangos.Length) - 1;
            bool conBajada = rango > 0 && ultimoNivelMostrado == nivel - 1;
            ultimoNivelMostrado = nivel;

            grupo.alpha = 1f;
            imagen.enabled = true;

            if (conBajada)
            {
                List<Sprite> bajada = Cortar($"{Rangos[rango - 1]}_a_{Rangos[rango]}", FramesBajada);
                yield return Reproducir(bajada, bajada.Count / Fps, enLoop: false, fundido: 0f);
            }

            List<Sprite> reposo = Cortar($"{Rangos[rango]}_idle", FramesReposo);
            yield return Reproducir(reposo, duracionReposo, enLoop: true, fundido: duracionFundido);

            Ocultar();
            Liberar();
            animacion = null;
        }

        // Pasa los frames durante 'duracion' segundos. Con Time.deltaTime y no el
        // tiempo real: si el jugador pausa, la animacion espera con el juego.
        private IEnumerator Reproducir(List<Sprite> frames, float duracion, bool enLoop, float fundido)
        {
            if (frames.Count == 0)
            {
                yield break;
            }

            for (float t = 0f; t < duracion; t += Time.deltaTime)
            {
                int indice = Mathf.FloorToInt(t * Fps);
                indice = enLoop ? indice % frames.Count : Mathf.Min(indice, frames.Count - 1);
                imagen.sprite = frames[indice];

                float restante = duracion - t;
                grupo.alpha = fundido > 0f && restante < fundido ? restante / fundido : 1f;

                yield return null;
            }
        }

        // Corta una hoja en sus celdas, de izquierda a derecha y de arriba abajo.
        // El tamano de celda se saca del ancho importado, asi funciona igual si la
        // hoja se importo achicada.
        private List<Sprite> Cortar(string nombreHoja, int cantidad)
        {
            var frames = new List<Sprite>(cantidad);

            Texture2D hoja = Resources.Load<Texture2D>(CarpetaHojas + nombreHoja);
            if (hoja == null)
            {
                Debug.LogWarning($"[IndicadorRango] No se encontro la hoja '{nombreHoja}' en Resources/{CarpetaHojas}.");
                return frames;
            }

            hojasCargadas.Add(hoja);
            float celda = hoja.width / (float)Columnas;

            for (int i = 0; i < cantidad; i++)
            {
                int columna = i % Columnas;
                int fila = i / Columnas;
                // Las texturas cuentan las filas desde abajo.
                Rect rect = new Rect(columna * celda, hoja.height - (fila + 1) * celda, celda, celda);

                Sprite sprite = Sprite.Create(hoja, rect, new Vector2(0.5f, 0.5f));
                spritesCreados.Add(sprite);
                frames.Add(sprite);
            }

            return frames;
        }

        private void Liberar()
        {
            foreach (Sprite sprite in spritesCreados)
            {
                Destroy(sprite);
            }
            spritesCreados.Clear();

            foreach (Texture2D hoja in hojasCargadas)
            {
                Resources.UnloadAsset(hoja);
            }
            hojasCargadas.Clear();
        }

        private void Ocultar()
        {
            grupo.alpha = 0f;
            imagen.sprite = null;
            imagen.enabled = false;
        }

        // Canvas propio, por debajo de los menus (100+) para que la pausa lo tape.
        private void CrearInterfaz()
        {
            GameObject objetoCanvas = new GameObject("CanvasIndicadorRango", typeof(RectTransform));
            objetoCanvas.transform.SetParent(transform, false);

            Canvas canvas = objetoCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;

            CanvasScaler escalador = objetoCanvas.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;

            grupo = objetoCanvas.AddComponent<CanvasGroup>();
            // Es solo informativo: no tiene que comerse clicks.
            grupo.blocksRaycasts = false;
            grupo.interactable = false;

            GameObject objetoImagen = new GameObject("Rango", typeof(RectTransform));
            objetoImagen.transform.SetParent(objetoCanvas.transform, false);

            RectTransform rect = (RectTransform)objetoImagen.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -margenSuperior);
            rect.sizeDelta = new Vector2(tamano, tamano);

            imagen = objetoImagen.AddComponent<Image>();
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;
        }
    }
}
