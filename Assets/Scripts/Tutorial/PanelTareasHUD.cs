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
        public float escala = 1.07f;

        [Tooltip("Margen respecto a la esquina superior izquierda (X a la derecha, Y hacia abajo).")]
        public Vector2 margen = new Vector2(0.4f, 100f);

        [Tooltip("Tamaño base del panel en píxeles de referencia.")]
        public Vector2 tamanoBase = new Vector2(350f, 490f);

        [Header("Ajustes de Texto y Espaciado")]
        [Tooltip("Tamaño base de la fuente de las tareas (escala automáticamente con el panel).")]
        [Range(12, 36)]
        public int tamanoFuente = 20;

        [Tooltip("Espacio vertical entre cada línea de tarea.")]
        [Range(15f, 90f)]
        public float espacioEntreLineas = 59.4f;

        [Tooltip("Margen vertical desde arriba hacia la primera tarea.")]
        public float offsetVerticalInicial = 134.6f;

        [Tooltip("Margen u offset horizontal desde el borde izquierdo hacia las tareas.")]
        [Range(0f, 150f)]
        public float offsetHorizontalTareas = 76.67f;

        // ── Estado interno ─────────────────────────────────────────────────────

        private Canvas          _canvas;
        private RectTransform   _panelRect;
        private List<Text>      textosEntradas  = new List<Text>();
        private List<Image>     lineasTachado   = new List<Image>();
        private List<Image>     lineasTachado2  = new List<Image>();
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

            // Permite modificar tamaño, posición, fuente y espaciado desde el inspector de Unity en tiempo real
            if (_panelRect != null)
            {
                Vector2 tamanoDeseado = tamanoBase * escala;
                Vector2 posDeseada = new Vector2(margen.x, -margen.y);

                if (_panelRect.sizeDelta != tamanoDeseado)
                    _panelRect.sizeDelta = tamanoDeseado;

                if (_panelRect.anchoredPosition != posDeseada)
                    _panelRect.anchoredPosition = posDeseada;

                // Las palabras y el espaciado escalan con el panel y responden a los ajustes del Inspector
                float margenIzqActual = offsetHorizontalTareas * escala;
                float anchoTextoActual = _panelRect.sizeDelta.x - (margenIzqActual * 2f);
                int fuenteEscalada = Mathf.Max(10, Mathf.RoundToInt(tamanoFuente * escala));
                float espacioActual = espacioEntreLineas * escala;
                float inicioYActual = offsetVerticalInicial * escala;

                for (int i = 0; i < textosEntradas.Count; i++)
                {
                    if (textosEntradas[i] == null) continue;

                    textosEntradas[i].fontSize = fuenteEscalada;

                    RectTransform rt = textosEntradas[i].GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(margenIzqActual, -(inicioYActual + i * espacioActual));
                        rt.sizeDelta = new Vector2(anchoTextoActual, 50f * escala);
                    }

                    if (completadas != null && i < completadas.Length && completadas[i])
                    {
                        ActualizarTachado(i, anchoTextoActual);
                    }
                }
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
        /// Marca la tarea en el índice dado como completada (tachando todas las letras aunque ocupen varias líneas).
        /// </summary>
        public void MarcarTareaCompletada(int indice)
        {
            if (indice < 0 || indice >= tareas.Length) return;
            if (completadas[indice]) return;

            completadas[indice] = true;

            if (indice < textosEntradas.Count && textosEntradas[indice] != null)
            {
                textosEntradas[indice].text  = tareas[indice];
                textosEntradas[indice].color = new Color(0.12f, 0.12f, 0.18f, 0.40f); // Tinta apagada

                float margenIzqActual = offsetHorizontalTareas * escala;
                float anchoTextoActual = _panelRect.sizeDelta.x - (margenIzqActual * 2f);
                ActualizarTachado(indice, anchoTextoActual);
            }
        }

        private void ActualizarTachado(int i, float anchoTextoActual)
        {
            if (i >= textosEntradas.Count || textosEntradas[i] == null) return;
            if (i >= lineasTachado.Count || lineasTachado[i] == null) return;

            Text texto = textosEntradas[i];
            float grosor = Mathf.Max(2.5f, 3.5f * escala);

            // Verificamos si el texto ocupa más de 1 línea para tachar todo
            bool esMultilinea = texto.preferredWidth > (anchoTextoActual - 10f) || (tareas != null && i < tareas.Length && tareas[i].Length > 20);

            // Línea 1 (cubre la primera línea)
            lineasTachado[i].gameObject.SetActive(true);
            float ancho1 = esMultilinea ? (anchoTextoActual + 8f * escala) : Mathf.Min(texto.preferredWidth + 14f * escala, anchoTextoActual + 8f * escala);
            lineasTachado[i].rectTransform.sizeDelta = new Vector2(ancho1, grosor);
            lineasTachado[i].rectTransform.anchoredPosition = new Vector2(-4f, -12f * escala);

            // Línea 2 (cubre la segunda línea si el texto se extendió)
            if (i < lineasTachado2.Count && lineasTachado2[i] != null)
            {
                if (esMultilinea)
                {
                    lineasTachado2[i].gameObject.SetActive(true);
                    float ancho2 = anchoTextoActual + 8f * escala;
                    lineasTachado2[i].rectTransform.sizeDelta = new Vector2(ancho2, grosor);
                    lineasTachado2[i].rectTransform.anchoredPosition = new Vector2(-4f, -34f * escala);
                }
                else
                {
                    lineasTachado2[i].gameObject.SetActive(false);
                }
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
                    textosEntradas[i].color = new Color(0.12f, 0.12f, 0.18f, 1f);
                }

                if (i < lineasTachado.Count && lineasTachado[i] != null)
                {
                    lineasTachado[i].gameObject.SetActive(false);
                }

                if (i < lineasTachado2.Count && lineasTachado2[i] != null)
                {
                    lineasTachado2[i].gameObject.SetActive(false);
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
            GameObject goPanel = new GameObject("Panel");
            goPanel.transform.SetParent(goCanvas.transform, false);

            _panelRect = goPanel.AddComponent<RectTransform>();
            // Anclado arriba-izquierda, tamaño proporcional al sprite (800×1120) aumentado un 25%
            _panelRect.anchorMin        = new Vector2(0f, 1f);
            _panelRect.anchorMax        = new Vector2(0f, 1f);
            _panelRect.pivot            = new Vector2(0f, 1f);
            _panelRect.anchoredPosition = new Vector2(margen.x, -margen.y);
            _panelRect.sizeDelta        = tamanoBase * escala;

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
            lineasTachado.Clear();
            lineasTachado2.Clear();
            float margenIzquierdo = offsetHorizontalTareas * escala;
            float anchoTexto = _panelRect.sizeDelta.x - (margenIzquierdo * 2f);
            float inicioY = offsetVerticalInicial * escala;

            for (int i = 0; i < tareas.Length; i++)
            {
                GameObject goTexto = new GameObject("Tarea_" + i);
                goTexto.transform.SetParent(goPanel.transform, false);

                RectTransform rt = goTexto.AddComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0f, 1f);
                rt.anchorMax        = new Vector2(0f, 1f);
                rt.pivot            = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(margenIzquierdo, -(inicioY + i * espacioEntreLineas * escala));
                rt.sizeDelta        = new Vector2(anchoTexto, 50f * escala);

                Text texto = goTexto.AddComponent<Text>();
                texto.font               = ConstructorUI.Fuente;
                texto.fontSize           = Mathf.Max(10, Mathf.RoundToInt(tamanoFuente * escala));
                texto.color              = new Color(0.12f, 0.12f, 0.18f); // Oscuro sobre la hoja crema
                texto.horizontalOverflow = HorizontalWrapMode.Wrap;
                texto.verticalOverflow   = VerticalWrapMode.Overflow;
                texto.supportRichText    = true;
                texto.text               = tareas[i];

                textosEntradas.Add(texto);

                // Línea 1 de tachado (primera línea de texto)
                GameObject goTachado1 = new GameObject("LineaTachado_1", typeof(RectTransform));
                goTachado1.transform.SetParent(goTexto.transform, false);

                RectTransform rtTachado1 = (RectTransform)goTachado1.transform;
                rtTachado1.anchorMin        = new Vector2(0f, 1f);
                rtTachado1.anchorMax        = new Vector2(0f, 1f);
                rtTachado1.pivot            = new Vector2(0f, 1f);
                rtTachado1.anchoredPosition = new Vector2(-4f, -12f * escala);
                rtTachado1.sizeDelta        = new Vector2(0f, Mathf.Max(2.5f, 3.5f * escala));

                Image imgTachado1 = goTachado1.AddComponent<Image>();
                imgTachado1.color = new Color(0.15f, 0.15f, 0.22f, 0.90f);
                imgTachado1.raycastTarget = false;
                goTachado1.SetActive(false);
                lineasTachado.Add(imgTachado1);

                // Línea 2 de tachado (para cuando el texto se extiende a una segunda línea)
                GameObject goTachado2 = new GameObject("LineaTachado_2", typeof(RectTransform));
                goTachado2.transform.SetParent(goTexto.transform, false);

                RectTransform rtTachado2 = (RectTransform)goTachado2.transform;
                rtTachado2.anchorMin        = new Vector2(0f, 1f);
                rtTachado2.anchorMax        = new Vector2(0f, 1f);
                rtTachado2.pivot            = new Vector2(0f, 1f);
                rtTachado2.anchoredPosition = new Vector2(-4f, -34f * escala);
                rtTachado2.sizeDelta        = new Vector2(0f, Mathf.Max(2.5f, 3.5f * escala));

                Image imgTachado2 = goTachado2.AddComponent<Image>();
                imgTachado2.color = new Color(0.15f, 0.15f, 0.22f, 0.90f);
                imgTachado2.raycastTarget = false;
                goTachado2.SetActive(false);
                lineasTachado2.Add(imgTachado2);
            }
        }

        /// <summary>
        /// Recalcula el tamaño y posición del panel según las configuraciones de escala actuales.
        /// Se puede llamar externamente si las tareas cambian en tiempo de ejecución.
        /// </summary>
        public void ActualizarPanel()
        {
            if (_panelRect == null) return;

            _panelRect.sizeDelta = tamanoBase * escala;
            _panelRect.anchoredPosition = new Vector2(margen.x, -margen.y);
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
