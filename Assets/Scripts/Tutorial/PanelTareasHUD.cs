using System.Collections.Generic;
using SoltaLaPala.Dialogue;
using SoltaLaPala.Menus;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Tutorial
{
    /// <summary>
    /// Panel HUD de tareas del nivel. Se muestra arriba-izquierda, debajo del contador de tiempo.
    /// Las tareas se tachan al completarse, sin eliminarse ni ocultarse.
    /// </summary>
    public class PanelTareasHUD : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Tareas")]
        [Tooltip("Lista de tareas del nivel. Cada string es una tarea.")]
        public string[] tareas;

        [Tooltip("Nombre del GameObject con MisionEntrega correspondiente a cada tarea " +
                 "(en el mismo orden que 'tareas'). Dejar vacío si la tarea no tiene misión asociada.")]
        public string[] idMisionesAsociadas;

        [Header("Visibilidad")]
        public bool ocultarConMenusAbiertos = true;

        [Header("Ajustes de Tamaño y Posición")]
        [Tooltip("Escala del panel de tareas. Modifícalo en el inspector para agrandar o achicar.")]
        [Range(0.5f, 2.5f)]
        public float escala = 1.0f;

        [Tooltip("Margen respecto a la esquina superior izquierda (X a la derecha, Y hacia abajo).")]
        public Vector2 margen = new Vector2(20f, 100f);

        [Tooltip("Tamaño base del panel en píxeles de referencia.")]
        public Vector2 tamanoBase = new Vector2(350f, 490f);

        // ── Constantes de layout ───────────────────────────────────────────────

        private const float AltoBase  = 25f;
        private const float AltoLinea = 35f;

        // ── Estado interno ─────────────────────────────────────────────────────

        private Canvas          _canvas;
        private RectTransform   _panelRect;
        private List<Text>      textosEntradas = new List<Text>();
        private bool[]          completadas;

        // ── Ciclo de vida Unity ────────────────────────────────────────────────

        private void Awake()
        {
            if (tareas == null || tareas.Length == 0)
            {
                tareas = new string[]
                {
                    "Hablar con Recepcionista [E]",
                    "Entregar encargo a Recepcionista",
                    "Sabotear el café de la oficina"
                };
                idMisionesAsociadas = new string[]
                {
                    "",
                    "Recepcionista",
                    ""
                };
            }

            completadas = new bool[tareas.Length];
            ConstruirUI();

            // Empieza oculto; se revela cuando comienza el nivel
            _canvas.enabled = false;
        }

        private void Start()
        {
            // Suscribirse al evento de instancia de EstadoPartida.
            // Si aún no existe se reintenta en Update cuando aparezca.
            SuscribirseAlNivel();
        }

        private void OnDestroy()
        {
            if (_estadoSuscrito != null)
                _estadoSuscrito.AlEmpezarNivel -= MostrarPanel;
        }

        private void Update()
        {
            // EstadoPartida puede crearse después que este HUD: reengancharse al nuevo.
            SuscribirseAlNivel();

            if (_canvas == null) return;

            // Permite modificar tamaño y posición desde el inspector de Unity en tiempo real
            if (_panelRect != null)
            {
                Vector2 tamanoDeseado = tamanoBase * escala;
                Vector2 posDeseada = new Vector2(margen.x, -margen.y);

                if (_panelRect.sizeDelta != tamanoDeseado)
                    _panelRect.sizeDelta = tamanoDeseado;

                if (_panelRect.anchoredPosition != posDeseada)
                    _panelRect.anchoredPosition = posDeseada;
            }

            bool jugando = _estadoSuscrito != null
                && _estadoSuscrito.PartidaEnCurso
                && (!ocultarConMenusAbiertos || GestorMenus.Instancia == null || !GestorMenus.Instancia.HayMenuAbierto);

            if (_canvas.enabled != jugando)
            {
                _canvas.enabled = jugando;
            }

            if (!jugando) return;

            // Revisar misiones asociadas para detectar tareas completadas
            for (int i = 0; i < tareas.Length; i++)
            {
                if (completadas[i]) continue;

                if (idMisionesAsociadas != null &&
                    i < idMisionesAsociadas.Length &&
                    !string.IsNullOrEmpty(idMisionesAsociadas[i]))
                {
                    GameObject goMision = GameObject.Find(idMisionesAsociadas[i]);
                    if (goMision != null)
                    {
                        MisionEntrega mision = goMision.GetComponent<MisionEntrega>();
                        if (mision != null && mision.Completada)
                        {
                            MarcarTareaCompletada(i);
                        }
                    }
                }
            }
        }

        // ── API pública ────────────────────────────────────────────────────────

        /// <summary>
        /// Marca la tarea en el índice dado como completada (tachado semitransparente).
        /// </summary>
        public void MarcarTareaCompletada(int indice)
        {
            if (indice < 0 || indice >= tareas.Length) return;
            if (completadas[indice]) return;

            completadas[indice] = true;

            if (indice < textosEntradas.Count && textosEntradas[indice] != null)
            {
                textosEntradas[indice].text  = "<s>" + tareas[indice] + "</s>";
                textosEntradas[indice].color = new Color(0f, 0f, 0f, 0.45f);
            }
        }

        /// <summary>
        /// Desmarca todas las tareas (para reinicio de nivel).
        /// </summary>
        public void ReiniciarTareas()
        {
            if (tareas == null) return;

            for (int i = 0; i < tareas.Length; i++)
            {
                completadas[i] = false;

                if (i < textosEntradas.Count && textosEntradas[i] != null)
                {
                    textosEntradas[i].text  = tareas[i];
                    textosEntradas[i].color = Color.black;
                }
            }
        }

        // ── Construcción de UI ─────────────────────────────────────────────────

        /// <summary>
        /// Crea el canvas, el panel con Image y los Text de cada tarea.
        /// </summary>
        private void ConstruirUI()
        {
            // — Canvas propio, orden 55, sin GraphicRaycaster —
            GameObject goCanvas = new GameObject("PanelTareasHUD_Canvas");
            goCanvas.transform.SetParent(transform, false);

            _canvas = goCanvas.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 55;

            goCanvas.AddComponent<CanvasScaler>();
            // Sin GraphicRaycaster intencionalmente

            // — Panel —
            float alturaDinamica = AltoBase + tareas.Length * AltoLinea;

            GameObject goPanel = new GameObject("Panel");
            goPanel.transform.SetParent(goCanvas.transform, false);

            _panelRect = goPanel.AddComponent<RectTransform>();
            // Anclado arriba-izquierda, tamaño proporcional al sprite (800×1120) aumentado un 25%
            _panelRect.anchorMin        = new Vector2(0f, 1f);
            _panelRect.anchorMax        = new Vector2(0f, 1f);
            _panelRect.pivot            = new Vector2(0f, 1f);
            _panelRect.anchoredPosition = new Vector2(20f, -100f);
            _panelRect.sizeDelta        = new Vector2(350f, 490f);

            // — Image del panel —
            Image imgPanel = goPanel.AddComponent<Image>();
            Sprite spritePanel = Resources.Load<Sprite>("Sprites/UI/paneles/Panel de tareas");

            if (spritePanel != null)
            {
                imgPanel.sprite          = spritePanel;
                imgPanel.type            = Image.Type.Simple;
                imgPanel.preserveAspect  = true;
            }
            else
            {
                imgPanel.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
            }

            // — Textos de tareas —
            textosEntradas.Clear();
            float margenIzquierdo = 32f;
            float anchoTexto = _panelRect.sizeDelta.x - (margenIzquierdo * 2f);
            float inicioY = 130f; // Debajo del encabezado del cartel, sobre la hoja crema

            for (int i = 0; i < tareas.Length; i++)
            {
                GameObject goTexto = new GameObject("Tarea_" + i);
                goTexto.transform.SetParent(goPanel.transform, false);

                RectTransform rt = goTexto.AddComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0f, 1f);
                rt.anchorMax        = new Vector2(0f, 1f);
                rt.pivot            = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(margenIzquierdo, -(inicioY + i * AltoLinea));
                rt.sizeDelta        = new Vector2(anchoTexto, 30f);

                Text texto = goTexto.AddComponent<Text>();
                texto.font               = ConstructorUI.Fuente;
                texto.fontSize           = 20;
                texto.color              = new Color(0.12f, 0.12f, 0.18f); // Oscuro sobre la hoja crema
                texto.horizontalOverflow = HorizontalWrapMode.Wrap;
                texto.verticalOverflow   = VerticalWrapMode.Overflow;
                texto.supportRichText    = true;
                texto.text               = tareas[i];

                textosEntradas.Add(texto);
            }
        }

        /// <summary>
        /// Recalcula la altura del panel según el número de tareas actuales.
        /// Se puede llamar externamente si las tareas cambian en tiempo de ejecución.
        /// </summary>
        private void ActualizarPanel()
        {
            if (_panelRect == null || tareas == null) return;

            float alturaDinamica = AltoBase + tareas.Length * AltoLinea;
            _panelRect.sizeDelta = new Vector2(_panelRect.sizeDelta.x, alturaDinamica);
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

        // ── Callbacks de eventos ───────────────────────────────────────────────

        private void MostrarPanel()
        {
            if (_canvas != null)
                _canvas.enabled = true;
        }
    }
}
