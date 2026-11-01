using System;
using System.Collections;
using System.Collections.Generic;
using SoltaLaPala.Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoltaLaPala.Dialogue
{
    public class InterfazDialogo : MonoBehaviour
    {
        [Header("Referencias")]
        public GameObject panel;
        public Text textoNombre;
        public Text textoDialogo;
        [Tooltip("Flechita o icono que indica que se puede pasar con espacio.")]
        public GameObject indicadorContinuar;

        [Header("Ajustes")]
        [Tooltip("Letras que aparecen por segundo. Un valor mas alto escribe mas rapido.")]
        public float letrasPorSegundo = 35f;

        [Header("Vibracion de las letras")]
        [Tooltip("Cuanto suben y bajan las letras al aparecer, en pixeles. 0 lo apaga.")]
        public float amplitudVibracion = 6f;
        [Tooltip("Cuanto dura la vibracion de cada letra, en segundos.")]
        public float duracionVibracion = 0.3f;
        [Tooltip("Cuantas veces sube y baja cada letra durante la vibracion.")]
        public float ciclosVibracion = 2f;

        [Header("Respuestas del jugador")]
        [Tooltip("Donde van los botones de respuesta. Si se deja vacio se crea solo, encima del cuadro.")]
        public RectTransform contenedorOpciones;
        public float altoOpcion = 46f;
        public Color colorOpcion = new Color(0.10f, 0.10f, 0.16f, 0.95f);
        public Color colorOpcionResaltada = new Color(0.24f, 0.24f, 0.36f, 1f);
        public Color colorOpcionPulsada = new Color(0.34f, 0.34f, 0.50f, 1f);

        [Header("Ajustes de Texto")]
        [Tooltip("Tamaño de la fuente del nombre del NPC.")]
        [Range(14, 40)]
        public int tamanoFuenteNombre = 28;

        [Tooltip("Tamaño de la fuente del texto del diálogo.")]
        [Range(14, 40)]
        public int tamanoFuenteDialogo = 28;

        [Tooltip("Tamaño de la fuente de las opciones.")]
        [Range(12, 32)]
        public int tamanoFuenteOpciones = 17;

        [Header("Sprites de arte")]
        [Tooltip("Sprite de la caja de diálogo. Se asigna desde el inspector.")]
        public Sprite spriteCajaDialogo;

        [Tooltip("Sprite del panel de opciones. Se asigna desde el inspector.")]
        public Sprite spritePanelOpciones;

        [Tooltip("Barra que se superpone sobre la opción seleccionada/hovered.")]
        public Sprite spriteBarraSeleccionada;

        [Header("Escala y Ajustes de Tamaño")]
        [Tooltip("Escala de la caja de diálogo. Modifícalo en el inspector para agrandar o achicar.")]
        [Range(0.5f, 2.5f)]
        public float escalaCajaDialogo = 1.0f;

        [Tooltip("Escala del panel de opciones. Modifícalo en el inspector para agrandar o achicar.")]
        [Range(0.5f, 2.5f)]
        public float escalaPanelOpciones = 1.0f;

        [Tooltip("Tamaño base de la caja de diálogo.")]
        public Vector2 tamanoBaseCaja = new Vector2(1380f, 386f);

        [Tooltip("Tamaño base del panel de opciones.")]
        public Vector2 tamanoBaseOpciones = new Vector2(490f, 284f);

        [Header("Posición y Desplazamiento")]
        [Tooltip("Offset horizontal y vertical de la caja de diálogo (X horizontal, Y vertical).")]
        public Vector2 offsetCajaDialogo = new Vector2(0f, 30f);

        [Tooltip("Offset horizontal y vertical del panel de opciones respecto a la caja de diálogo (X horizontal, Y vertical).")]
        public Vector2 offsetPanelOpciones = new Vector2(0f, 15f);

        [Header("Espaciado de Opciones")]
        [Tooltip("Espacio vertical entre cada línea/opción en el panel de opciones.")]
        [Range(0f, 40f)]
        public float espacioEntreLineasOpciones = 6f;

        private readonly List<GameObject> botonesOpciones = new List<GameObject>();
        private readonly List<Image> barrasOpciones = new List<Image>();
        private readonly List<Text> textosOpciones = new List<Text>();
        private int opcionSeleccionada = 0;
        private Action<int> accionAlElegir;

        private Coroutine rutinaEscritura;
        private string lineaActualCompleta = "";

        public bool Escribiendo { get; private set; }

        public event System.Action<float> AlEmpezarLinea;

        public event System.Action AlTerminarLinea;

        private EfectoVibracionLetras efectoVibracion;

        private void Awake()
        {
            if (panel == null || textoNombre == null || textoDialogo == null)
            {
                ConstruirInterfazSiFalta();
            }

            spriteCajaDialogo       = CargarSprite(spriteCajaDialogo,       "Sprites/UI/paneles/Caja de dialogo vacia");
            spritePanelOpciones     = CargarSprite(spritePanelOpciones,     "Sprites/UI/paneles/Panel de seleccionar una opcion");
            spriteBarraSeleccionada = CargarSprite(spriteBarraSeleccionada, "Sprites/UI/paneles/Barra de opcion seleccionada");

            if (spriteCajaDialogo != null)
            {
                Image imagenPanel = panel.GetComponent<Image>();
                if (imagenPanel != null)
                {
                    imagenPanel.sprite = spriteCajaDialogo;
                    imagenPanel.color  = Color.white;
                    imagenPanel.preserveAspect = true;
                }

                RectTransform rectPanel = panel.GetComponent<RectTransform>();
                if (rectPanel != null)
                {
                    rectPanel.sizeDelta = tamanoBaseCaja * escalaCajaDialogo;
                    rectPanel.anchoredPosition = offsetCajaDialogo;
                }

                if (textoNombre != null)
                {
                    RectTransform rectNombre = textoNombre.GetComponent<RectTransform>();
                    if (rectNombre != null)
                    {
                        rectNombre.anchorMin = new Vector2(0f, 1f);
                        rectNombre.anchorMax = new Vector2(0f, 1f);
                        rectNombre.pivot     = new Vector2(0f, 1f);
                        rectNombre.anchoredPosition = new Vector2(76f, -28f);
                        rectNombre.sizeDelta = new Vector2(383f, 76f);
                    }
                    textoNombre.alignment = TextAnchor.MiddleCenter;
                    textoNombre.fontSize = 28;
                    textoNombre.color = new Color(0.12f, 0.12f, 0.18f);
                }

                if (textoDialogo != null)
                {
                    RectTransform rectFrase = textoDialogo.GetComponent<RectTransform>();
                    if (rectFrase != null)
                    {
                        rectFrase.anchorMin = new Vector2(0f, 0f);
                        rectFrase.anchorMax = new Vector2(1f, 1f);
                        rectFrase.offsetMin = new Vector2(76f, 35f);
                        rectFrase.offsetMax = new Vector2(-76f, -115f);
                    }
                    textoDialogo.color = Color.white;
                    textoDialogo.fontSize = 28;
                }

                if (indicadorContinuar != null)
                {
                    RectTransform rectContinuar = indicadorContinuar.GetComponent<RectTransform>();
                    if (rectContinuar != null)
                    {
                        rectContinuar.anchoredPosition = new Vector2(-46f, 23f);
                    }
                }
            }

            if (textoDialogo != null)
            {
                efectoVibracion = textoDialogo.GetComponent<EfectoVibracionLetras>();
                if (efectoVibracion == null)
                {
                    efectoVibracion = textoDialogo.gameObject.AddComponent<EfectoVibracionLetras>();
                }
            }

            Cerrar();
        }

        private void ConstruirInterfazSiFalta()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;

            CanvasScaler escalador = canvas.GetComponent<CanvasScaler>();
            if (escalador == null)
            {
                escalador = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            if (panel == null)
            {
                panel = ConstructorUI.CrearPanel("PanelDialogo", canvas.transform, tamanoBaseCaja);
                RectTransform rectPanel = panel.GetComponent<RectTransform>();
                rectPanel.anchorMin = new Vector2(0.5f, 0f);
                rectPanel.anchorMax = new Vector2(0.5f, 0f);
                rectPanel.pivot = new Vector2(0.5f, 0f);
                rectPanel.anchoredPosition = offsetCajaDialogo;
            }

            if (textoNombre == null)
            {
                textoNombre = ConstructorUI.CrearTexto("NombreNPC", panel.transform, "",
                    tamanoFuenteNombre, TextAnchor.MiddleCenter,
                    new Vector2(76f, -28f), new Vector2(383f, 76f));
            }
            textoNombre.rectTransform.anchorMin = new Vector2(0f, 1f);
            textoNombre.rectTransform.anchorMax = new Vector2(0f, 1f);
            textoNombre.rectTransform.pivot = new Vector2(0f, 1f);

            if (textoDialogo == null)
            {
                textoDialogo = ConstructorUI.CrearTexto("TextoDialogo", panel.transform, "",
                    tamanoFuenteDialogo, TextAnchor.MiddleLeft,
                    Vector2.zero, Vector2.zero);
            }
            textoDialogo.rectTransform.anchorMin = Vector2.zero;
            textoDialogo.rectTransform.anchorMax = Vector2.one;
            textoDialogo.rectTransform.offsetMin = new Vector2(76f, 35f);
            textoDialogo.rectTransform.offsetMax = new Vector2(-76f, -115f);

            if (indicadorContinuar == null)
            {
                Text indicador = ConstructorUI.CrearTexto("IndicadorContinuar", panel.transform, "▼",
                    20, TextAnchor.MiddleCenter, new Vector2(-46f, 23f), new Vector2(40f, 32f));
                indicador.raycastTarget = false;
                indicadorContinuar = indicador.gameObject;
            }

            RectTransform rectIndicador = indicadorContinuar.GetComponent<RectTransform>();
            if (rectIndicador != null)
            {
                rectIndicador.anchorMin = new Vector2(1f, 0f);
                rectIndicador.anchorMax = new Vector2(1f, 0f);
                rectIndicador.pivot = new Vector2(1f, 0f);
                rectIndicador.anchoredPosition = new Vector2(-46f, 23f);
            }
        }

        private void Update()
        {
            if (textoNombre != null && textoNombre.fontSize != tamanoFuenteNombre)
                textoNombre.fontSize = tamanoFuenteNombre;

            if (textoDialogo != null && textoDialogo.fontSize != tamanoFuenteDialogo)
                textoDialogo.fontSize = tamanoFuenteDialogo;

            for (int i = 0; i < textosOpciones.Count; i++)
            {
                if (textosOpciones[i] != null && textosOpciones[i].fontSize != tamanoFuenteOpciones)
                    textosOpciones[i].fontSize = tamanoFuenteOpciones;
            }

            if (panel != null && panel.activeSelf)
            {
                RectTransform rectPanel = panel.GetComponent<RectTransform>();
                if (rectPanel != null)
                {
                    Vector2 tamanoDeseado = tamanoBaseCaja * escalaCajaDialogo;
                    if (rectPanel.sizeDelta != tamanoDeseado)
                        rectPanel.sizeDelta = tamanoDeseado;

                    if (rectPanel.anchoredPosition != offsetCajaDialogo)
                        rectPanel.anchoredPosition = offsetCajaDialogo;
                }
            }

            if (contenedorOpciones != null && contenedorOpciones.gameObject.activeSelf)
            {
                Vector2 tamanoDeseadoOpciones = tamanoBaseOpciones * escalaPanelOpciones;
                if (contenedorOpciones.sizeDelta != tamanoDeseadoOpciones)
                    contenedorOpciones.sizeDelta = tamanoDeseadoOpciones;

                if (contenedorOpciones.anchoredPosition != offsetPanelOpciones)
                    contenedorOpciones.anchoredPosition = offsetPanelOpciones;

                VerticalLayoutGroup columna = contenedorOpciones.GetComponent<VerticalLayoutGroup>();
                if (columna != null && !Mathf.Approximately(columna.spacing, espacioEntreLineasOpciones))
                    columna.spacing = espacioEntreLineasOpciones;

                if (botonesOpciones.Count > 0)
                {
                    if (Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        int nuevo = opcionSeleccionada - 1;
                        if (nuevo < 0) nuevo = botonesOpciones.Count - 1;
                        ResaltarOpcion(nuevo);
                    }
                    else if (Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        int nuevo = opcionSeleccionada + 1;
                        if (nuevo >= botonesOpciones.Count) nuevo = 0;
                        ResaltarOpcion(nuevo);
                    }
                    else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                    {
                        ElegirOpcion(opcionSeleccionada);
                    }
                }
            }
        }

        public void Abrir(string nombreNpc)
        {
            if (panel != null) panel.SetActive(true);
            if (textoNombre != null) textoNombre.text = nombreNpc;
            if (textoDialogo != null) textoDialogo.text = "";
            if (indicadorContinuar != null) indicadorContinuar.SetActive(false);
        }

        public void Cerrar()
        {
            if (rutinaEscritura != null)
            {
                StopCoroutine(rutinaEscritura);
                rutinaEscritura = null;
            }
            if (Escribiendo)
            {
                Escribiendo = false;
                AlTerminarLinea?.Invoke();
            }
            OcultarOpciones();
            if (panel != null) panel.SetActive(false);
        }

        public void MostrarOpciones(IList<string> textos, Action<int> alElegir)
        {
            OcultarOpciones();
            AsegurarContenedorOpciones();
            ConstructorUI.AsegurarEventSystem();

            accionAlElegir = alElegir;
            int total = Mathf.Min(textos.Count, 4);
            for (int i = 0; i < total; i++)
            {
                int indice = i;
                botonesOpciones.Add(CrearBotonOpcion($"{i + 1}. {textos[i]}", () => ElegirOpcion(indice)));
            }

            contenedorOpciones.gameObject.SetActive(true);

            if (botonesOpciones.Count > 0)
            {
                ResaltarOpcion(0);
            }

            if (indicadorContinuar != null) indicadorContinuar.SetActive(false);
        }

        public void OcultarOpciones()
        {
            foreach (GameObject boton in botonesOpciones)
            {
                Destroy(boton);
            }
            botonesOpciones.Clear();
            barrasOpciones.Clear();
            textosOpciones.Clear();
            opcionSeleccionada = 0;
            accionAlElegir = null;

            if (contenedorOpciones != null) contenedorOpciones.gameObject.SetActive(false);
        }

        public void ResaltarOpcion(int indice)
        {
            if (indice < 0 || indice >= botonesOpciones.Count) return;
            opcionSeleccionada = indice;

            for (int i = 0; i < barrasOpciones.Count; i++)
            {
                if (barrasOpciones[i] != null)
                {
                    barrasOpciones[i].color = (i == indice) ? Color.white : Color.clear;
                }
            }
        }

        public void ElegirOpcion(int indice)
        {
            if (indice < 0 || indice >= botonesOpciones.Count) return;
            Action<int> callback = accionAlElegir;
            OcultarOpciones();
            callback?.Invoke(indice);
        }

        private void AsegurarContenedorOpciones()
        {
            if (contenedorOpciones != null)
            {
                return;
            }

            GameObject objeto = new GameObject("Opciones", typeof(RectTransform));
            objeto.transform.SetParent(panel.transform, false);

            contenedorOpciones = (RectTransform)objeto.transform;

            if (spritePanelOpciones != null)
            {
                contenedorOpciones.anchorMin = new Vector2(0.5f, 1f);
                contenedorOpciones.anchorMax = new Vector2(0.5f, 1f);
                contenedorOpciones.pivot     = new Vector2(0.5f, 0f);
                contenedorOpciones.anchoredPosition = offsetPanelOpciones;
                contenedorOpciones.sizeDelta = tamanoBaseOpciones * escalaPanelOpciones;

                Image imagenFondo = objeto.AddComponent<Image>();
                imagenFondo.sprite          = spritePanelOpciones;
                imagenFondo.type            = Image.Type.Simple;
                imagenFondo.preserveAspect  = true;
                imagenFondo.color           = Color.white;

                VerticalLayoutGroup columna = objeto.AddComponent<VerticalLayoutGroup>();
                columna.padding.top          = 15;
                columna.padding.bottom       = 15;
                columna.padding.left         = 20;
                columna.padding.right        = 20;
                columna.spacing              = espacioEntreLineasOpciones;
                columna.childControlWidth    = true;
                columna.childControlHeight   = true;
                columna.childForceExpandWidth  = true;
                columna.childForceExpandHeight = false;
            }
            else
            {
                contenedorOpciones.anchorMin = new Vector2(0f, 1f);
                contenedorOpciones.anchorMax = new Vector2(1f, 1f);
                contenedorOpciones.pivot     = new Vector2(0.5f, 0f);
                contenedorOpciones.anchoredPosition = offsetPanelOpciones;
                contenedorOpciones.sizeDelta = Vector2.zero;
                objeto.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                VerticalLayoutGroup columna = objeto.AddComponent<VerticalLayoutGroup>();
                columna.spacing              = espacioEntreLineasOpciones;
                columna.childControlWidth    = true;
                columna.childControlHeight   = true;
                columna.childForceExpandWidth  = true;
                columna.childForceExpandHeight = false;
            }
        }

        private GameObject CrearBotonOpcion(string texto, Action alPulsar)
        {
            int miIndice = botonesOpciones.Count;

            GameObject objeto = new GameObject("Opcion", typeof(RectTransform));
            objeto.transform.SetParent(contenedorOpciones, false);

            float alturaBoton = spriteBarraSeleccionada != null ? 46f : altoOpcion;
            objeto.AddComponent<LayoutElement>().preferredHeight = alturaBoton;

            EventTrigger trigger = objeto.AddComponent<EventTrigger>();
            EventTrigger.Entry entryHover = new EventTrigger.Entry();
            entryHover.eventID = EventTriggerType.PointerEnter;
            entryHover.callback.AddListener((eventData) => { ResaltarOpcion(miIndice); });
            trigger.triggers.Add(entryHover);

            if (spriteBarraSeleccionada != null)
            {
                Image fondoTransparente = objeto.AddComponent<Image>();
                fondoTransparente.color = Color.clear;
                fondoTransparente.raycastTarget = true;

                GameObject seleccionadoImg = new GameObject("BarraSeleccion", typeof(RectTransform));
                seleccionadoImg.transform.SetParent(objeto.transform, false);

                RectTransform rectBarra = (RectTransform)seleccionadoImg.transform;
                rectBarra.anchorMin = Vector2.zero;
                rectBarra.anchorMax = Vector2.one;
                rectBarra.offsetMin = Vector2.zero;
                rectBarra.offsetMax = Vector2.zero;

                Image imagenBarra = seleccionadoImg.AddComponent<Image>();
                imagenBarra.sprite = spriteBarraSeleccionada;
                imagenBarra.type   = Image.Type.Simple;
                imagenBarra.color  = Color.clear;
                imagenBarra.raycastTarget = false;

                barrasOpciones.Add(imagenBarra);

                Button boton = objeto.AddComponent<Button>();
                boton.targetGraphic = fondoTransparente;
                boton.transition = Selectable.Transition.None;
                boton.onClick.AddListener(() => alPulsar());
            }
            else
            {
                Image fondo = objeto.AddComponent<Image>();
                fondo.color = Color.white;
                barrasOpciones.Add(fondo);

                Button boton = objeto.AddComponent<Button>();
                boton.targetGraphic = fondo;
                ColorBlock colores = ColorBlock.defaultColorBlock;
                colores.normalColor      = colorOpcion;
                colores.highlightedColor = colorOpcionResaltada;
                colores.pressedColor     = colorOpcionPulsada;
                colores.selectedColor    = colorOpcion;
                colores.fadeDuration     = 0.05f;
                boton.colors = colores;
                boton.onClick.AddListener(() => alPulsar());
            }

            GameObject objetoTexto = new GameObject("Texto", typeof(RectTransform));
            objetoTexto.transform.SetParent(objeto.transform, false);

            RectTransform rectTexto = (RectTransform)objetoTexto.transform;
            rectTexto.anchorMin = Vector2.zero;
            rectTexto.anchorMax = Vector2.one;
            rectTexto.offsetMin = new Vector2(25f, 0f);
            rectTexto.offsetMax = new Vector2(-25f, 0f);

            Text etiqueta = objetoTexto.AddComponent<Text>();
            etiqueta.text          = texto;
            etiqueta.font          = textoDialogo != null ? textoDialogo.font : ConstructorUI.Fuente;
            etiqueta.fontSize      = tamanoFuenteOpciones;
            etiqueta.alignment     = TextAnchor.MiddleLeft;
            etiqueta.color         = Color.white;
            etiqueta.raycastTarget = false;

            textosOpciones.Add(etiqueta);

            return objeto;
        }

        public void MostrarLinea(string linea)
        {
            if (rutinaEscritura != null)
            {
                StopCoroutine(rutinaEscritura);
            }
            rutinaEscritura = StartCoroutine(EscribirLinea(linea));
        }

        public void CompletarLinea()
        {
            if (!Escribiendo) return;

            if (rutinaEscritura != null)
            {
                StopCoroutine(rutinaEscritura);
                rutinaEscritura = null;
            }
            Escribiendo = false;
            if (textoDialogo != null) textoDialogo.text = lineaActualCompleta;
            if (efectoVibracion != null) efectoVibracion.Detener();
            if (indicadorContinuar != null) indicadorContinuar.SetActive(true);
            AlTerminarLinea?.Invoke();
        }

        private IEnumerator EscribirLinea(string linea)
        {
            Escribiendo = true;
            lineaActualCompleta = linea;
            if (textoDialogo != null) textoDialogo.text = "";
            if (indicadorContinuar != null) indicadorContinuar.SetActive(false);

            float delay = 1f / Mathf.Max(letrasPorSegundo, 1f);

            if (efectoVibracion != null)
            {
                efectoVibracion.Configurar(amplitudVibracion, duracionVibracion, ciclosVibracion, delay);
            }

            AlEmpezarLinea?.Invoke(linea.Length * delay);

            for (int i = 0; i < linea.Length; i++)
            {
                if (textoDialogo != null)
                {
                    textoDialogo.text = linea.Substring(0, i + 1);
                }

                if (efectoVibracion != null) efectoVibracion.LetraNueva();

                yield return new WaitForSecondsRealtime(delay);
            }

            Escribiendo = false;
            rutinaEscritura = null;
            if (indicadorContinuar != null) indicadorContinuar.SetActive(true);
            AlTerminarLinea?.Invoke();
        }

        private static Sprite CargarSprite(Sprite asignado, string rutaResources)
        {
            return asignado != null ? asignado : Resources.Load<Sprite>(rutaResources);
        }
    }
}
