using UnityEngine;
using UnityEngine.UI;
using SoltaLaPala.Dialogue;
using SoltaLaPala.Menus;

namespace SoltaLaPala.Tutorial
{
    public class PanelTutorialHUD : MonoBehaviour
    {
        [Header("Datos")]
        public DatosTutorial datos;

        [Header("Visibilidad")]
        public bool ocultarConMenusAbiertos = true;

        [Header("Ajustes de Tamaño y Posición")]
        [Tooltip("Escala del panel tutorial. Modifícalo en el inspector para agrandar o achicar.")]
        [Range(0.5f, 2.5f)]
        public float escala = 1.26f;

        [Tooltip("Margen respecto a la esquina superior derecha (X hacia la izquierda, Y hacia abajo).")]
        public Vector2 margen = new Vector2(20f, 20f);

        [Tooltip("Tamaño base del panel en píxeles de referencia.")]
        public Vector2 tamanoBase = new Vector2(400f, 230f);

        [Header("Ajustes de Texto")]
        [Tooltip("Tamaño base de la fuente del contenido del tutorial.")]
        [Range(12, 32)]
        public int tamanoFuente = 18;

        [Tooltip("Tamaño de la fuente del indicador de páginas (← 1/4 →).")]
        [Range(10, 24)]
        public int tamanoFuenteIndicador = 15;

        [Header("Posición de Elementos")]
        [Tooltip("Posición u offset vertical del indicador de navegación (← 1/4 →) respecto al fondo de la caja.")]
        [Range(0f, 80f)]
        public float offsetVerticalIndicador = 28f;

        Canvas        _canvas;
        RectTransform _panelRect;
        Text          _textoContenido;
        Text          _textoIndicador;
        RectTransform _lineaTachadoTutorial;

        int    _pasoActual  = 0;
        bool[] _completados;
        bool   _panelCerrado = false;

        bool _warningMostrado = false;

        void Start()
        {
            if (datos == null)
            {
                datos = Resources.Load<DatosTutorial>("Tutorial/DatosTutorial");
                if (datos == null)
                {
                    datos = ScriptableObject.CreateInstance<DatosTutorial>();
                    datos.pasos = new PasoTutorial[]
                    {
                        new PasoTutorial { texto = "Usa WASD para moverte y el mouse para mirar. Mantén Shift para correr.", condicion = CondicionPaso.SoloManual },
                        new PasoTutorial { texto = "Habla con la Recepcionista [E] para recibir tu encargo.", condicion = CondicionPaso.SoloManual },
                        new PasoTutorial { texto = "Consigue la Pala o Palita y entrégasela a la Recepcionista.", condicion = CondicionPaso.AlCompletarMision, idMisionAsociada = "Recepcionista" },
                        new PasoTutorial { texto = "Sabotea las tazas de café sin que los compañeros te descubran.", condicion = CondicionPaso.SoloManual }
                    };
                }
            }

            ConstruirUI();

            if (_canvas != null)
                _canvas.gameObject.SetActive(false);

            if (datos != null && datos.pasos != null)
                _completados = new bool[datos.pasos.Length];

            SuscribirseAlNivel();
        }

        void OnDestroy()
        {
            if (_estadoSuscrito != null)
                _estadoSuscrito.AlEmpezarNivel -= MostrarPanel;
        }

        void Update()
        {
            SuscribirseAlNivel();

            if (_panelRect != null)
            {
                Vector2 tamanoDeseado = tamanoBase * escala;
                Vector2 posDeseada = new Vector2(-margen.x, -margen.y);

                if (_panelRect.sizeDelta != tamanoDeseado)
                    _panelRect.sizeDelta = tamanoDeseado;

                if (_panelRect.anchoredPosition != posDeseada)
                    _panelRect.anchoredPosition = posDeseada;

                int fuenteEscalada = Mathf.Max(10, Mathf.RoundToInt(tamanoFuente * escala));
                if (_textoContenido != null && _textoContenido.fontSize != fuenteEscalada)
                    _textoContenido.fontSize = fuenteEscalada;

                int indicadorEscalado = Mathf.Max(8, Mathf.RoundToInt(tamanoFuenteIndicador * escala));
                if (_textoIndicador != null)
                {
                    if (_textoIndicador.fontSize != indicadorEscalado)
                        _textoIndicador.fontSize = indicadorEscalado;

                    Vector2 posInd = new Vector2(0f, offsetVerticalIndicador * escala);
                    if (_textoIndicador.rectTransform.anchoredPosition != posInd)
                        _textoIndicador.rectTransform.anchoredPosition = posInd;
                }
            }

            if (datos == null || datos.pasos == null || datos.pasos.Length == 0)
            {
                if (!_warningMostrado)
                {
                    Debug.LogWarning("[PanelTutorialHUD] No hay DatosTutorial asignado o no contiene pasos.", this);
                    _warningMostrado = true;
                }
                return;
            }

            bool jugando = _estadoSuscrito != null
                && _estadoSuscrito.PartidaEnCurso
                && (!ocultarConMenusAbiertos || GestorMenus.Instancia == null || !GestorMenus.Instancia.HayMenuAbierto);

            if (!jugando || _panelCerrado)
            {
                if (_canvas != null && _canvas.gameObject.activeSelf)
                    _canvas.gameObject.SetActive(false);
                return;
            }

            if (_canvas != null && !_canvas.gameObject.activeSelf)
                _canvas.gameObject.SetActive(true);

            if (Input.GetKeyDown(KeyCode.LeftArrow) && _pasoActual > 0)
                _pasoActual--;

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (_pasoActual < datos.pasos.Length - 1)
                {
                    _pasoActual++;
                }
                else
                {
                    _panelCerrado = true;
                    if (_canvas != null) _canvas.gameObject.SetActive(false);
                    return;
                }
            }

            ComprobarMisiones();

            ActualizarUI();
        }

        public void CompletarPaso(int indice)
        {
            if (_completados == null || indice < 0 || indice >= _completados.Length)
                return;

            if (_completados[indice]) return;

            _completados[indice] = true;

            if (indice == _pasoActual)
            {
                if (_pasoActual < datos.pasos.Length - 1)
                {
                    _pasoActual++;
                }
                else
                {
                    _panelCerrado = true;
                    if (_canvas != null)
                        _canvas.gameObject.SetActive(false);
                }
            }
        }

        void MostrarPanel()
        {
            _panelCerrado = false;
            if (_canvas != null)
                _canvas.gameObject.SetActive(true);
        }

        void ComprobarMisiones()
        {
            if (_completados == null) return;

            for (int i = 0; i < datos.pasos.Length; i++)
            {
                PasoTutorial paso = datos.pasos[i];

                if (paso.condicion != CondicionPaso.AlCompletarMision) continue;
                if (string.IsNullOrEmpty(paso.idMisionAsociada))        continue;
                if (_completados[i])                                     continue;

                GameObject npcObj = GameObject.Find(paso.idMisionAsociada);
                if (npcObj == null) continue;

                MisionEntrega mision = npcObj.GetComponent<MisionEntrega>();
                if (mision != null && mision.Completada)
                    CompletarPaso(i);
            }
        }

        void ActualizarUI()
        {
            if (_textoContenido == null || _textoIndicador == null) return;
            if (_completados == null) return;

            PasoTutorial paso = datos.pasos[_pasoActual];

            _textoContenido.text = paso.texto;
            if (_completados[_pasoActual])
            {
                _textoContenido.color = new Color(1f, 1f, 1f, 0.45f);
                if (_lineaTachadoTutorial != null)
                {
                    _lineaTachadoTutorial.gameObject.SetActive(true);
                    float ancho = Mathf.Min(_textoContenido.preferredWidth + (10f * escala), _textoContenido.rectTransform.rect.width);
                    _lineaTachadoTutorial.sizeDelta = new Vector2(ancho, Mathf.Max(2f, 3f * escala));
                }
            }
            else
            {
                _textoContenido.color = Color.white;
                if (_lineaTachadoTutorial != null)
                    _lineaTachadoTutorial.gameObject.SetActive(false);
            }

            int total = datos.pasos.Length;
            _textoIndicador.text = "← " + (_pasoActual + 1) + "/" + total + " →";
        }

        void ConstruirUI()
        {
            _canvas = ConstructorUI.CrearCanvas("CanvasTutorialHUD", transform, 55);

            GraphicRaycaster raycaster = _canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                Destroy(raycaster);

            GameObject panelObj = new GameObject("PanelTutorial");
            panelObj.transform.SetParent(_canvas.transform, false);

            Image image = panelObj.AddComponent<Image>();
            Sprite spritePanel = Resources.Load<Sprite>("Sprites/UI/paneles/Panel tutorial");
            if (spritePanel != null)
            {
                image.sprite          = spritePanel;
                image.type            = Image.Type.Simple;
                image.preserveAspect  = false;
            }
            else
            {
                Debug.LogWarning("[PanelTutorialHUD] No se encontró el sprite 'Sprites/UI/paneles/Panel tutorial'.", this);
            }

            RectTransform rtPanel = panelObj.GetComponent<RectTransform>();
            rtPanel.anchorMin        = new Vector2(1f, 1f);
            rtPanel.anchorMax        = new Vector2(1f, 1f);
            rtPanel.pivot            = new Vector2(1f, 1f);
            rtPanel.anchoredPosition = new Vector2(-margen.x, -margen.y);
            rtPanel.sizeDelta        = tamanoBase * escala;

            _panelRect = rtPanel;

            image.preserveAspect = true;

            GameObject textoObj = new GameObject("TextoContenido");
            textoObj.transform.SetParent(panelObj.transform, false);

            _textoContenido                   = textoObj.AddComponent<Text>();
            _textoContenido.font              = ConstructorUI.Fuente;
            _textoContenido.fontSize          = 18;
            _textoContenido.color             = Color.white;
            _textoContenido.alignment         = TextAnchor.MiddleCenter;
            _textoContenido.supportRichText    = true;
            _textoContenido.horizontalOverflow = HorizontalWrapMode.Wrap;
            _textoContenido.verticalOverflow   = VerticalWrapMode.Overflow;

            RectTransform rtTexto = textoObj.GetComponent<RectTransform>();
            rtTexto.anchorMin        = new Vector2(0f, 0f);
            rtTexto.anchorMax        = new Vector2(1f, 1f);
            rtTexto.offsetMin        = new Vector2(25f, 35f);
            rtTexto.offsetMax        = new Vector2(-25f, -75f);

            GameObject goTachado = new GameObject("LineaTachado", typeof(RectTransform));
            goTachado.transform.SetParent(textoObj.transform, false);
            _lineaTachadoTutorial = (RectTransform)goTachado.transform;
            _lineaTachadoTutorial.anchorMin        = new Vector2(0.5f, 0.5f);
            _lineaTachadoTutorial.anchorMax        = new Vector2(0.5f, 0.5f);
            _lineaTachadoTutorial.pivot            = new Vector2(0.5f, 0.5f);
            _lineaTachadoTutorial.anchoredPosition = Vector2.zero;
            Image imgTach = goTachado.AddComponent<Image>();
            imgTach.color = new Color(1f, 1f, 1f, 0.85f);
            imgTach.raycastTarget = false;
            goTachado.SetActive(false);

            GameObject indicadorObj = new GameObject("TextoIndicador");
            indicadorObj.transform.SetParent(panelObj.transform, false);

            _textoIndicador                   = indicadorObj.AddComponent<Text>();
            _textoIndicador.font              = ConstructorUI.Fuente;
            _textoIndicador.fontSize          = 15;
            _textoIndicador.color             = new Color(1f, 1f, 1f, 0.9f);
            _textoIndicador.alignment         = TextAnchor.MiddleCenter;
            _textoIndicador.supportRichText    = false;
            _textoIndicador.horizontalOverflow = HorizontalWrapMode.Overflow;

            RectTransform rtIndicador = indicadorObj.GetComponent<RectTransform>();
            rtIndicador.anchorMin        = new Vector2(0.5f, 0f);
            rtIndicador.anchorMax        = new Vector2(0.5f, 0f);
            rtIndicador.pivot            = new Vector2(0.5f, 0f);
            rtIndicador.anchoredPosition = new Vector2(0f, offsetVerticalIndicador * escala);
            rtIndicador.sizeDelta        = new Vector2(200f, 22f);
        }

        EstadoPartida _estadoSuscrito;

        void SuscribirseAlNivel()
        {
            EstadoPartida estado = EstadoPartida.Instancia;
            if (estado == null || estado == _estadoSuscrito) return;

            if (_estadoSuscrito != null)
                _estadoSuscrito.AlEmpezarNivel -= MostrarPanel;

            estado.AlEmpezarNivel += MostrarPanel;
            _estadoSuscrito = estado;
        }
    }
}
