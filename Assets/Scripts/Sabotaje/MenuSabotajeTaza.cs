using System;
using System.Collections.Generic;
using SoltaLaPala.Dialogue;
using SoltaLaPala.Guardado;
using SoltaLaPala.Interaction;
using SoltaLaPala.Menus;
using SoltaLaPala.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoltaLaPala.Sabotaje
{
    public class MenuSabotajeTaza : MonoBehaviour
    {
        public static MenuSabotajeTaza Instancia { get; private set; }

        [Header("UI y Panel")]
        public GameObject panel;
        public Text textoTitulo;
        public Text textoResultado;

        [Header("Sprites de arte")]
        [Tooltip("Sprite del panel de opciones (1000×580). Se carga de Resources si está vacío.")]
        public Sprite spritePanelOpciones;

        [Tooltip("Barra de selección superpuesta sobre la opción con hover/seleccionada.")]
        public Sprite spriteBarraSeleccionada;

        [Header("Ajustes de Tamaño y Posición")]
        [Tooltip("Tamaño base del panel en píxeles de referencia.")]
        public Vector2 tamanoBase = new Vector2(490f, 284f);

        [Range(0.5f, 2.5f)]
        public float escala = 0.75f;

        [Tooltip("Offset horizontal y vertical del panel en pantalla.")]
        public Vector2 offsetPanel = Vector2.zero;

        [Header("Espaciado y Fuente")]
        [Range(0f, 40f)]
        public float espacioEntreLineasOpciones = 6f;

        [Range(12, 32)]
        public int tamanoFuenteOpciones = 17;

        [Header("Botones de Opciones")]
        public Button botonNormal;
        public Button botonSal;
        public Button botonFrio;
        public Button botonDetergente;

        [Header("Controles")]
        [Tooltip("Tecla que cierra el menú sin sabotear.")]
        public KeyCode teclaSalir = KeyCode.X;

        private readonly List<GameObject> botonesOpciones = new List<GameObject>();
        private readonly List<Image> barrasOpciones = new List<Image>();
        private readonly List<Text> textosOpciones = new List<Text>();
        private int opcionSeleccionada = 0;

        private readonly ControlJugador controlJugador = new ControlJugador();

        private GameObject objetoTazaMundo;
        private RequisitoItem requisitoActual;

        public bool Abierto => panel != null && panel.activeSelf;

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

            if (panel == null)
            {
                ConstruirUIPorDefecto();
            }
            else
            {
                ConfigurarPanelExistente();
            }

            if (panel != null)
                panel.SetActive(false);
        }

        private static Sprite CargarSprite(Sprite asignado, string rutaResources)
        {
            return asignado != null ? asignado : Resources.Load<Sprite>(rutaResources);
        }

        private void ConstruirUIPorDefecto()
        {
            ConstructorUI.AsegurarEventSystem();

            Canvas canvas = ConstructorUI.CrearCanvas("CanvasSabotaje", transform, 45);

            GameObject goPanel = new GameObject("PanelSabotaje", typeof(RectTransform));
            goPanel.transform.SetParent(canvas.transform, false);

            panel = goPanel;

            ConfigurarPanelExistente();
        }

        private void ConfigurarPanelExistente()
        {
            if (panel == null) return;

            RectTransform rtPanel = panel.GetComponent<RectTransform>();
            if (rtPanel != null)
            {
                rtPanel.anchorMin = new Vector2(0.5f, 0.5f);
                rtPanel.anchorMax = new Vector2(0.5f, 0.5f);
                rtPanel.pivot     = new Vector2(0.5f, 0.5f);
                rtPanel.anchoredPosition = offsetPanel;
                rtPanel.sizeDelta        = tamanoBase;
                panel.transform.localScale = Vector3.one * escala;
            }

            Image imgFondo = panel.GetComponent<Image>();
            if (imgFondo == null)
                imgFondo = panel.AddComponent<Image>();

            if (spritePanelOpciones != null)
            {
                imgFondo.sprite = spritePanelOpciones;
                imgFondo.color  = Color.white;
                imgFondo.preserveAspect = true;
                imgFondo.type   = Image.Type.Simple;
            }
            else
            {
                imgFondo.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
            }

            VerticalLayoutGroup columna = panel.GetComponent<VerticalLayoutGroup>();
            if (columna == null)
                columna = panel.AddComponent<VerticalLayoutGroup>();

            columna.padding.top          = 15;
            columna.padding.bottom       = 15;
            columna.padding.left         = 20;
            columna.padding.right        = 20;
            columna.spacing              = espacioEntreLineasOpciones;
            columna.childControlWidth    = true;
            columna.childControlHeight   = true;
            columna.childForceExpandWidth  = true;
            columna.childForceExpandHeight = false;

            for (int i = panel.transform.childCount - 1; i >= 0; i--)
            {
                Transform hijo = panel.transform.GetChild(i);
                if (hijo.gameObject != null)
                    Destroy(hijo.gameObject);
            }

            botonesOpciones.Clear();
            barrasOpciones.Clear();
            textosOpciones.Clear();

            botonNormal     = CrearBotonOpcion("Café Normal", () => ElegirOpcion("Café Normal", 0, 0));
            botonSal        = CrearBotonOpcion("Café con Sal", () => ElegirOpcion("Café con Sal", 1, 5));
            botonFrio       = CrearBotonOpcion("Café Frío y Rancio", () => ElegirOpcion("Café Frío y Rancio", 2, 10));
            botonDetergente = CrearBotonOpcion("Café con Detergente", () => ElegirOpcion("Café con Detergente", 3, 20));
        }

        private Button CrearBotonOpcion(string texto, Action alPulsar)
        {
            int miIndice = botonesOpciones.Count;

            GameObject objeto = new GameObject("Opcion_" + miIndice, typeof(RectTransform));
            objeto.transform.SetParent(panel.transform, false);

            objeto.AddComponent<LayoutElement>().preferredHeight = 46f;

            EventTrigger trigger = objeto.AddComponent<EventTrigger>();
            EventTrigger.Entry entryHover = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entryHover.callback.AddListener((data) => { ResaltarOpcion(miIndice); });
            trigger.triggers.Add(entryHover);

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

            GameObject objetoTexto = new GameObject("Texto", typeof(RectTransform));
            objetoTexto.transform.SetParent(objeto.transform, false);

            RectTransform rectTexto = (RectTransform)objetoTexto.transform;
            rectTexto.anchorMin = Vector2.zero;
            rectTexto.anchorMax = Vector2.one;
            rectTexto.offsetMin = new Vector2(25f, 0f);
            rectTexto.offsetMax = new Vector2(-25f, 0f);

            Text etiqueta = objetoTexto.AddComponent<Text>();
            etiqueta.text          = texto;
            etiqueta.font          = ConstructorUI.Fuente;
            etiqueta.fontSize      = tamanoFuenteOpciones;
            etiqueta.alignment     = TextAnchor.MiddleLeft;
            etiqueta.color         = Color.white;
            etiqueta.raycastTarget = false;

            textosOpciones.Add(etiqueta);
            botonesOpciones.Add(objeto);

            return boton;
        }

        private void ResaltarOpcion(int indice)
        {
            if (indice < 0 || indice >= barrasOpciones.Count) return;
            opcionSeleccionada = indice;

            for (int i = 0; i < barrasOpciones.Count; i++)
            {
                if (barrasOpciones[i] != null)
                {
                    barrasOpciones[i].color = (i == indice) ? Color.white : Color.clear;
                }
            }
        }

        public void Abrir(GameObject tazaEnMundo, RequisitoItem requisito = null)
        {
            objetoTazaMundo = tazaEnMundo;
            requisitoActual = requisito;

            controlJugador.Bloquear(true);

            if (textoResultado != null) textoResultado.text = "";

            if (panel != null)
            {
                panel.SetActive(true);
                ResaltarOpcion(0);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }

        public void Cancelar()
        {
            Cerrar();
        }

        private void Update()
        {
            if (!Abierto) return;

            if (panel != null)
            {
                RectTransform rtPanel = panel.GetComponent<RectTransform>();
                if (rtPanel != null)
                {
                    Vector2 tamanoDeseado = tamanoBase;
                    if (rtPanel.sizeDelta != tamanoDeseado)
                        rtPanel.sizeDelta = tamanoDeseado;

                    if (rtPanel.anchoredPosition != offsetPanel)
                        rtPanel.anchoredPosition = offsetPanel;
                }

                Vector3 escalaDeseada = Vector3.one * escala;
                if (panel.transform.localScale != escalaDeseada)
                    panel.transform.localScale = escalaDeseada;

                VerticalLayoutGroup columna = panel.GetComponent<VerticalLayoutGroup>();
                if (columna != null && !Mathf.Approximately(columna.spacing, espacioEntreLineasOpciones))
                    columna.spacing = espacioEntreLineasOpciones;

                for (int i = 0; i < textosOpciones.Count; i++)
                {
                    if (textosOpciones[i] != null && textosOpciones[i].fontSize != tamanoFuenteOpciones)
                        textosOpciones[i].fontSize = tamanoFuenteOpciones;
                }
            }

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
                    EjecutarOpcion(opcionSeleccionada);
                }
            }

            if (Input.GetKeyDown(teclaSalir) || Input.GetKeyDown(KeyCode.Escape))
            {
                Cancelar();
            }
        }

        private void EjecutarOpcion(int indice)
        {
            switch (indice)
            {
                case 0: ElegirOpcion("Café Normal", 0, 0); break;
                case 1: ElegirOpcion("Café con Sal", 1, 5); break;
                case 2: ElegirOpcion("Café Frío y Rancio", 2, 10); break;
                case 3: ElegirOpcion("Café con Detergente", 3, 20); break;
            }
        }

        public void Cerrar()
        {
            if (!Abierto) return;

            panel.SetActive(false);
            requisitoActual = null;

            controlJugador.Bloquear(false);

            if (GestorMenus.Instancia != null && GestorMenus.Instancia.HayMenuAbierto)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (GestorMenus.Instancia != null)
            {
                GestorMenus.Instancia.RefrescarTiempo();
            }
            else
            {
                bool hayDialogo = GestorDialogos.Instancia != null
                    && GestorDialogos.Instancia.DialogoActivo;
                Time.timeScale = hayDialogo ? 0f : 1f;
            }
        }

        private void ElegirOpcion(string nombreCafe, int impacto, int sospecha)
        {
            Debug.Log($"[Sabotaje Café] Resultado: '{nombreCafe}' | Impacto: +{impacto} | Sospecha: +{sospecha}");

            if (textoResultado != null)
            {
                textoResultado.text = $"Has preparado: {nombreCafe}\nImpacto: +{impacto} | Sospecha: +{sospecha}";
            }

            if (objetoTazaMundo != null)
            {
                objetoTazaMundo.SetActive(false);

                if (RegistroObjetosConsumidos.Instancia != null)
                {
                    RegistroObjetosConsumidos.Instancia.Marcar(objetoTazaMundo);
                }
            }

            requisitoActual?.ConsumirSiHaceFalta();

            Cerrar();

            if (EstadoPartida.Instancia != null)
            {
                EstadoPartida.Instancia.RegistrarSabotaje(
                    impacto, sospecha, $"Taza de cafe: {nombreCafe}");
            }

            if (GestorGuardado.Instancia != null)
            {
                GestorGuardado.Instancia.GuardarAhora();
            }
        }
    }
}
