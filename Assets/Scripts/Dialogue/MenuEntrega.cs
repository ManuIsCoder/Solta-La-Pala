using System.Collections.Generic;
using SoltaLaPala.Interaction;
using SoltaLaPala.Menus;
using SoltaLaPala.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoltaLaPala.Dialogue
{
    // Menú para elegir qué le entregas a un NPC que te encargó algo (ej: Pala / Palita).
    // Utiliza el mismo panel de opciones y sprites que las opciones de diálogo y el sabotaje.
    // Las opciones que no se tienen aparecen en pantalla pero atenuadas y no se pueden seleccionar.
    public class MenuEntrega : MonoBehaviour
    {
        public static MenuEntrega Instancia { get; private set; }

        [Header("Sprites de arte")]
        [Tooltip("Sprite del panel de opciones (1000×580). Se carga de Resources si está vacío.")]
        public Sprite spritePanelOpciones;

        [Tooltip("Barra de selección superpuesta sobre la opción con hover/seleccionada.")]
        public Sprite spriteBarraSeleccionada;

        [Header("Ajustes de Tamaño y Posición")]
        public Vector2 tamanoBase = new Vector2(490f, 284f);

        [Range(0.5f, 2.5f)]
        public float escala = 1.0f;

        public Vector2 offsetPanel = Vector2.zero;

        [Header("Espaciado y Fuente")]
        [Range(0f, 40f)]
        public float espacioEntreLineasOpciones = 6f;

        [Range(12, 32)]
        public int tamanoFuenteOpciones = 17;

        [Header("Controles")]
        [Tooltip("Tecla que cierra el menú sin entregar nada.")]
        public KeyCode teclaSalir = KeyCode.X;

        // Canvas por encima del diálogo (20) y debajo de pausa (120)
        private const int OrdenCanvas = 40;

        private GameObject panel;
        private VerticalLayoutGroup columnaLayout;
        private MisionEntrega misionActual;

        private readonly List<GameObject> botones = new List<GameObject>();
        private readonly List<Image> barrasSeleccion = new List<Image>();
        private readonly List<Text> textosBotones = new List<Text>();
        private readonly List<MisionEntrega.Opcion> opcionesActuales = new List<MisionEntrega.Opcion>();
        private int opcionSeleccionada = -1;

        public bool Abierto => panel != null && panel.activeSelf;

        // Congela al jugador mientras el menú está abierto.
        private readonly ControlJugador controlJugador = new ControlJugador();

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            spritePanelOpciones     = CargarSprite(spritePanelOpciones,     "Sprites/UI/paneles/Panel de seleccionar una opcion");
            spriteBarraSeleccionada = CargarSprite(spriteBarraSeleccionada, "Sprites/UI/paneles/Barra de opcion seleccionada");

            Construir();
        }

        private static Sprite CargarSprite(Sprite asignado, string rutaResources)
        {
            return asignado != null ? asignado : Resources.Load<Sprite>(rutaResources);
        }

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
            }
        }

        private void Update()
        {
            if (!Abierto) return;

            // Mantener tamaño, posición y espaciado en tiempo real
            if (panel != null)
            {
                RectTransform rtPanel = panel.GetComponent<RectTransform>();
                if (rtPanel != null)
                {
                    Vector2 tamanoDeseado = tamanoBase * escala;
                    if (rtPanel.sizeDelta != tamanoDeseado)
                        rtPanel.sizeDelta = tamanoDeseado;

                    if (rtPanel.anchoredPosition != offsetPanel)
                        rtPanel.anchoredPosition = offsetPanel;
                }

                if (columnaLayout != null && !Mathf.Approximately(columnaLayout.spacing, espacioEntreLineasOpciones))
                    columnaLayout.spacing = espacioEntreLineasOpciones;

                for (int i = 0; i < textosBotones.Count; i++)
                {
                    if (textosBotones[i] != null && textosBotones[i].fontSize != tamanoFuenteOpciones)
                        textosBotones[i].fontSize = tamanoFuenteOpciones;
                }
            }

            // Navegación por teclado (flechas arriba/abajo)
            if (opcionesActuales.Count > 0)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    MoverSeleccion(-1);
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    MoverSeleccion(1);
                }
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    if (opcionSeleccionada >= 0 && opcionSeleccionada < opcionesActuales.Count)
                    {
                        if (opcionesActuales[opcionSeleccionada].disponible)
                        {
                            Elegir(opcionesActuales[opcionSeleccionada]);
                        }
                    }
                }
            }

            if (Input.GetKeyDown(teclaSalir) || Input.GetKeyDown(KeyCode.Escape))
            {
                Cerrar();
            }
        }

        private void MoverSeleccion(int direccion)
        {
            int total = opcionesActuales.Count;
            if (total == 0) return;

            int nuevo = opcionSeleccionada;
            for (int i = 0; i < total; i++)
            {
                nuevo = (nuevo + direccion + total) % total;
                if (opcionesActuales[nuevo].disponible)
                {
                    ResaltarOpcion(nuevo);
                    return;
                }
            }
        }

        // Abre el menú con las opciones de esa misión. Devuelve false si no hay
        // nada que ofrecer, para que quien llama siga con el diálogo normal.
        public bool Abrir(MisionEntrega mision)
        {
            if (mision == null || mision.Completada)
            {
                return false;
            }

            List<MisionEntrega.Opcion> opciones = mision.ObtenerOpciones();
            if (opciones.Count == 0)
            {
                return false;
            }

            misionActual = mision;
            RellenarBotones(opciones);

            panel.SetActive(true);
            controlJugador.Bloquear(true);

            // Seleccionar por defecto la primera opción disponible
            int primeraValida = -1;
            for (int i = 0; i < opciones.Count; i++)
            {
                if (opciones[i].disponible)
                {
                    primeraValida = i;
                    break;
                }
            }
            ResaltarOpcion(primeraValida);

            return true;
        }

        public void Cerrar()
        {
            if (!Abierto) return;

            panel.SetActive(false);
            misionActual = null;

            controlJugador.Bloquear(false);
        }

        private void Elegir(MisionEntrega.Opcion opcion)
        {
            MisionEntrega mision = misionActual;
            Cerrar();
            mision?.EntregarOpcion(opcion);
        }

        private void ResaltarOpcion(int indice)
        {
            opcionSeleccionada = indice;

            for (int i = 0; i < barrasSeleccion.Count; i++)
            {
                if (barrasSeleccion[i] != null)
                {
                    bool resaltar = (i == indice) && i < opcionesActuales.Count && opcionesActuales[i].disponible;
                    barrasSeleccion[i].color = resaltar ? Color.white : Color.clear;
                }
            }
        }

        // Crea un botón por opción usando exactamente el diseño de InterfazDialogo.
        // Las opciones no disponibles aparecen pero no se pueden seleccionar.
        private void RellenarBotones(List<MisionEntrega.Opcion> opciones)
        {
            foreach (GameObject viejo in botones)
            {
                Destroy(viejo);
            }
            botones.Clear();
            barrasSeleccion.Clear();
            textosBotones.Clear();
            opcionesActuales.Clear();

            opcionesActuales.AddRange(opciones);

            for (int i = 0; i < opciones.Count; i++)
            {
                int indiceOpcion = i;
                MisionEntrega.Opcion opcion = opciones[i];

                string nombre = !string.IsNullOrEmpty(opcion.item.nombre)
                    ? opcion.item.nombre
                    : opcion.item.name;

                string etiqueta = opcion.disponible
                    ? nombre
                    : $"{nombre}  (no lo tienes)";

                GameObject objeto = new GameObject($"Opcion_{nombre}", typeof(RectTransform));
                objeto.transform.SetParent(panel.transform, false);

                objeto.AddComponent<LayoutElement>().preferredHeight = 46f;

                // EventTrigger para activar la barra seleccionada al pasar el ratón (solo si está disponible)
                if (opcion.disponible)
                {
                    EventTrigger trigger = objeto.AddComponent<EventTrigger>();
                    EventTrigger.Entry entryHover = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                    entryHover.callback.AddListener((data) => { ResaltarOpcion(indiceOpcion); });
                    trigger.triggers.Add(entryHover);
                }

                // Imagen base para raycasts
                Image fondoTransparente = objeto.AddComponent<Image>();
                fondoTransparente.color = Color.clear;
                fondoTransparente.raycastTarget = opcion.disponible;

                // Overlay de selección
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

                barrasSeleccion.Add(imagenBarra);

                Button boton = objeto.AddComponent<Button>();
                boton.targetGraphic = fondoTransparente;
                boton.transition = Selectable.Transition.None;

                if (opcion.disponible)
                {
                    MisionEntrega.Opcion elegida = opcion;
                    boton.onClick.AddListener(() => Elegir(elegida));
                }
                else
                {
                    boton.interactable = false;
                }

                // Texto de la opción
                GameObject objetoTexto = new GameObject("Texto", typeof(RectTransform));
                objetoTexto.transform.SetParent(objeto.transform, false);

                RectTransform rectTexto = (RectTransform)objetoTexto.transform;
                rectTexto.anchorMin = Vector2.zero;
                rectTexto.anchorMax = Vector2.one;
                rectTexto.offsetMin = new Vector2(25f, 0f);
                rectTexto.offsetMax = new Vector2(-25f, 0f);

                Text textoComponent = objetoTexto.AddComponent<Text>();
                textoComponent.text          = etiqueta;
                textoComponent.font          = ConstructorUI.Fuente;
                textoComponent.fontSize      = tamanoFuenteOpciones;
                textoComponent.alignment     = TextAnchor.MiddleLeft;
                textoComponent.color         = opcion.disponible ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                textoComponent.raycastTarget = false;

                textosBotones.Add(textoComponent);
                botones.Add(objeto);
            }
        }

        private void Construir()
        {
            ConstructorUI.AsegurarEventSystem();

            Canvas canvas = ConstructorUI.CrearCanvas("CanvasEntrega", transform, OrdenCanvas);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            GameObject goPanel = new GameObject("PanelEntrega", typeof(RectTransform));
            goPanel.transform.SetParent(canvas.transform, false);
            panel = goPanel;

            RectTransform rtPanel = panel.GetComponent<RectTransform>();
            rtPanel.anchorMin        = new Vector2(0.5f, 0.5f);
            rtPanel.anchorMax        = new Vector2(0.5f, 0.5f);
            rtPanel.pivot            = new Vector2(0.5f, 0.5f);
            rtPanel.anchoredPosition = offsetPanel;
            rtPanel.sizeDelta        = tamanoBase * escala;

            Image img = panel.AddComponent<Image>();
            if (spritePanelOpciones != null)
            {
                img.sprite = spritePanelOpciones;
                img.color  = Color.white;
                img.type   = Image.Type.Simple;
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
            }

            columnaLayout = panel.AddComponent<VerticalLayoutGroup>();
            columnaLayout.padding.top          = 15;
            columnaLayout.padding.bottom       = 15;
            columnaLayout.padding.left         = 20;
            columnaLayout.padding.right        = 20;
            columnaLayout.spacing              = espacioEntreLineasOpciones;
            columnaLayout.childControlWidth    = true;
            columnaLayout.childControlHeight   = true;
            columnaLayout.childForceExpandWidth  = true;
            columnaLayout.childForceExpandHeight = false;

            panel.SetActive(false);
        }
    }
}
