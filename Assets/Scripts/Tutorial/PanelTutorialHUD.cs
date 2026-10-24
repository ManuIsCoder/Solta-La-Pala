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

        // ──────────────────────────────────────────────
        //  Referencias internas de UI
        // ──────────────────────────────────────────────

        Canvas   _canvas;       // Canvas propio del HUD de tutorial
        Text     _textoContenido;  // Texto principal del paso actual
        Text     _textoIndicador;  // Indicador "← N/T →"

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
            ConstruirUI();

            // El panel empieza oculto; se muestra cuando empieza el nivel
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);

            // Inicializar el array de completados si los datos ya están asignados
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

            // RectTransform del panel: arriba-derecha, 400×160, con margen de 20px
            RectTransform rtPanel = panelObj.GetComponent<RectTransform>();
            rtPanel.anchorMin        = new Vector2(1f, 1f);
            rtPanel.anchorMax        = new Vector2(1f, 1f);
            rtPanel.pivot            = new Vector2(1f, 1f);
            rtPanel.anchoredPosition = new Vector2(-20f, -20f);
            rtPanel.sizeDelta        = new Vector2(400f, 160f);

            // ── Texto de contenido ──
            GameObject textoObj = new GameObject("TextoContenido");
            textoObj.transform.SetParent(panelObj.transform, false);

            _textoContenido                   = textoObj.AddComponent<Text>();
            _textoContenido.font              = ConstructorUI.Fuente;
            _textoContenido.fontSize          = 20;
            _textoContenido.color             = Color.black;
            _textoContenido.alignment         = TextAnchor.MiddleCenter;
            _textoContenido.supportRichText    = true;
            _textoContenido.horizontalOverflow = HorizontalWrapMode.Wrap;
            _textoContenido.verticalOverflow   = VerticalWrapMode.Overflow;

            // Anclar arriba y ocupar todo el ancho con padding lateral de 16px
            RectTransform rtTexto = textoObj.GetComponent<RectTransform>();
            rtTexto.anchorMin        = new Vector2(0f, 1f);
            rtTexto.anchorMax        = new Vector2(1f, 1f);
            rtTexto.pivot            = new Vector2(0.5f, 1f);
            rtTexto.anchoredPosition = new Vector2(0f, -8f);   // -8px para dejar espacio al encabezado del sprite
            rtTexto.sizeDelta        = new Vector2(0f, 110f);   // altura fija; ancho se controla con offsetMin/Max
            rtTexto.offsetMin        = new Vector2(16f,  rtTexto.offsetMin.y);   // padding izquierdo
            rtTexto.offsetMax        = new Vector2(-16f, rtTexto.offsetMax.y);   // padding derecho

            // ── Texto indicador de navegación ──
            GameObject indicadorObj = new GameObject("TextoIndicador");
            indicadorObj.transform.SetParent(panelObj.transform, false);

            _textoIndicador                   = indicadorObj.AddComponent<Text>();
            _textoIndicador.font              = ConstructorUI.Fuente;
            _textoIndicador.fontSize          = 16;
            _textoIndicador.color             = Color.black;
            _textoIndicador.alignment         = TextAnchor.MiddleCenter;
            _textoIndicador.supportRichText    = false;
            _textoIndicador.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Anclar abajo-centro, a 10px del borde inferior, 200×24
            RectTransform rtIndicador = indicadorObj.GetComponent<RectTransform>();
            rtIndicador.anchorMin        = new Vector2(0.5f, 0f);
            rtIndicador.anchorMax        = new Vector2(0.5f, 0f);
            rtIndicador.pivot            = new Vector2(0.5f, 0f);
            rtIndicador.anchoredPosition = new Vector2(0f, 10f);
            rtIndicador.sizeDelta        = new Vector2(200f, 24f);
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
