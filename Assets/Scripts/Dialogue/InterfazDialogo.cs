using System;
using System.Collections;
using System.Collections.Generic;
using SoltaLaPala.Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoltaLaPala.Dialogue
{
    // Cuadro de dialogo: rectangulo con el nombre del NPC arriba
    // y el texto de la frase escribiendose letra a letra, con las letras vibrando
    // de arriba para abajo al aparecer.
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

        private readonly List<GameObject> botonesOpciones = new List<GameObject>();
        private readonly List<Image> barrasOpciones = new List<Image>();
        private readonly List<Text> textosOpciones = new List<Text>();
        private int opcionSeleccionada = 0;
        private Action<int> accionAlElegir;

        private Coroutine rutinaEscritura;
        private string lineaActualCompleta = "";

        // True mientras el texto se esta escribiendo letra a letra.
        public bool Escribiendo { get; private set; }

        // Se dispara al empezar a escribir una linea. Parametro: segundos que va a
        // tardar en escribirse entera. Es el gancho para el sonido de balbuceo: que
        // suene lo mismo que dura el texto.
        public event System.Action<float> AlEmpezarLinea;

        // Se dispara cuando la linea deja de escribirse: porque termino sola, porque
        // el jugador la completo con espacio o porque se cerro el cuadro.
        // El balbuceo tiene que cortarse aca.
        public event System.Action AlTerminarLinea;

        private EfectoVibracionLetras efectoVibracion;

        private void Awake()
        {
            if (panel == null) panel = gameObject;

            // Rellenar sprites desde Resources si no se asignaron en el inspector.
            spriteCajaDialogo       = CargarSprite(spriteCajaDialogo,       "Sprites/UI/paneles/Caja de dialogo vacia");
            spritePanelOpciones     = CargarSprite(spritePanelOpciones,     "Sprites/UI/paneles/Panel de seleccionar una opcion");
            spriteBarraSeleccionada = CargarSprite(spriteBarraSeleccionada, "Sprites/UI/paneles/Barra de opcion seleccionada");

            // Aplicar el sprite de fondo a la caja de dialogo si tiene Image
            // y ajustar sus dimensiones al tamaño oficial (+15% de escala).
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

            // El efecto se anade solo al texto del dialogo: asi no hay que tocar
            // la escena ni los prefabs para que las letras vibren.
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

        // Permite ajustar las escalas de la caja de diálogo y panel de opciones,
        // así como el tamaño de las fuentes, y manejar la navegación por teclado (flechas arriba/abajo).
        private void Update()
        {
            // Ajuste en tiempo real de los tamaños de fuente
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
                }
            }

            if (contenedorOpciones != null && contenedorOpciones.gameObject.activeSelf)
            {
                Vector2 tamanoDeseadoOpciones = tamanoBaseOpciones * escalaPanelOpciones;
                if (contenedorOpciones.sizeDelta != tamanoDeseadoOpciones)
                    contenedorOpciones.sizeDelta = tamanoDeseadoOpciones;

                // Navegación con flechas de arriba y abajo entre las opciones
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

        // Abre el cuadro de dialogo y pone el nombre del NPC.
        public void Abrir(string nombreNpc)
        {
            if (panel != null) panel.SetActive(true);
            if (textoNombre != null) textoNombre.text = nombreNpc;
            if (textoDialogo != null) textoDialogo.text = "";
            if (indicadorContinuar != null) indicadorContinuar.SetActive(false);
        }

        // Cierra el cuadro y limpia el texto.
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

        // Muestra un boton por respuesta, numerados desde 1. alElegir recibe el
        // indice (desde 0) de la que se toco con el mouse o se eligio con teclado.
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

            // Resaltar la primera opción por defecto
            if (botonesOpciones.Count > 0)
            {
                ResaltarOpcion(0);
            }

            // Mientras hay que elegir, ESPACIO no avanza el diálogo regular.
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

        // Columna centrada encima del cuadro de dialogo con el panel de opciones
        // con su sprite disenado (1000x580) y 4 slots proporcionados.
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
                // Anclado arriba-centro del diálogo, tamaño proporcional al sprite (1000×580) reducido un 30%
                contenedorOpciones.anchorMin = new Vector2(0.5f, 1f);
                contenedorOpciones.anchorMax = new Vector2(0.5f, 1f);
                contenedorOpciones.pivot     = new Vector2(0.5f, 0f);
                contenedorOpciones.anchoredPosition = new Vector2(0f, 15f);
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
                columna.spacing              = 6f;
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
                contenedorOpciones.anchoredPosition = new Vector2(0f, 10f);
                contenedorOpciones.sizeDelta = Vector2.zero;
                objeto.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                VerticalLayoutGroup columna = objeto.AddComponent<VerticalLayoutGroup>();
                columna.spacing              = 6f;
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

            // Trigger para activar la barra seleccionada cuando se pasa el mouse por encima
            EventTrigger trigger = objeto.AddComponent<EventTrigger>();
            EventTrigger.Entry entryHover = new EventTrigger.Entry();
            entryHover.eventID = EventTriggerType.PointerEnter;
            entryHover.callback.AddListener((eventData) => { ResaltarOpcion(miIndice); });
            trigger.triggers.Add(entryHover);

            if (spriteBarraSeleccionada != null)
            {
                // Con sprite: la Image base es transparente pero recibe clicks y hover
                Image fondoTransparente = objeto.AddComponent<Image>();
                fondoTransparente.color = Color.clear;
                fondoTransparente.raycastTarget = true;

                // Hijo con la barra de seleccion, estirado para cubrir todo el padre.
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
                boton.transition = Selectable.Transition.None; // El resaltado lo controla ResaltarOpcion
                boton.onClick.AddListener(() => alPulsar());
            }
            else
            {
                // Sin sprite: comportamiento anterior con colores sobre el fondo plano.
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

        // Empieza a escribir una linea letra a letra.
        public void MostrarLinea(string linea)
        {
            if (rutinaEscritura != null)
            {
                StopCoroutine(rutinaEscritura);
            }
            rutinaEscritura = StartCoroutine(EscribirLinea(linea));
        }

        // Corta la animacion de escritura y muestra la linea entera de golpe.
        // Es lo que pasa al pulsar espacio a media frase.
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
            // La linea aparece entera de golpe: sin vibracion.
            if (efectoVibracion != null) efectoVibracion.Detener();
            if (indicadorContinuar != null) indicadorContinuar.SetActive(true);
            AlTerminarLinea?.Invoke();
        }

        // Corrutina que va anadiendo una letra por vez al texto segun letrasPorSegundo.
        //
        // Espera en tiempo real y no en tiempo de juego: mientras dura el dialogo el
        // juego esta congelado (timeScale 0) y con WaitForSeconds el texto no avanzaria.
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

        // Carga el sprite del path de Resources si el campo del inspector está vacío.
        // Así el prefab funciona incluso si alguien olvida asignarlo a mano.
        private static Sprite CargarSprite(Sprite asignado, string rutaResources)
        {
            return asignado != null ? asignado : Resources.Load<Sprite>(rutaResources);
        }
    }
}
