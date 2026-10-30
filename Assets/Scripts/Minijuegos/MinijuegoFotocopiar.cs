using System;
using System.Collections;
using SoltaLaPala.Inventory;
using SoltaLaPala.Menus;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Minijuegos
{
    /// <summary>
    /// Minijuego: Fotocopiar imagen (UI Cuadrada en la Computadora).
    /// 
    /// Flujo de interacción:
    /// 1. Interactuar con Computadora -> Si no hay papel en la impresora, dice "Pon el documento en la impresora".
    /// 2. Ir a la Impresora física -> Poner el documento (Informes o Foto del Chiqui).
    /// 3. Volver a la Computadora -> Darle al botón "IMPRIMIR".
    /// 4. Impresora física -> Sale la Carpeta.
    /// </summary>
    public class MinijuegoFotocopiar : MinijuegoBase
    {
        [Header("Impresora Física Asociada")]
        [Tooltip("Referencia a la impresora física en la sala. Si se deja vacía, se buscará automáticamente en la escena.")]
        public ImpresoraInteractuable impresora;

        [Header("IDs de Ítems e Inventario")]
        public string idInforme = "Informe";
        public string idInformeAlternativo = "Documento";
        public string idFotoChiqui = "FotoChiqui";
        public string idFotoChiquiAlternativo = "fotoChiqui";
        public string idCarpetaResultado = "Carpeta";

        [Header("Sprites y Gráficos")]
        public Sprite spriteFondoComputadora;
        public Sprite spriteIconoAlertaVacio;
        public Sprite spriteIconoInforme;
        public Sprite spriteIconoChiqui;

        [Header("Referencias de UI (Cuadrada)")]
        public Image imagenFondo;
        public Image imagenIconoEstado;
        public Text textoTituloVentana;
        public Text textoMensajePrincipal;
        public Text textoSubmensajeInstruccion;
        public Slider barraProgreso;
        public Button botonImprimir;
        public Button botonSalir;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip clipImpresora;

        [Header("UI - Ventana de Estado Final")]
        public GameObject ventanaPopup;
        public Text textoTituloPopup;
        public Text textoMensajePopup;
        public Button botonAceptarPopup;
        public Image fondoBarraTituloPopup;

        [Header("Ajustes")]
        [Tooltip("Permite probar la impresión aunque no se haya puesto físicamente el documento en la impresora")]
        public bool modoPrueba = false;
        [Tooltip("Tiempo en segundos que dura el proceso de fotocopiado")]
        public float duracionFotocopiado = 2.6f;

        // Estado interno
        private bool procesoEnMarcha = false;
        private bool minijuegoTerminado = false;
        private bool resultadoExito = false;
        private string mensajeVentanaFinal = "";
        private Coroutine corrutinaFotocopiado;

        protected override void AlIniciar()
        {
            if (panelRaiz == null)
            {
                panelRaiz = gameObject;
            }

            BuscarImpresoraSiEsNecesario();
            ConstruirUIAutomaticaSiEsNecesario();
            CrearVentanaPopupSiNoExiste();
            ConfigurarAudio();

            if (botonSalir != null)
            {
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
            }

            if (botonImprimir != null)
            {
                botonImprimir.onClick.RemoveAllListeners();
                botonImprimir.onClick.AddListener(OnBotonImprimirPresionado);
            }

            if (ventanaPopup != null)
            {
                ventanaPopup.SetActive(false);
            }

            if (barraProgreso != null)
            {
                barraProgreso.value = 0f;
            }

            ActualizarPantallaSegunEstadoImpresora();
        }

        private void BuscarImpresoraSiEsNecesario()
        {
            if (impresora == null)
            {
                impresora = FindFirstObjectByType<ImpresoraInteractuable>(FindObjectsInactive.Include);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !procesoEnMarcha)
            {
                Salir();
            }
        }

        private void OnEnable()
        {
            if (EstadoPartida.Instancia != null)
            {
                EstadoPartida.Instancia.AlEmpezarNivel += Reiniciar;
            }
        }

        private void OnDisable()
        {
            if (EstadoPartida.Instancia != null)
            {
                EstadoPartida.Instancia.AlEmpezarNivel -= Reiniciar;
            }
        }

        public override void Reiniciar()
        {
            minijuegoTerminado = false;
            procesoEnMarcha = false;
            resultadoExito = false;
            mensajeVentanaFinal = "";

            if (corrutinaFotocopiado != null)
            {
                StopCoroutine(corrutinaFotocopiado);
                corrutinaFotocopiado = null;
            }

            if (barraProgreso != null)
            {
                barraProgreso.value = 0f;
            }

            if (ventanaPopup != null)
            {
                ventanaPopup.SetActive(false);
            }

            if (panelRaiz != null)
            {
                panelRaiz.SetActive(false);
            }
        }

        protected override void AlSalir()
        {
            if (corrutinaFotocopiado != null)
            {
                StopCoroutine(corrutinaFotocopiado);
                corrutinaFotocopiado = null;
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            procesoEnMarcha = false;
        }

        public void ActualizarPantallaSegunEstadoImpresora()
        {
            BuscarImpresoraSiEsNecesario();

            bool tieneDocumento = modoPrueba || (impresora != null && impresora.documentoCargado != ImpresoraInteractuable.EstadoDocumentoImpresora.Vacio);

            if (!tieneDocumento)
            {
                // PASO 1: NO HAY DOCUMENTO EN LA IMPRESORA
                if (imagenIconoEstado != null)
                {
                    imagenIconoEstado.gameObject.SetActive(true);
                    if (spriteIconoAlertaVacio != null)
                    {
                        imagenIconoEstado.sprite = spriteIconoAlertaVacio;
                    }
                    imagenIconoEstado.color = new Color(1f, 0.75f, 0.2f, 1f);
                }

                if (textoMensajePrincipal != null)
                {
                    textoMensajePrincipal.text = "<color=#FFCC00><b>⚠ BANDEJA DE ESCÁNER VACÍA</b></color>";
                }

                if (textoSubmensajeInstruccion != null)
                {
                    textoSubmensajeInstruccion.text = "No se detectó ningún documento en la impresora.\n\n" +
                                                      "<size=17><color=#55CCFF><b>➔ Pon el documento en la impresora</b></color></size>\n\n" +
                                                      "<i>(Ve físicamente a la impresora y presiona [E] con los Informes o la Foto de Chiqui).</i>";
                }

                if (botonImprimir != null)
                {
                    botonImprimir.interactable = false;
                    Text t = botonImprimir.GetComponentInChildren<Text>();
                    if (t != null) t.text = "IMPRIMIR (Bandeja Vacía)";
                }
            }
            else
            {
                // PASO 3: YA HAY DOCUMENTO EN LA IMPRESORA
                var doc = impresora != null ? impresora.documentoCargado : ImpresoraInteractuable.EstadoDocumentoImpresora.Informe;

                if (imagenIconoEstado != null)
                {
                    imagenIconoEstado.gameObject.SetActive(true);
                    imagenIconoEstado.sprite = (doc == ImpresoraInteractuable.EstadoDocumentoImpresora.FotoChiqui)
                        ? spriteIconoChiqui
                        : spriteIconoInforme;
                    imagenIconoEstado.color = Color.white;
                }

                if (textoMensajePrincipal != null)
                {
                    if (doc == ImpresoraInteractuable.EstadoDocumentoImpresora.FotoChiqui)
                    {
                        textoMensajePrincipal.text = "<color=#FFAA55><b>✔ DOCUMENTO DETECTADO: Foto de Chiqui Tapia</b></color>";
                    }
                    else
                    {
                        textoMensajePrincipal.text = "<color=#55FF88><b>✔ DOCUMENTO DETECTADO: Informes del Jefe</b></color>";
                    }
                }

                if (textoSubmensajeInstruccion != null)
                {
                    textoSubmensajeInstruccion.text = "Documento verificado en el escáner de la impresora.\n\n" +
                                                      "Presiona el botón de abajo para iniciar la impresión.\n" +
                                                      "<b>La Carpeta duplicada saldrá de la impresora.</b>";
                }

                if (botonImprimir != null)
                {
                    botonImprimir.interactable = true;
                    Text t = botonImprimir.GetComponentInChildren<Text>();
                    if (t != null) t.text = "🖨 IMPRIMIR DOCUMENTO";
                }
            }
        }

        #region Proceso de Impresión

        public void OnBotonImprimirPresionado()
        {
            if (procesoEnMarcha) return;

            corrutinaFotocopiado = StartCoroutine(RutinaImpresion());
        }

        private IEnumerator RutinaImpresion()
        {
            procesoEnMarcha = true;
            if (botonImprimir != null) botonImprimir.interactable = false;
            if (botonSalir != null) botonSalir.interactable = false;

            // Reproducir sonido de impresora
            if (audioSource != null && clipImpresora != null)
            {
                audioSource.clip = clipImpresora;
                audioSource.Play();
            }

            float tiempoTotal = duracionFotocopiado;
            if (clipImpresora != null && clipImpresora.length > 0.5f)
            {
                tiempoTotal = Mathf.Min(clipImpresora.length, duracionFotocopiado);
            }

            float tiempoPasado = 0f;

            while (tiempoPasado < tiempoTotal)
            {
                tiempoPasado += Time.deltaTime;
                float t = Mathf.Clamp01(tiempoPasado / tiempoTotal);

                if (barraProgreso != null)
                {
                    barraProgreso.value = t;
                }

                if (textoSubmensajeInstruccion != null)
                {
                    if (t < 0.4f)
                        textoSubmensajeInstruccion.text = "⚙ <i>Escaneando documento original...</i>";
                    else if (t < 0.8f)
                        textoSubmensajeInstruccion.text = "📄 <i>Imprimiendo páginas en la impresora...</i>";
                    else
                        textoSubmensajeInstruccion.text = "📁 <i>Expulsando Carpeta en la bandeja de salida...</i>";
                }

                yield return null;
            }

            if (barraProgreso != null) barraProgreso.value = 1f;

            FinalizarImpresion();
        }

        private void FinalizarImpresion()
        {
            procesoEnMarcha = false;
            minijuegoTerminado = true;

            BuscarImpresoraSiEsNecesario();

            bool fueSabotaje = impresora != null && impresora.documentoCargado == ImpresoraInteractuable.EstadoDocumentoImpresora.FotoChiqui;

            if (fueSabotaje)
            {
                // SABOTAJE
                resultadoExito = false;
                mensajeVentanaFinal = "¡Tarea Saboteada!\nHas fotocopiado la foto de Chiqui Tapia en lugar de los informes.\n\n" +
                                     "<b>➔ La Carpeta saboteada ha salido de la impresora.</b>";

                if (EstadoPartida.Instancia != null)
                {
                    EstadoPartida.Instancia.RegistrarSabotaje(3, 15);
                }

                Debug.LogWarning("[MinijuegoFotocopiar] ¡Sabotaje cometido! Se fotocopió la foto de Chiqui Tapia.");
            }
            else
            {
                // ÉXITO LEGÍTIMO
                resultadoExito = true;
                mensajeVentanaFinal = "Informes oficiales fotocopiados con éxito.\nEl jefe estará conforme.\n\n" +
                                     "<b>➔ La Carpeta con los informes ha salido de la impresora.</b>";

                if (EstadoPartida.Instancia != null)
                {
                    EstadoPartida.Instancia.ReducirSospecha(15);
                }

                Debug.Log("[MinijuegoFotocopiar] ¡Fotocopiado exitoso! Informes copiados.");
            }

            // PASO 4: DE LA IMPRESORA SALE LA CARPETA FÍSICAMENTE
            if (impresora != null)
            {
                impresora.ExpulsarCarpeta(fueSabotaje);
            }
            else
            {
                // Fallback directo al inventario si no hay impresora asignada
                DatosItem itemCarpeta = Resources.Load<DatosItem>("Items/" + idCarpetaResultado) ?? Resources.Load<DatosItem>(idCarpetaResultado);
                if (itemCarpeta != null && Inventario != null)
                {
                    Inventario.IntentarAgregarItem(itemCarpeta);
                }
            }

            MostrarVentanaFinal(mensajeVentanaFinal, resultadoExito);
        }

        #endregion

        #region Ventana Popup Final y Construcción UI Cuadrada

        public void MostrarVentanaFinal(string mensaje, bool exito)
        {
            CrearVentanaPopupSiNoExiste();

            if (ventanaPopup != null)
            {
                ventanaPopup.SetActive(true);
                ventanaPopup.transform.SetAsLastSibling();
            }

            if (textoMensajePopup != null)
            {
                textoMensajePopup.text = mensaje;
            }

            if (textoTituloPopup != null)
            {
                textoTituloPopup.text = exito ? "Aviso del Sistema" : "Alerta de Seguridad";
            }

            if (fondoBarraTituloPopup != null)
            {
                fondoBarraTituloPopup.color = exito
                    ? new Color(0.12f, 0.45f, 0.24f, 1f)
                    : new Color(0.65f, 0.15f, 0.15f, 1f);
            }

            if (botonAceptarPopup != null)
            {
                botonAceptarPopup.interactable = true;
            }
        }

        private void CrearVentanaPopupSiNoExiste()
        {
            if (ventanaPopup != null) return;

            Transform padre = panelRaiz != null ? panelRaiz.transform : transform;

            // Ventana Cuadrada / Compacta
            ventanaPopup = new GameObject("VentanaResultado", typeof(RectTransform), typeof(Image));
            ventanaPopup.transform.SetParent(padre, false);
            RectTransform rtVentana = ventanaPopup.GetComponent<RectTransform>();
            rtVentana.anchorMin = new Vector2(0.5f, 0.5f);
            rtVentana.anchorMax = new Vector2(0.5f, 0.5f);
            rtVentana.pivot = new Vector2(0.5f, 0.5f);
            rtVentana.anchoredPosition = Vector2.zero;
            rtVentana.sizeDelta = new Vector2(500f, 260f);

            Image imgFondo = ventanaPopup.GetComponent<Image>();
            imgFondo.color = new Color(0.11f, 0.13f, 0.18f, 0.98f);

            // Barra de Título
            GameObject objBarra = new GameObject("BarraTitulo", typeof(RectTransform), typeof(Image));
            objBarra.transform.SetParent(ventanaPopup.transform, false);
            RectTransform rtBarra = objBarra.GetComponent<RectTransform>();
            rtBarra.anchorMin = new Vector2(0f, 1f);
            rtBarra.anchorMax = new Vector2(1f, 1f);
            rtBarra.pivot = new Vector2(0.5f, 1f);
            rtBarra.anchoredPosition = Vector2.zero;
            rtBarra.sizeDelta = new Vector2(0f, 38f);
            fondoBarraTituloPopup = objBarra.GetComponent<Image>();
            fondoBarraTituloPopup.color = new Color(0.18f, 0.22f, 0.32f, 1f);

            GameObject objTxtTit = new GameObject("Titulo", typeof(RectTransform), typeof(Text));
            objTxtTit.transform.SetParent(objBarra.transform, false);
            RectTransform rtTit = objTxtTit.GetComponent<RectTransform>();
            rtTit.anchorMin = Vector2.zero;
            rtTit.anchorMax = Vector2.one;
            rtTit.offsetMin = new Vector2(15f, 0f);
            rtTit.offsetMax = new Vector2(-15f, 0f);
            textoTituloPopup = objTxtTit.GetComponent<Text>();
            textoTituloPopup.font = ConstructorUI.Fuente;
            textoTituloPopup.fontSize = 15;
            textoTituloPopup.fontStyle = FontStyle.Bold;
            textoTituloPopup.alignment = TextAnchor.MiddleLeft;
            textoTituloPopup.color = Color.white;
            textoTituloPopup.text = "Aviso del Sistema";

            // Mensaje Central
            GameObject objTxtMsg = new GameObject("Mensaje", typeof(RectTransform), typeof(Text));
            objTxtMsg.transform.SetParent(ventanaPopup.transform, false);
            RectTransform rtMsg = objTxtMsg.GetComponent<RectTransform>();
            rtMsg.anchorMin = new Vector2(0f, 0.28f);
            rtMsg.anchorMax = new Vector2(1f, 0.88f);
            rtMsg.offsetMin = new Vector2(25f, 0f);
            rtMsg.offsetMax = new Vector2(-25f, 0f);
            textoMensajePopup = objTxtMsg.GetComponent<Text>();
            textoMensajePopup.font = ConstructorUI.Fuente;
            textoMensajePopup.fontSize = 15;
            textoMensajePopup.alignment = TextAnchor.MiddleCenter;
            textoMensajePopup.color = Color.white;

            // Botón Aceptar
            GameObject objBtnAceptar = new GameObject("BotonAceptar", typeof(RectTransform), typeof(Image), typeof(Button));
            objBtnAceptar.transform.SetParent(ventanaPopup.transform, false);
            RectTransform rtBtn = objBtnAceptar.GetComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(0.5f, 0f);
            rtBtn.anchorMax = new Vector2(0.5f, 0f);
            rtBtn.pivot = new Vector2(0.5f, 0f);
            rtBtn.anchoredPosition = new Vector2(0f, 16f);
            rtBtn.sizeDelta = new Vector2(140f, 38f);
            objBtnAceptar.GetComponent<Image>().color = new Color(0.24f, 0.28f, 0.38f, 1f);

            botonAceptarPopup = objBtnAceptar.GetComponent<Button>();
            botonAceptarPopup.colors = ConstructorUI.ColoresBoton();
            botonAceptarPopup.onClick.RemoveAllListeners();
            botonAceptarPopup.onClick.AddListener(Salir);

            GameObject objTxtBtn = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            objTxtBtn.transform.SetParent(objBtnAceptar.transform, false);
            ConstructorUI.Estirar(objTxtBtn.GetComponent<RectTransform>());
            Text tBtn = objTxtBtn.GetComponent<Text>();
            tBtn.font = ConstructorUI.Fuente;
            tBtn.fontSize = 15;
            tBtn.fontStyle = FontStyle.Bold;
            tBtn.alignment = TextAnchor.MiddleCenter;
            tBtn.color = Color.white;
            tBtn.text = "Aceptar";

            ventanaPopup.SetActive(false);
        }

        private void ConfigurarAudio()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (clipImpresora == null)
            {
                clipImpresora = Resources.Load<AudioClip>("Audio/Objetos/Impresora2");
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }
        }

        public void ConstruirUIAutomaticaSiEsNecesario()
        {
            // Cargar Sprites de Resources si hacen falta
            if (spriteFondoComputadora == null)
            {
                spriteFondoComputadora = Resources.Load<Sprite>("Sprites/Minijuegos/fondo computadora");
            }
            if (spriteIconoInforme == null)
            {
                spriteIconoInforme = Resources.Load<Sprite>("Sprites/Minijuegos/documento_importante_24px")
                                     ?? Resources.Load<Sprite>("Sprites/Minijuegos/papelescrito-sinfondo");
            }
            if (spriteIconoChiqui == null)
            {
                spriteIconoChiqui = Resources.Load<Sprite>("Sprites/Minijuegos/chiqui tapia formato hoja");
            }

            // PANEL RAÍZ: CUADRADO (700 x 700)
            RectTransform rtPanel = panelRaiz != null ? panelRaiz.GetComponent<RectTransform>() : GetComponent<RectTransform>();
            if (rtPanel != null)
            {
                rtPanel.anchorMin = new Vector2(0.5f, 0.5f);
                rtPanel.anchorMax = new Vector2(0.5f, 0.5f);
                rtPanel.pivot = new Vector2(0.5f, 0.5f);
                rtPanel.sizeDelta = new Vector2(700f, 700f);
            }

            if (imagenFondo != null && botonImprimir != null)
            {
                return;
            }

            // 1. Imagen Fondo Cuadrado
            if (imagenFondo == null)
            {
                Transform trFondo = transform.Find("Fondo");
                if (trFondo != null)
                {
                    imagenFondo = trFondo.GetComponent<Image>();
                }
                else
                {
                    GameObject objFondo = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
                    objFondo.transform.SetParent(transform, false);
                    ConstructorUI.Estirar(objFondo.GetComponent<RectTransform>());
                    imagenFondo = objFondo.GetComponent<Image>();
                }
            }
            if (imagenFondo != null)
            {
                imagenFondo.sprite = spriteFondoComputadora;
                imagenFondo.color = Color.white;
            }

            // 2. Barra Superior y Botón Salir
            if (botonSalir == null)
            {
                GameObject objSalir = new GameObject("BotonSalir", typeof(RectTransform), typeof(Image), typeof(Button));
                objSalir.transform.SetParent(transform, false);
                RectTransform rtSalir = objSalir.GetComponent<RectTransform>();
                rtSalir.anchorMin = new Vector2(1f, 1f);
                rtSalir.anchorMax = new Vector2(1f, 1f);
                rtSalir.pivot = new Vector2(1f, 1f);
                rtSalir.anchoredPosition = new Vector2(-15f, -15f);
                rtSalir.sizeDelta = new Vector2(90f, 34f);

                objSalir.GetComponent<Image>().color = new Color(0.78f, 0.20f, 0.20f, 0.95f);
                botonSalir = objSalir.GetComponent<Button>();
                botonSalir.colors = ConstructorUI.ColoresBoton();

                GameObject objTxtSalir = new GameObject("Txt", typeof(RectTransform), typeof(Text));
                objTxtSalir.transform.SetParent(objSalir.transform, false);
                ConstructorUI.Estirar(objTxtSalir.GetComponent<RectTransform>());
                Text txtSalir = objTxtSalir.GetComponent<Text>();
                txtSalir.text = "✕ Salir";
                txtSalir.fontSize = 14;
                txtSalir.alignment = TextAnchor.MiddleCenter;
                txtSalir.color = Color.white;
                txtSalir.font = ConstructorUI.Fuente;
            }

            // Título Superior
            if (textoTituloVentana == null)
            {
                GameObject objTit = new GameObject("Titulo", typeof(RectTransform), typeof(Text));
                objTit.transform.SetParent(transform, false);
                RectTransform rtTit = objTit.GetComponent<RectTransform>();
                rtTit.anchorMin = new Vector2(0.5f, 1f);
                rtTit.anchorMax = new Vector2(0.5f, 1f);
                rtTit.pivot = new Vector2(0.5f, 1f);
                rtTit.anchoredPosition = new Vector2(0f, -20f);
                rtTit.sizeDelta = new Vector2(500f, 32f);

                Text txt = objTit.GetComponent<Text>();
                txt.text = "<b>TERMINAL DE FOTOCOPIADO</b>";
                txt.font = ConstructorUI.Fuente;
                txt.fontSize = 20;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                textoTituloVentana = txt;
            }

            // Marco Central Cuadrado para Estado
            GameObject panelCentro = new GameObject("PanelCentro", typeof(RectTransform), typeof(Image));
            panelCentro.transform.SetParent(transform, false);
            RectTransform rtCentro = panelCentro.GetComponent<RectTransform>();
            rtCentro.anchorMin = new Vector2(0.5f, 0.5f);
            rtCentro.anchorMax = new Vector2(0.5f, 0.5f);
            rtCentro.pivot = new Vector2(0.5f, 0.5f);
            rtCentro.anchoredPosition = new Vector2(0f, 25f);
            rtCentro.sizeDelta = new Vector2(580f, 440f);
            panelCentro.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.19f, 0.90f);

            // Icono de Estado / Documento
            GameObject objIcono = new GameObject("IconoEstado", typeof(RectTransform), typeof(Image));
            objIcono.transform.SetParent(panelCentro.transform, false);
            RectTransform rtIcono = objIcono.GetComponent<RectTransform>();
            rtIcono.anchorMin = new Vector2(0.5f, 1f);
            rtIcono.anchorMax = new Vector2(0.5f, 1f);
            rtIcono.pivot = new Vector2(0.5f, 1f);
            rtIcono.anchoredPosition = new Vector2(0f, -25f);
            rtIcono.sizeDelta = new Vector2(110f, 110f);
            imagenIconoEstado = objIcono.GetComponent<Image>();
            imagenIconoEstado.preserveAspect = true;

            // Mensaje Principal ("⚠ BANDEJA DE ESCÁNER VACÍA" o "✔ DOCUMENTO DETECTADO")
            GameObject objMsgPrinc = new GameObject("MensajePrincipal", typeof(RectTransform), typeof(Text));
            objMsgPrinc.transform.SetParent(panelCentro.transform, false);
            RectTransform rtMsgPrinc = objMsgPrinc.GetComponent<RectTransform>();
            rtMsgPrinc.anchorMin = new Vector2(0.5f, 1f);
            rtMsgPrinc.anchorMax = new Vector2(0.5f, 1f);
            rtMsgPrinc.pivot = new Vector2(0.5f, 1f);
            rtMsgPrinc.anchoredPosition = new Vector2(0f, -150f);
            rtMsgPrinc.sizeDelta = new Vector2(520f, 35f);
            textoMensajePrincipal = objMsgPrinc.GetComponent<Text>();
            textoMensajePrincipal.font = ConstructorUI.Fuente;
            textoMensajePrincipal.fontSize = 17;
            textoMensajePrincipal.alignment = TextAnchor.MiddleCenter;
            textoMensajePrincipal.color = Color.white;

            // Submensaje Instrucción ("Pon el documento en la impresora...")
            GameObject objSub = new GameObject("SubmensajeInstruccion", typeof(RectTransform), typeof(Text));
            objSub.transform.SetParent(panelCentro.transform, false);
            RectTransform rtSub = objSub.GetComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 1f);
            rtSub.anchorMax = new Vector2(0.5f, 1f);
            rtSub.pivot = new Vector2(0.5f, 1f);
            rtSub.anchoredPosition = new Vector2(0f, -195f);
            rtSub.sizeDelta = new Vector2(520f, 140f);
            textoSubmensajeInstruccion = objSub.GetComponent<Text>();
            textoSubmensajeInstruccion.font = ConstructorUI.Fuente;
            textoSubmensajeInstruccion.fontSize = 14;
            textoSubmensajeInstruccion.alignment = TextAnchor.MiddleCenter;
            textoSubmensajeInstruccion.color = new Color(0.85f, 0.90f, 1f, 0.95f);

            // Barra de Progreso
            GameObject objBarra = new GameObject("BarraProgreso", typeof(RectTransform), typeof(Slider));
            objBarra.transform.SetParent(panelCentro.transform, false);
            RectTransform rtProg = objBarra.GetComponent<RectTransform>();
            rtProg.anchorMin = new Vector2(0.5f, 0f);
            rtProg.anchorMax = new Vector2(0.5f, 0f);
            rtProg.pivot = new Vector2(0.5f, 0f);
            rtProg.anchoredPosition = new Vector2(0f, 25f);
            rtProg.sizeDelta = new Vector2(500f, 16f);

            barraProgreso = objBarra.GetComponent<Slider>();
            barraProgreso.minValue = 0f;
            barraProgreso.maxValue = 1f;
            barraProgreso.value = 0f;

            GameObject bgBarra = new GameObject("FondoBarra", typeof(RectTransform), typeof(Image));
            bgBarra.transform.SetParent(objBarra.transform, false);
            ConstructorUI.Estirar(bgBarra.GetComponent<RectTransform>());
            bgBarra.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 1f);

            GameObject areaRelleno = new GameObject("AreaRelleno", typeof(RectTransform));
            areaRelleno.transform.SetParent(objBarra.transform, false);
            ConstructorUI.Estirar(areaRelleno.GetComponent<RectTransform>());
            GameObject fillBarra = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillBarra.transform.SetParent(areaRelleno.transform, false);
            RectTransform rtFill = fillBarra.GetComponent<RectTransform>();
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = new Vector2(0f, 1f);
            rtFill.sizeDelta = Vector2.zero;
            fillBarra.GetComponent<Image>().color = new Color(0.25f, 0.75f, 0.45f, 1f);
            barraProgreso.fillRect = rtFill;

            // Botón Inferior: IMPRIMIR DOCUMENTO
            GameObject objBtnImp = new GameObject("BotonImprimir", typeof(RectTransform), typeof(Image), typeof(Button));
            objBtnImp.transform.SetParent(transform, false);
            RectTransform rtBtnImp = objBtnImp.GetComponent<RectTransform>();
            rtBtnImp.anchorMin = new Vector2(0.5f, 0f);
            rtBtnImp.anchorMax = new Vector2(0.5f, 0f);
            rtBtnImp.pivot = new Vector2(0.5f, 0f);
            rtBtnImp.anchoredPosition = new Vector2(0f, 25f);
            rtBtnImp.sizeDelta = new Vector2(340f, 52f);
            objBtnImp.GetComponent<Image>().color = new Color(0.18f, 0.52f, 0.32f, 1f);

            botonImprimir = objBtnImp.GetComponent<Button>();
            ColorBlock cbImp = ConstructorUI.ColoresBoton();
            cbImp.normalColor = Color.white;
            cbImp.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cbImp.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            cbImp.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            botonImprimir.colors = cbImp;

            GameObject objTxtBtn = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            objTxtBtn.transform.SetParent(objBtnImp.transform, false);
            ConstructorUI.Estirar(objTxtBtn.GetComponent<RectTransform>());
            Text tBtn = objTxtBtn.GetComponent<Text>();
            tBtn.font = ConstructorUI.Fuente;
            tBtn.fontSize = 17;
            tBtn.fontStyle = FontStyle.Bold;
            tBtn.alignment = TextAnchor.MiddleCenter;
            tBtn.color = Color.white;
            tBtn.text = "🖨 IMPRIMIR DOCUMENTO";
        }

        #endregion
    }
}
