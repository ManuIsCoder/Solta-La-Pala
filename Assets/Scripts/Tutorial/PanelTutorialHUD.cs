using UnityEngine;
using UnityEngine.UI;
using SoltaLaPala.Dialogue;
using SoltaLaPala.Menus;

namespace SoltaLaPala.Tutorial
{
    /// <summary>
    /// Panel HUD del tutorial. Se sitúa arriba a la derecha con orden de canvas 55.
    /// Muestra los pasos del DatosTutorial y permite navegar con ← →.
    /// Los pasos con CondicionPaso.AlCompletarMision se completan automáticamente
    /// cuando la MisionEntrega del NPC indicado reporta Completada == true.
    /// </summary>
    public class PanelTutorialHUD : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        //  Inspector
        // ──────────────────────────────────────────────

        [Header("Datos")]
        public DatosTutorial datos;                     // Arrastrar el ScriptableObject desde el inspector

        [Header("Visibilidad")]
        public bool ocultarConMenusAbiertos = true;     // Ocultar el panel si hay algún menú abierto

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

        // ──────────────────────────────────────────────
        //  Referencias internas de UI
        // ──────────────────────────────────────────────

        Canvas        _canvas;          // Canvas propio del HUD de tutorial
        RectTransform _panelRect;       // RectTransform del panel de fondo
        Text          _textoContenido;  // Texto principal del paso actual
        Text          _textoIndicador;  // Indicador "← N/T →"

        // ──────────────────────────────────────────────
        //  Estado
        // ──────────────────────────────────────────────

        int    _pasoActual  = 0;
        bool[] _completados;            // Un bool por cada paso en datos.pasos
        bool   _panelCerrado = false;   // True cuando el jugador o el avance cierra el panel

        bool _warningMostrado = false;  // Evitar spam de warnings en consola

        // ──────────────────────────────────────────────
        //  Ciclo de vida
        // ──────────────────────────────────────────────

        void Start()
        {
            // Si no se asignó un ScriptableObject en el inspector, buscar uno en Resources
            // o crear datos por defecto para que la UI funcione de inmediato.
            if (datos == null)
            {
                datos = Resources.Load<DatosTutorial>("Tutorial/DatosTutorial");
                if (datos == null)
                {
                    datos = ScriptableObject.CreateInstance<DatosTutorial>();
                    datos.pasos = new PasoTutorial[]
                    {
                        new PasoTutorial { texto = "Usa WASD para moverte y el ratón para mirar. Mantén Shift para correr.", condicion = CondicionPaso.SoloManual },
                        new PasoTutorial { texto = "Habla con la Recepcionista [E] para recibir tu encargo.", condicion = CondicionPaso.SoloManual },
                        new PasoTutorial { texto = "Consigue la Pala o Palita y entrégasela a la Recepcionista.", condicion = CondicionPaso.AlCompletarMision, idMisionAsociada = "Recepcionista" },
                        new PasoTutorial { texto = "Sabotea las tazas de café sin que los compañeros te descubran.", condicion = CondicionPaso.SoloManual }
                    };
                }
            }

            ConstruirUI();

            // El panel empieza oculto; se muestra cuando empieza el nivel
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);

            // Inicializar el array de completados
            if (datos != null && datos.pasos != null)
                _completados = new bool[datos.pasos.Length];

            // Suscribirse al evento de instancia de EstadoPartida.
            // Si aún no existe se reintenta en Update cuando aparezca.
            SuscribirseAlNivel();
        }

        void OnDestroy()
        {
            if (_estadoSuscrito != null)
                _estadoSuscrito.AlEmpezarNivel -= MostrarPanel;
        }

        void Update()
        {
            // EstadoPartida puede crearse después que este HUD: reengancharse al nuevo.
            SuscribirseAlNivel();

            // Permite modificar tamaño y posición desde el inspector de Unity en tiempo real
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
                if (_textoIndicador != null && _textoIndicador.fontSize != indicadorEscalado)
                    _textoIndicador.fontSize = indicadorEscalado;
            }

            // Guardia: sin datos no hay nada que hacer
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

            // Si no estamos jugando o el panel fue cerrado, mantener oculto
            if (!jugando || _panelCerrado)
            {
                if (_canvas != null && _canvas.gameObject.activeSelf)
                    _canvas.gameObject.SetActive(false);
                return;
            }

            if (_canvas != null && !_canvas.gameObject.activeSelf)
                _canvas.gameObject.SetActive(true);

            // ── Navegación con teclado ──
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
                    // Al tocar la flecha derecha en el último paso, desaparece el panel
                    _panelCerrado = true;
                    if (_canvas != null) _canvas.gameObject.SetActive(false);
                    return;
                }
            }

            // ── Comprobar misiones completadas automáticamente ──
            ComprobarMisiones();

            // ── Refrescar la UI ──
            ActualizarUI();
        }

        // ──────────────────────────────────────────────
        //  API pública
        // ──────────────────────────────────────────────

        /// <summary>
        /// Permite que otros sistemas fuercen la completación de un paso concreto.
        /// Al completarse el paso actual, avanza al siguiente paso o desaparece si era el último.
        /// </summary>
        public void CompletarPaso(int indice)
        {
            if (_completados == null || indice < 0 || indice >= _completados.Length)
                return;

            if (_completados[indice]) return;

            _completados[indice] = true;

            // Al completar el objetivo actual: cambia al siguiente paso, o desaparece si era el último
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

        // ──────────────────────────────────────────────
        //  Lógica interna
        // ──────────────────────────────────────────────

        /// <summary>
        /// Muestra el panel (llamado desde el evento AlEmpezarNivel).
        /// </summary>
        void MostrarPanel()
        {
            _panelCerrado = false;
            if (_canvas != null)
                _canvas.gameObject.SetActive(true);
        }

        /// <summary>
        /// Recorre los pasos con CondicionPaso.AlCompletarMision y comprueba
        /// si la MisionEntrega del NPC indicado ya está completada.
        /// </summary>
        void ComprobarMisiones()
        {
            if (_completados == null) return;

            for (int i = 0; i < datos.pasos.Length; i++)
            {
                PasoTutorial paso = datos.pasos[i];

                if (paso.condicion != CondicionPaso.AlCompletarMision) continue;
                if (string.IsNullOrEmpty(paso.idMisionAsociada))        continue;
                if (_completados[i])                                     continue; // ya completado

                // Buscar el GameObject del NPC en la escena por nombre
                GameObject npcObj = GameObject.Find(paso.idMisionAsociada);
                if (npcObj == null) continue;

                MisionEntrega mision = npcObj.GetComponent<MisionEntrega>();
                if (mision != null && mision.Completada)
                    CompletarPaso(i);
            }
        }

        /// <summary>
        /// Actualiza los textos del panel con el estado del paso actual.
        /// </summary>
        void ActualizarUI()
        {
            if (_textoContenido == null || _textoIndicador == null) return;
            if (_completados == null) return;

            PasoTutorial paso = datos.pasos[_pasoActual];

            // Texto con tachado si el paso está completado
            if (_completados[_pasoActual])
                _textoContenido.text = "<s>" + paso.texto + "</s>";
            else
                _textoContenido.text = paso.texto;

            // Indicador de navegación: "← N/T →"
            int total = datos.pasos.Length;
            _textoIndicador.text = "← " + (_pasoActual + 1) + "/" + total + " →";
        }

        // ──────────────────────────────────────────────
        //  Construcción de UI
        // ──────────────────────────────────────────────

        /// <summary>
        /// Crea en tiempo de ejecución el canvas y todos los elementos visuales del panel.
        /// </summary>
        void ConstruirUI()
        {
            // ── Canvas ──
            _canvas = ConstructorUI.CrearCanvas("CanvasTutorialHUD", transform, 55);

            // Quitar GraphicRaycaster (igual que HudTiempo; el panel no necesita recibir clicks)
            GraphicRaycaster raycaster = _canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                Destroy(raycaster);

            // ── Panel imagen ──
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

            // RectTransform del panel: arriba-derecha, 400×230 (proporción 800×460 del sprite)
            RectTransform rtPanel = panelObj.GetComponent<RectTransform>();
            rtPanel.anchorMin        = new Vector2(1f, 1f);
            rtPanel.anchorMax        = new Vector2(1f, 1f);
            rtPanel.pivot            = new Vector2(1f, 1f);
            rtPanel.anchoredPosition = new Vector2(-margen.x, -margen.y);
            rtPanel.sizeDelta        = tamanoBase * escala;

            _panelRect = rtPanel;

            image.preserveAspect = true;

            // ── Texto de contenido ──
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

            // Situado en el área azul del diseño (el encabezado mide ~75px a esta escala)
            RectTransform rtTexto = textoObj.GetComponent<RectTransform>();
            rtTexto.anchorMin        = new Vector2(0f, 0f);
            rtTexto.anchorMax        = new Vector2(1f, 1f);
            rtTexto.offsetMin        = new Vector2(25f, 35f);
            rtTexto.offsetMax        = new Vector2(-25f, -75f);

            // ── Texto indicador de navegación ──
            GameObject indicadorObj = new GameObject("TextoIndicador");
            indicadorObj.transform.SetParent(panelObj.transform, false);

            _textoIndicador                   = indicadorObj.AddComponent<Text>();
            _textoIndicador.font              = ConstructorUI.Fuente;
            _textoIndicador.fontSize          = 15;
            _textoIndicador.color             = new Color(1f, 1f, 1f, 0.9f);
            _textoIndicador.alignment         = TextAnchor.MiddleCenter;
            _textoIndicador.supportRichText    = false;
            _textoIndicador.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Anclado en la parte inferior, centrado entre las flechas
            RectTransform rtIndicador = indicadorObj.GetComponent<RectTransform>();
            rtIndicador.anchorMin        = new Vector2(0.5f, 0f);
            rtIndicador.anchorMax        = new Vector2(0.5f, 0f);
            rtIndicador.pivot            = new Vector2(0.5f, 0f);
            rtIndicador.anchoredPosition = new Vector2(0f, 10f);
            rtIndicador.sizeDelta        = new Vector2(200f, 22f);
        }

        // ── Suscripción al evento de instancia ────────────────────────────────

        // EstadoPartida puede crearse después que este HUD: si no existe aún,
        // Update lo reintenta en cuanto aparezca (igual que HudTiempo).
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
