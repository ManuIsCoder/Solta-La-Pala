using System;
using System.Collections;
using SoltaLaPala.Inventory;
using SoltaLaPala.Menus;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Minijuegos
{
    /// <summary>
    /// Minijuego: Fotocopiar imagen.
    /// 
    /// De qué trata:
    ///   El jefe pide fotocopiar unos informes oficiales de la oficina.
    /// 
    /// Objetivo legítimo (cumplir la tarea):
    ///   Poner los Informes en la impresora y presionar Fotocopiar.
    ///   Resultado: Éxito, reduce la sospecha (-15) y entrega la Carpeta oficial.
    /// 
    /// Sabotaje (arruinar la tarea):
    ///   Poner la foto de Chiqui Tapia en la impresora y presionar Fotocopiar.
    ///   Resultado: Sabotaje, aumenta la sospecha (+15), registra sabotaje y entrega la Carpeta saboteada.
    /// </summary>
    public class MinijuegoFotocopiar : MinijuegoBase
    {
        public enum TipoDocumento
        {
            Ninguno,
            Informe,
            FotoChiqui
        }

        [Header("Estado de Entrada")]
        [Tooltip("Documento actualmente colocado sobre la bandeja del escáner / impresora")]
        public TipoDocumento documentoEnBandeja = TipoDocumento.Ninguno;

        [Header("IDs de Ítems e Inventario")]
        [Tooltip("ID principal del ítem de informe en el inventario")]
        public string idInforme = "Informe";
        [Tooltip("ID alternativo de informe por compatibilidad")]
        public string idInformeAlternativo = "Documento";
        [Tooltip("ID del ítem de la foto de Chiqui Tapia para sabotear")]
        public string idFotoChiqui = "FotoChiqui";
        [Tooltip("ID alternativo de foto de Chiqui por compatibilidad")]
        public string idFotoChiquiAlternativo = "fotoChiqui";
        [Tooltip("ID del ítem de salida que se entrega al jugador")]
        public string idCarpetaResultado = "Carpeta";

        [Header("Sprites y Gráficos")]
        public Sprite spriteFondoComputadora;
        public Sprite spriteImpresora;
        public Sprite spriteIconoInforme;
        public Sprite spriteIconoChiqui;

        [Header("Referencias de UI")]
        public Image imagenFondo;
        public Image imagenImpresoraVisual;
        public Image imagenDocumentoEnBandeja;
        public Text textoDocumentoEnBandeja;
        public Text textoInstrucciones;
        public Text textoEstadoProceso;
        public Slider barraProgresoImpresion;
        public Button botonFotocopiar;
        public Button botonSalir;

        [Header("Botones / Selección de Archivo en la Computadora")]
        public Button botonSeleccionarInforme;
        public Button botonSeleccionarChiqui;
        public Image marcoSeleccionInforme;
        public Image marcoSeleccionChiqui;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip clipImpresora;

        [Header("UI - Ventana de Estado Final")]
        public GameObject ventanaPopup;
        public Text textoTituloVentana;
        public Text textoMensajeVentana;
        public Button botonAceptarVentana;
        public Image fondoBarraTituloVentana;

        [Header("Ajustes")]
        [Tooltip("Permite probar el minijuego sin requerir tener los ítems en el inventario")]
        public bool modoPrueba = false;
        [Tooltip("Tiempo en segundos que dura el proceso de fotocopiado")]
        public float duracionFotocopiado = 2.8f;

        // Estado interno
        private bool yaInicializado = false;
        private bool minijuegoTerminado = false;
        private bool procesoEnMarcha = false;
        private bool resultadoExito = false;
        private string mensajeVentanaFinal = "";
        private Coroutine corrutinaFotocopiado;

        protected override void AlIniciar()
        {
            if (panelRaiz == null)
            {
                panelRaiz = gameObject;
            }

            ConstruirUIAutomaticaSiEsNecesario();
            CrearVentanaPopupSiNoExiste();
            ConfigurarAudio();

            if (botonSalir != null)
            {
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
            }

            if (botonFotocopiar != null)
            {
                botonFotocopiar.onClick.RemoveAllListeners();
                botonFotocopiar.onClick.AddListener(OnBotonFotocopiarPresionado);
            }

            if (botonSeleccionarInforme != null)
            {
                botonSeleccionarInforme.onClick.RemoveAllListeners();
                botonSeleccionarInforme.onClick.AddListener(() => ColocarDocumento(TipoDocumento.Informe));
            }

            if (botonSeleccionarChiqui != null)
            {
                botonSeleccionarChiqui.onClick.RemoveAllListeners();
                botonSeleccionarChiqui.onClick.AddListener(() => ColocarDocumento(TipoDocumento.FotoChiqui));
            }

            // Si el minijuego ya fue completado o saboteado previamente en esta partida
            if (minijuegoTerminado)
            {
                BloquearControles();
                MostrarVentanaFinal(mensajeVentanaFinal, resultadoExito);
                return;
            }

            if (ventanaPopup != null)
            {
                ventanaPopup.SetActive(false);
            }

            // Detectar ítems en posesión del jugador
            bool tieneInforme = JugadorTieneInforme();
            bool tieneChiqui = JugadorTieneFotoChiqui();

            // Si es la primera vez que se interactúa, preseleccionar según lo que lleve en mano
            if (!yaInicializado)
            {
                yaInicializado = true;
                if (tieneInforme && !tieneChiqui)
                {
                    ColocarDocumento(TipoDocumento.Informe);
                }
                else if (!tieneInforme && tieneChiqui)
                {
                    ColocarDocumento(TipoDocumento.FotoChiqui);
                }
                else if (tieneInforme && tieneChiqui)
                {
                    // Si tiene ambos, arranca en informe por defecto pero puede elegir
                    ColocarDocumento(TipoDocumento.Informe);
                }
                else if (modoPrueba)
                {
                    ColocarDocumento(TipoDocumento.Informe);
                }
                else
                {
                    ColocarDocumento(TipoDocumento.Ninguno);
                }
            }
            else
            {
                ActualizarVisualBandeja();
            }

            ActualizarEstadoBotonesSeleccion(tieneInforme, tieneChiqui);
            ActualizarEstadoBotonFotocopiar();
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
            yaInicializado = false;
            procesoEnMarcha = false;
            resultadoExito = false;
            mensajeVentanaFinal = "";
            documentoEnBandeja = TipoDocumento.Ninguno;

            if (corrutinaFotocopiado != null)
            {
                StopCoroutine(corrutinaFotocopiado);
                corrutinaFotocopiado = null;
            }

            if (barraProgresoImpresion != null)
            {
                barraProgresoImpresion.value = 0f;
            }

            if (ventanaPopup != null)
            {
                ventanaPopup.SetActive(false);
            }

            if (panelRaiz != null)
            {
                panelRaiz.SetActive(false);
            }

            DesbloquearControles();
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

        #region Comprobación de Ítems del Jugador

        public bool JugadorTieneInforme()
        {
            if (modoPrueba) return true;
            if (Inventario == null) return false;

            return Inventario.TieneItem(idInforme)
                || (!string.IsNullOrEmpty(idInformeAlternativo) && Inventario.TieneItem(idInformeAlternativo))
                || Inventario.TieneItem("Papeles");
        }

        public bool JugadorTieneFotoChiqui()
        {
            if (modoPrueba) return true;
            if (Inventario == null) return false;

            return Inventario.TieneItem(idFotoChiqui)
                || (!string.IsNullOrEmpty(idFotoChiquiAlternativo) && Inventario.TieneItem(idFotoChiquiAlternativo))
                || Inventario.TieneItem("Dibujo");
        }

        public bool JugadorTieneCualquierRequisito()
        {
            return modoPrueba || JugadorTieneInforme() || JugadorTieneFotoChiqui();
        }

        #endregion

        #region Lógica de Selección y Colocación

        public void ColocarDocumento(TipoDocumento tipo)
        {
            if (procesoEnMarcha || minijuegoTerminado) return;

            documentoEnBandeja = tipo;
            ActualizarVisualBandeja();
            ActualizarEstadoBotonFotocopiar();
        }

        private void ActualizarVisualBandeja()
        {
            if (imagenDocumentoEnBandeja != null)
            {
                switch (documentoEnBandeja)
                {
                    case TipoDocumento.Informe:
                        imagenDocumentoEnBandeja.gameObject.SetActive(true);
                        imagenDocumentoEnBandeja.sprite = spriteIconoInforme;
                        imagenDocumentoEnBandeja.color = Color.white;
                        break;
                    case TipoDocumento.FotoChiqui:
                        imagenDocumentoEnBandeja.gameObject.SetActive(true);
                        imagenDocumentoEnBandeja.sprite = spriteIconoChiqui;
                        imagenDocumentoEnBandeja.color = Color.white;
                        break;
                    default:
                        imagenDocumentoEnBandeja.gameObject.SetActive(false);
                        break;
                }
            }

            if (textoDocumentoEnBandeja != null)
            {
                switch (documentoEnBandeja)
                {
                    case TipoDocumento.Informe:
                        textoDocumentoEnBandeja.text = "<color=#88DDFF><b>En bandeja:</b> Informes oficiales de la empresa</color>";
                        break;
                    case TipoDocumento.FotoChiqui:
                        textoDocumentoEnBandeja.text = "<color=#FFAA55><b>En bandeja:</b> Foto del Chiqui Tapia</color>";
                        break;
                    default:
                        textoDocumentoEnBandeja.text = "<color=#AAAAAA><i>Bandeja de escáner vacía. Selecciona un archivo.</i></color>";
                        break;
                }
            }

            // Marcos de selección en la UI de la computadora
            if (marcoSeleccionInforme != null)
            {
                marcoSeleccionInforme.color = (documentoEnBandeja == TipoDocumento.Informe)
                    ? new Color(0.2f, 0.8f, 0.4f, 0.9f)
                    : new Color(0.3f, 0.35f, 0.45f, 0.4f);
            }

            if (marcoSeleccionChiqui != null)
            {
                marcoSeleccionChiqui.color = (documentoEnBandeja == TipoDocumento.FotoChiqui)
                    ? new Color(0.9f, 0.5f, 0.2f, 0.9f)
                    : new Color(0.3f, 0.35f, 0.45f, 0.4f);
            }

            if (textoEstadoProceso != null && !procesoEnMarcha)
            {
                if (documentoEnBandeja == TipoDocumento.Ninguno)
                {
                    textoEstadoProceso.text = "Selecciona un documento en la computadora para colocarlo en la impresora.";
                }
                else
                {
                    textoEstadoProceso.text = "Documento cargado en el escáner. Presiona <b>FOTOCOPIAR</b>.";
                }
            }
        }

        private void ActualizarEstadoBotonesSeleccion(bool tieneInforme, bool tieneChiqui)
        {
            if (botonSeleccionarInforme != null)
            {
                botonSeleccionarInforme.interactable = modoPrueba || tieneInforme;
            }

            if (botonSeleccionarChiqui != null)
            {
                botonSeleccionarChiqui.interactable = modoPrueba || tieneChiqui;
            }
        }

        private void ActualizarEstadoBotonFotocopiar()
        {
            if (botonFotocopiar != null)
            {
                bool puedeFotocopiar = !procesoEnMarcha && !minijuegoTerminado && documentoEnBandeja != TipoDocumento.Ninguno;
                botonFotocopiar.interactable = puedeFotocopiar;
            }
        }

        #endregion

        #region Proceso de Fotocopiado

        public void OnBotonFotocopiarPresionado()
        {
            if (procesoEnMarcha || minijuegoTerminado || documentoEnBandeja == TipoDocumento.Ninguno)
                return;

            corrutinaFotocopiado = StartCoroutine(RutinaFotocopiar());
        }

        private IEnumerator RutinaFotocopiar()
        {
            procesoEnMarcha = true;
            BloquearControles();

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

                if (barraProgresoImpresion != null)
                {
                    barraProgresoImpresion.value = t;
                }

                if (textoEstadoProceso != null)
                {
                    if (t < 0.35f)
                    {
                        textoEstadoProceso.text = "⚙ Escaneando documento original...";
                    }
                    else if (t < 0.75f)
                    {
                        textoEstadoProceso.text = "📄 Procesando e imprimiendo copias...";
                    }
                    else
                    {
                        textoEstadoProceso.text = "📁 Expulsando copias a la bandeja de salida...";
                    }
                }

                // Efecto parpadeo sutil de escáner en la imagen del documento
                if (imagenDocumentoEnBandeja != null)
                {
                    float alphaLuz = 0.7f + Mathf.PingPong(tiempoPasado * 4f, 0.3f);
                    imagenDocumentoEnBandeja.color = new Color(1f, 1f, 1f, alphaLuz);
                }

                yield return null;
            }

            if (barraProgresoImpresion != null)
            {
                barraProgresoImpresion.value = 1f;
            }

            if (imagenDocumentoEnBandeja != null)
            {
                imagenDocumentoEnBandeja.color = Color.white;
            }

            FinalizarFotocopiado();
        }

        private void FinalizarFotocopiado()
        {
            procesoEnMarcha = false;
            minijuegoTerminado = true;

            // Determinar si fue cumplimiento o sabotaje
            if (documentoEnBandeja == TipoDocumento.Informe)
            {
                // Cumplimiento legítimo
                resultadoExito = true;
                mensajeVentanaFinal = "Informes oficiales fotocopiados con éxito.\nSe ha archivado el duplicado en la Carpeta.";

                // Reducir la sospecha
                if (EstadoPartida.Instancia != null)
                {
                    EstadoPartida.Instancia.ReducirSospecha(15);
                }

                // Consumir el informe del inventario si corresponde
                ConsumirItemUsado(idInforme, idInformeAlternativo);

                // Entregar el objeto resultante: la Carpeta
                EntregarCarpetaAlJugador();

                if (textoEstadoProceso != null)
                {
                    textoEstadoProceso.text = "<color=#55FF88><b>✔ ¡COMPLETADO!</b> Informes duplicados correctamente.</color>";
                }

                Debug.Log("[MinijuegoFotocopiar] ¡Fotocopiado exitoso! Informes copiados y sospecha reducida.");
            }
            else if (documentoEnBandeja == TipoDocumento.FotoChiqui)
            {
                // Sabotaje cometido: arruina la tarea
                resultadoExito = false;
                mensajeVentanaFinal = "¡Tarea Saboteada!\nHas fotocopiado la imagen de Chiqui Tapia en lugar de los informes del jefe.";

                // Registrar sabotaje y aumentar sospecha
                if (EstadoPartida.Instancia != null)
                {
                    EstadoPartida.Instancia.RegistrarSabotaje(3, 15);
                }

                // Consumir la foto del inventario si corresponde
                ConsumirItemUsado(idFotoChiqui, idFotoChiquiAlternativo);

                // Entregar el objeto resultante: la Carpeta
                EntregarCarpetaAlJugador();

                if (textoEstadoProceso != null)
                {
                    textoEstadoProceso.text = "<color=#FF5555><b>⚠ ¡TAREA SABOTEADA!</b> La foto de Chiqui Tapia fue fotocopiada.</color>";
                }

                Debug.LogWarning("[MinijuegoFotocopiar] ¡Sabotaje cometido! Se fotocopió la foto de Chiqui Tapia. Sospecha aumentada.");
            }

            MostrarVentanaFinal(mensajeVentanaFinal, resultadoExito);
        }

        private void ConsumirItemUsado(string idPrincipal, string idAlternativo)
        {
            if (Inventario == null) return;

            if (Inventario.TieneItem(idPrincipal))
            {
                Inventario.QuitarItem(idPrincipal);
            }
            else if (!string.IsNullOrEmpty(idAlternativo) && Inventario.TieneItem(idAlternativo))
            {
                Inventario.QuitarItem(idAlternativo);
            }
        }

        private void EntregarCarpetaAlJugador()
        {
            if (Inventario == null) return;

            // Intentar cargar el item Carpeta desde Resources
            DatosItem itemCarpeta = Resources.Load<DatosItem>("Items/" + idCarpetaResultado);
            if (itemCarpeta == null)
            {
                // Fallback por si está registrado sin subcarpeta
                itemCarpeta = Resources.Load<DatosItem>(idCarpetaResultado);
            }

            if (itemCarpeta != null)
            {
                bool agregado = Inventario.IntentarAgregarItem(itemCarpeta);
                if (agregado)
                {
                    Debug.Log($"[MinijuegoFotocopiar] Se entregó el ítem '{itemCarpeta.nombre}' al inventario del jugador.");
                }
                else
                {
                    Debug.LogWarning("[MinijuegoFotocopiar] El inventario está lleno; no se pudo entregar la Carpeta.");
                }
            }
            else
            {
                Debug.LogWarning($"[MinijuegoFotocopiar] No se encontró el DatosItem para '{idCarpetaResultado}' en Resources/Items.");
            }
        }

        #endregion

        #region Bloqueo y Ventana Final

        private void BloquearControles()
        {
            if (botonFotocopiar != null) botonFotocopiar.interactable = false;
            if (botonSalir != null) botonSalir.interactable = false;
            if (botonSeleccionarInforme != null) botonSeleccionarInforme.interactable = false;
            if (botonSeleccionarChiqui != null) botonSeleccionarChiqui.interactable = false;
        }

        private void DesbloquearControles()
        {
            if (botonSalir != null) botonSalir.interactable = true;
            ActualizarEstadoBotonesSeleccion(JugadorTieneInforme(), JugadorTieneFotoChiqui());
            ActualizarEstadoBotonFotocopiar();
        }

        public void MostrarVentanaFinal(string mensaje, bool exito)
        {
            CrearVentanaPopupSiNoExiste();

            if (ventanaPopup != null)
            {
                ventanaPopup.SetActive(true);
                ventanaPopup.transform.SetAsLastSibling();
            }

            if (textoMensajeVentana != null)
            {
                textoMensajeVentana.text = mensaje;
            }

            if (textoTituloVentana != null)
            {
                textoTituloVentana.text = exito ? "Aviso del Sistema" : "Alerta de Seguridad";
            }

            if (fondoBarraTituloVentana != null)
            {
                fondoBarraTituloVentana.color = exito
                    ? new Color(0.12f, 0.45f, 0.24f, 1f)
                    : new Color(0.65f, 0.15f, 0.15f, 1f);
            }

            if (botonAceptarVentana != null)
            {
                botonAceptarVentana.interactable = true;
            }
        }

        private void CrearVentanaPopupSiNoExiste()
        {
            if (ventanaPopup != null) return;

            Transform padre = panelRaiz != null ? panelRaiz.transform : transform;

            // Ventana Marco Principal
            ventanaPopup = new GameObject("VentanaResultado", typeof(RectTransform), typeof(Image));
            ventanaPopup.transform.SetParent(padre, false);
            RectTransform rtVentana = ventanaPopup.GetComponent<RectTransform>();
            rtVentana.anchorMin = new Vector2(0.5f, 0.5f);
            rtVentana.anchorMax = new Vector2(0.5f, 0.5f);
            rtVentana.pivot = new Vector2(0.5f, 0.5f);
            rtVentana.anchoredPosition = Vector2.zero;
            rtVentana.sizeDelta = new Vector2(520f, 240f);

            Image imgFondo = ventanaPopup.GetComponent<Image>();
            imgFondo.color = new Color(0.13f, 0.15f, 0.20f, 0.98f);

            // Barra de Título
            GameObject objBarra = new GameObject("BarraTitulo", typeof(RectTransform), typeof(Image));
            objBarra.transform.SetParent(ventanaPopup.transform, false);
            RectTransform rtBarra = objBarra.GetComponent<RectTransform>();
            rtBarra.anchorMin = new Vector2(0f, 1f);
            rtBarra.anchorMax = new Vector2(1f, 1f);
            rtBarra.pivot = new Vector2(0.5f, 1f);
            rtBarra.anchoredPosition = Vector2.zero;
            rtBarra.sizeDelta = new Vector2(0f, 40f);
            fondoBarraTituloVentana = objBarra.GetComponent<Image>();
            fondoBarraTituloVentana.color = new Color(0.18f, 0.22f, 0.32f, 1f);

            // Texto Título
            GameObject objTxtTit = new GameObject("Titulo", typeof(RectTransform), typeof(Text));
            objTxtTit.transform.SetParent(objBarra.transform, false);
            RectTransform rtTit = objTxtTit.GetComponent<RectTransform>();
            rtTit.anchorMin = Vector2.zero;
            rtTit.anchorMax = Vector2.one;
            rtTit.offsetMin = new Vector2(15f, 0f);
            rtTit.offsetMax = new Vector2(-15f, 0f);
            textoTituloVentana = objTxtTit.GetComponent<Text>();
            textoTituloVentana.font = ConstructorUI.Fuente;
            textoTituloVentana.fontSize = 16;
            textoTituloVentana.fontStyle = FontStyle.Bold;
            textoTituloVentana.alignment = TextAnchor.MiddleLeft;
            textoTituloVentana.color = Color.white;
            textoTituloVentana.text = "Aviso del Sistema";

            // Texto Mensaje Central
            GameObject objTxtMsg = new GameObject("Mensaje", typeof(RectTransform), typeof(Text));
            objTxtMsg.transform.SetParent(ventanaPopup.transform, false);
            RectTransform rtMsg = objTxtMsg.GetComponent<RectTransform>();
            rtMsg.anchorMin = new Vector2(0f, 0.3f);
            rtMsg.anchorMax = new Vector2(1f, 0.85f);
            rtMsg.offsetMin = new Vector2(25f, 0f);
            rtMsg.offsetMax = new Vector2(-25f, 0f);
            textoMensajeVentana = objTxtMsg.GetComponent<Text>();
            textoMensajeVentana.font = ConstructorUI.Fuente;
            textoMensajeVentana.fontSize = 17;
            textoMensajeVentana.alignment = TextAnchor.MiddleCenter;
            textoMensajeVentana.color = Color.white;

            // Botón Aceptar
            GameObject objBtnAceptar = new GameObject("BotonAceptar", typeof(RectTransform), typeof(Image), typeof(Button));
            objBtnAceptar.transform.SetParent(ventanaPopup.transform, false);
            RectTransform rtBtn = objBtnAceptar.GetComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(0.5f, 0f);
            rtBtn.anchorMax = new Vector2(0.5f, 0f);
            rtBtn.pivot = new Vector2(0.5f, 0f);
            rtBtn.anchoredPosition = new Vector2(0f, 20f);
            rtBtn.sizeDelta = new Vector2(150f, 40f);

            Image imgBtn = objBtnAceptar.GetComponent<Image>();
            imgBtn.color = new Color(0.24f, 0.28f, 0.38f, 1f);

            botonAceptarVentana = objBtnAceptar.GetComponent<Button>();
            botonAceptarVentana.colors = ConstructorUI.ColoresBoton();
            botonAceptarVentana.onClick.RemoveAllListeners();
            botonAceptarVentana.onClick.AddListener(Salir);

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

        #endregion

        #region Configuración de Audio y UI Automática

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
            // Cargar recursos si no están asignados
            if (spriteFondoComputadora == null)
            {
                spriteFondoComputadora = Resources.Load<Sprite>("Sprites/Minijuegos/fondo computadora");
            }
            if (spriteImpresora == null)
            {
                spriteImpresora = Resources.Load<Sprite>("Sprites/Minijuegos/impresora-sinfondo");
            }
            if (spriteIconoInforme == null)
            {
                spriteIconoInforme = Resources.Load<Sprite>("Sprites/Minijuegos/documento_importante_24px");
                if (spriteIconoInforme == null)
                {
                    spriteIconoInforme = Resources.Load<Sprite>("Sprites/Minijuegos/papelescrito-sinfondo");
                }
            }
            if (spriteIconoChiqui == null)
            {
                spriteIconoChiqui = Resources.Load<Sprite>("Sprites/Minijuegos/chiqui tapia formato hoja");
            }

            // Panel Principal
            RectTransform rtPanel = panelRaiz != null ? panelRaiz.GetComponent<RectTransform>() : GetComponent<RectTransform>();
            if (rtPanel != null)
            {
                rtPanel.sizeDelta = new Vector2(1150f, 748f);
            }

            // Si ya tiene hijos armados por Inspector, no duplicar
            if (imagenFondo != null && botonFotocopiar != null && imagenImpresoraVisual != null)
            {
                return;
            }

            // 1. Imagen Fondo
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
                if (spriteFondoComputadora != null)
                {
                    imagenFondo.sprite = spriteFondoComputadora;
                    imagenFondo.color = Color.white;
                }
                else
                {
                    imagenFondo.color = new Color(0.12f, 0.14f, 0.18f, 1f);
                }
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
                rtSalir.anchoredPosition = new Vector2(-25f, -25f);
                rtSalir.sizeDelta = new Vector2(110f, 40f);

                Image imgSalir = objSalir.GetComponent<Image>();
                imgSalir.color = new Color(0.78f, 0.20f, 0.20f, 0.95f);

                botonSalir = objSalir.GetComponent<Button>();
                botonSalir.colors = ConstructorUI.ColoresBoton();

                GameObject objTxtSalir = new GameObject("Txt", typeof(RectTransform), typeof(Text));
                objTxtSalir.transform.SetParent(objSalir.transform, false);
                ConstructorUI.Estirar(objTxtSalir.GetComponent<RectTransform>());
                Text txtSalir = objTxtSalir.GetComponent<Text>();
                txtSalir.text = "✕ Salir";
                txtSalir.fontSize = 15;
                txtSalir.alignment = TextAnchor.MiddleCenter;
                txtSalir.color = Color.white;
                txtSalir.font = ConstructorUI.Fuente;
            }

            // Título Superior
            if (textoInstrucciones == null)
            {
                GameObject objTit = new GameObject("TituloMinijuego", typeof(RectTransform), typeof(Text));
                objTit.transform.SetParent(transform, false);
                RectTransform rtTit = objTit.GetComponent<RectTransform>();
                rtTit.anchorMin = new Vector2(0.5f, 1f);
                rtTit.anchorMax = new Vector2(0.5f, 1f);
                rtTit.pivot = new Vector2(0.5f, 1f);
                rtTit.anchoredPosition = new Vector2(0f, -28f);
                rtTit.sizeDelta = new Vector2(700f, 40f);

                Text txt = objTit.GetComponent<Text>();
                txt.text = "<b>SISTEMA DE FOTOCOPIADO E IMPRESIÓN</b>";
                txt.font = ConstructorUI.Fuente;
                txt.fontSize = 22;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                textoInstrucciones = txt;
            }

            // Subtítulo / Indicación
            GameObject objSub = new GameObject("Subtitulo", typeof(RectTransform), typeof(Text));
            objSub.transform.SetParent(transform, false);
            RectTransform rtSub = objSub.GetComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 1f);
            rtSub.anchorMax = new Vector2(0.5f, 1f);
            rtSub.pivot = new Vector2(0.5f, 1f);
            rtSub.anchoredPosition = new Vector2(0f, -65f);
            rtSub.sizeDelta = new Vector2(800f, 30f);
            Text txtSub = objSub.GetComponent<Text>();
            txtSub.text = "Selecciona el documento en la computadora y presiona <b>FOTOCOPIAR</b>.";
            txtSub.font = ConstructorUI.Fuente;
            txtSub.fontSize = 15;
            txtSub.alignment = TextAnchor.MiddleCenter;
            txtSub.color = new Color(0.8f, 0.85f, 0.95f, 0.9f);

            // ==========================================
            // LADO IZQUIERDO: ARCHIVOS EN COMPUTADORA
            // ==========================================
            GameObject panelArchivos = new GameObject("PanelArchivosPC", typeof(RectTransform), typeof(Image));
            panelArchivos.transform.SetParent(transform, false);
            RectTransform rtPnlArch = panelArchivos.GetComponent<RectTransform>();
            rtPnlArch.anchorMin = new Vector2(0f, 0.5f);
            rtPnlArch.anchorMax = new Vector2(0f, 0.5f);
            rtPnlArch.pivot = new Vector2(0f, 0.5f);
            rtPnlArch.anchoredPosition = new Vector2(50f, 30f);
            rtPnlArch.sizeDelta = new Vector2(440f, 480f);
            Image imgPnlArch = panelArchivos.GetComponent<Image>();
            imgPnlArch.color = new Color(0.15f, 0.18f, 0.24f, 0.85f);

            // Título Panel PC
            GameObject objTxtPC = new GameObject("TituloPC", typeof(RectTransform), typeof(Text));
            objTxtPC.transform.SetParent(panelArchivos.transform, false);
            RectTransform rtTxtPC = objTxtPC.GetComponent<RectTransform>();
            rtTxtPC.anchorMin = new Vector2(0.5f, 1f);
            rtTxtPC.anchorMax = new Vector2(0.5f, 1f);
            rtTxtPC.pivot = new Vector2(0.5f, 1f);
            rtTxtPC.anchoredPosition = new Vector2(0f, -15f);
            rtTxtPC.sizeDelta = new Vector2(400f, 30f);
            Text tPC = objTxtPC.GetComponent<Text>();
            tPC.text = "<b>📁 ARCHIVOS EN EL ESCRITORIO</b>";
            tPC.font = ConstructorUI.Fuente;
            tPC.fontSize = 16;
            tPC.alignment = TextAnchor.MiddleCenter;
            tPC.color = new Color(0.9f, 0.95f, 1f, 1f);

            // 1. Tarjeta Selección: Informes del Jefe
            botonSeleccionarInforme = CrearTarjetaArchivo(panelArchivos.transform,
                "BotonInforme",
                "Informes del Jefe (Oficial)",
                "Documento solicitado para fotocopiar",
                spriteIconoInforme,
                new Vector2(0f, -65f),
                out marcoSeleccionInforme);

            // 2. Tarjeta Selección: Foto Chiqui Tapia
            botonSeleccionarChiqui = CrearTarjetaArchivo(panelArchivos.transform,
                "BotonChiqui",
                "Foto de Chiqui Tapia",
                "¡CUIDADO! Arruinará la tarea (Sabotaje)",
                spriteIconoChiqui,
                new Vector2(0f, -225f),
                out marcoSeleccionChiqui);

            // Texto de ayuda en panel PC
            GameObject objAyuda = new GameObject("TxtAyuda", typeof(RectTransform), typeof(Text));
            objAyuda.transform.SetParent(panelArchivos.transform, false);
            RectTransform rtAyuda = objAyuda.GetComponent<RectTransform>();
            rtAyuda.anchorMin = new Vector2(0.5f, 0f);
            rtAyuda.anchorMax = new Vector2(0.5f, 0f);
            rtAyuda.pivot = new Vector2(0.5f, 0f);
            rtAyuda.anchoredPosition = new Vector2(0f, 15f);
            rtAyuda.sizeDelta = new Vector2(400f, 40f);
            Text tAyuda = objAyuda.GetComponent<Text>();
            tAyuda.text = "<i>Haz clic sobre el archivo para cargarlo en la bandeja del escáner de la impresora.</i>";
            tAyuda.font = ConstructorUI.Fuente;
            tAyuda.fontSize = 12;
            tAyuda.alignment = TextAnchor.MiddleCenter;
            tAyuda.color = new Color(0.7f, 0.75f, 0.85f, 0.8f);

            // ==========================================
            // LADO DERECHO: SALA DE IMPRESORA / ESCÁNER
            // ==========================================
            GameObject panelImpresora = new GameObject("PanelImpresora", typeof(RectTransform), typeof(Image));
            panelImpresora.transform.SetParent(transform, false);
            RectTransform rtPnlImp = panelImpresora.GetComponent<RectTransform>();
            rtPnlImp.anchorMin = new Vector2(1f, 0.5f);
            rtPnlImp.anchorMax = new Vector2(1f, 0.5f);
            rtPnlImp.pivot = new Vector2(1f, 0.5f);
            rtPnlImp.anchoredPosition = new Vector2(-50f, 30f);
            rtPnlImp.sizeDelta = new Vector2(560f, 480f);
            Image imgPnlImp = panelImpresora.GetComponent<Image>();
            imgPnlImp.color = new Color(0.12f, 0.14f, 0.19f, 0.85f);

            // Título Panel Impresora
            GameObject objTxtImp = new GameObject("TituloImp", typeof(RectTransform), typeof(Text));
            objTxtImp.transform.SetParent(panelImpresora.transform, false);
            RectTransform rtTxtImp = objTxtImp.GetComponent<RectTransform>();
            rtTxtImp.anchorMin = new Vector2(0.5f, 1f);
            rtTxtImp.anchorMax = new Vector2(0.5f, 1f);
            rtTxtImp.pivot = new Vector2(0.5f, 1f);
            rtTxtImp.anchoredPosition = new Vector2(0f, -15f);
            rtTxtImp.sizeDelta = new Vector2(500f, 30f);
            Text tImp = objTxtImp.GetComponent<Text>();
            tImp.text = "<b>🖨 IMPRESORA / FOTOCOPIADORA OFICIAL</b>";
            tImp.font = ConstructorUI.Fuente;
            tImp.fontSize = 16;
            tImp.alignment = TextAnchor.MiddleCenter;
            tImp.color = new Color(0.9f, 0.95f, 1f, 1f);

            // Imagen Gráfica de la Impresora
            GameObject objImgImp = new GameObject("GraficoImpresora", typeof(RectTransform), typeof(Image));
            objImgImp.transform.SetParent(panelImpresora.transform, false);
            RectTransform rtImgImp = objImgImp.GetComponent<RectTransform>();
            rtImgImp.anchorMin = new Vector2(0.5f, 1f);
            rtImgImp.anchorMax = new Vector2(0.5f, 1f);
            rtImgImp.pivot = new Vector2(0.5f, 1f);
            rtImgImp.anchoredPosition = new Vector2(0f, -55f);
            rtImgImp.sizeDelta = new Vector2(240f, 190f);
            imagenImpresoraVisual = objImgImp.GetComponent<Image>();
            imagenImpresoraVisual.preserveAspect = true;
            if (spriteImpresora != null)
            {
                imagenImpresoraVisual.sprite = spriteImpresora;
            }

            // Bandeja de Escáner (Vista Previa del documento cargado)
            GameObject objBandeja = new GameObject("BandejaEscaner", typeof(RectTransform), typeof(Image));
            objBandeja.transform.SetParent(panelImpresora.transform, false);
            RectTransform rtBandeja = objBandeja.GetComponent<RectTransform>();
            rtBandeja.anchorMin = new Vector2(0.5f, 1f);
            rtBandeja.anchorMax = new Vector2(0.5f, 1f);
            rtBandeja.pivot = new Vector2(0.5f, 1f);
            rtBandeja.anchoredPosition = new Vector2(0f, -255f);
            rtBandeja.sizeDelta = new Vector2(480f, 75f);
            Image imgBandeja = objBandeja.GetComponent<Image>();
            imgBandeja.color = new Color(0.08f, 0.10f, 0.14f, 0.95f);

            // Icono del documento en la bandeja
            GameObject objDocBandeja = new GameObject("IconoDocumentoBandeja", typeof(RectTransform), typeof(Image));
            objDocBandeja.transform.SetParent(objBandeja.transform, false);
            RectTransform rtDocBandeja = objDocBandeja.GetComponent<RectTransform>();
            rtDocBandeja.anchorMin = new Vector2(0f, 0.5f);
            rtDocBandeja.anchorMax = new Vector2(0f, 0.5f);
            rtDocBandeja.pivot = new Vector2(0f, 0.5f);
            rtDocBandeja.anchoredPosition = new Vector2(15f, 0f);
            rtDocBandeja.sizeDelta = new Vector2(55f, 55f);
            imagenDocumentoEnBandeja = objDocBandeja.GetComponent<Image>();
            imagenDocumentoEnBandeja.preserveAspect = true;

            // Texto descriptivo en bandeja
            GameObject objTxtBandeja = new GameObject("TxtBandeja", typeof(RectTransform), typeof(Text));
            objTxtBandeja.transform.SetParent(objBandeja.transform, false);
            RectTransform rtTxtBandeja = objTxtBandeja.GetComponent<RectTransform>();
            rtTxtBandeja.anchorMin = new Vector2(0f, 0f);
            rtTxtBandeja.anchorMax = new Vector2(1f, 1f);
            rtTxtBandeja.offsetMin = new Vector2(85f, 5f);
            rtTxtBandeja.offsetMax = new Vector2(-15f, -5f);
            textoDocumentoEnBandeja = objTxtBandeja.GetComponent<Text>();
            textoDocumentoEnBandeja.font = ConstructorUI.Fuente;
            textoDocumentoEnBandeja.fontSize = 14;
            textoDocumentoEnBandeja.alignment = TextAnchor.MiddleLeft;

            // Barra de Progreso
            GameObject objBarra = new GameObject("BarraProgreso", typeof(RectTransform), typeof(Slider));
            objBarra.transform.SetParent(panelImpresora.transform, false);
            RectTransform rtProg = objBarra.GetComponent<RectTransform>();
            rtProg.anchorMin = new Vector2(0.5f, 1f);
            rtProg.anchorMax = new Vector2(0.5f, 1f);
            rtProg.pivot = new Vector2(0.5f, 1f);
            rtProg.anchoredPosition = new Vector2(0f, -340f);
            rtProg.sizeDelta = new Vector2(480f, 18f);

            barraProgresoImpresion = objBarra.GetComponent<Slider>();
            barraProgresoImpresion.minValue = 0f;
            barraProgresoImpresion.maxValue = 1f;
            barraProgresoImpresion.value = 0f;

            // Fondo de la barra
            GameObject bgBarra = new GameObject("FondoBarra", typeof(RectTransform), typeof(Image));
            bgBarra.transform.SetParent(objBarra.transform, false);
            ConstructorUI.Estirar(bgBarra.GetComponent<RectTransform>());
            bgBarra.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 1f);

            // Relleno de la barra
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
            barraProgresoImpresion.fillRect = rtFill;

            // Texto de Estado
            GameObject objTxtEst = new GameObject("TextoEstado", typeof(RectTransform), typeof(Text));
            objTxtEst.transform.SetParent(panelImpresora.transform, false);
            RectTransform rtTxtEst = objTxtEst.GetComponent<RectTransform>();
            rtTxtEst.anchorMin = new Vector2(0.5f, 1f);
            rtTxtEst.anchorMax = new Vector2(0.5f, 1f);
            rtTxtEst.pivot = new Vector2(0.5f, 1f);
            rtTxtEst.anchoredPosition = new Vector2(0f, -365f);
            rtTxtEst.sizeDelta = new Vector2(480f, 26f);
            textoEstadoProceso = objTxtEst.GetComponent<Text>();
            textoEstadoProceso.font = ConstructorUI.Fuente;
            textoEstadoProceso.fontSize = 14;
            textoEstadoProceso.alignment = TextAnchor.MiddleCenter;
            textoEstadoProceso.color = Color.white;

            // Botón Principal: FOTOCOPIAR
            GameObject objBtnFoto = new GameObject("BotonFotocopiar", typeof(RectTransform), typeof(Image), typeof(Button));
            objBtnFoto.transform.SetParent(panelImpresora.transform, false);
            RectTransform rtBtnFoto = objBtnFoto.GetComponent<RectTransform>();
            rtBtnFoto.anchorMin = new Vector2(0.5f, 0f);
            rtBtnFoto.anchorMax = new Vector2(0.5f, 0f);
            rtBtnFoto.pivot = new Vector2(0.5f, 0f);
            rtBtnFoto.anchoredPosition = new Vector2(0f, 20f);
            rtBtnFoto.sizeDelta = new Vector2(320f, 50f);

            Image imgBtnFoto = objBtnFoto.GetComponent<Image>();
            imgBtnFoto.color = new Color(0.18f, 0.52f, 0.32f, 1f);

            botonFotocopiar = objBtnFoto.GetComponent<Button>();
            ColorBlock cbFoto = ConstructorUI.ColoresBoton();
            cbFoto.normalColor = Color.white;
            cbFoto.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cbFoto.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            cbFoto.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            botonFotocopiar.colors = cbFoto;

            GameObject objTxtBtnFoto = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            objTxtBtnFoto.transform.SetParent(objBtnFoto.transform, false);
            ConstructorUI.Estirar(objTxtBtnFoto.GetComponent<RectTransform>());
            Text tBtnFoto = objTxtBtnFoto.GetComponent<Text>();
            tBtnFoto.font = ConstructorUI.Fuente;
            tBtnFoto.fontSize = 18;
            tBtnFoto.fontStyle = FontStyle.Bold;
            tBtnFoto.alignment = TextAnchor.MiddleCenter;
            tBtnFoto.color = Color.white;
            tBtnFoto.text = "🖨 FOTOCOPIAR";
        }

        private Button CrearTarjetaArchivo(Transform padre, string nombreObj, string titulo, string descripcion, Sprite icono, Vector2 pos, out Image marco)
        {
            GameObject tarjeta = new GameObject(nombreObj, typeof(RectTransform), typeof(Image), typeof(Button));
            tarjeta.transform.SetParent(padre, false);
            RectTransform rt = tarjeta.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400f, 140f);

            marco = tarjeta.GetComponent<Image>();
            marco.color = new Color(0.25f, 0.30f, 0.40f, 0.4f);

            Button btn = tarjeta.GetComponent<Button>();
            btn.colors = ConstructorUI.ColoresBoton();

            // Fondo interior
            GameObject interior = new GameObject("Interior", typeof(RectTransform), typeof(Image));
            interior.transform.SetParent(tarjeta.transform, false);
            RectTransform rtInt = interior.GetComponent<RectTransform>();
            rtInt.anchorMin = Vector2.zero;
            rtInt.anchorMax = Vector2.one;
            rtInt.offsetMin = new Vector2(3f, 3f);
            rtInt.offsetMax = new Vector2(-3f, -3f);
            interior.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            // Icono
            GameObject objIcono = new GameObject("Icono", typeof(RectTransform), typeof(Image));
            objIcono.transform.SetParent(interior.transform, false);
            RectTransform rtIcono = objIcono.GetComponent<RectTransform>();
            rtIcono.anchorMin = new Vector2(0f, 0.5f);
            rtIcono.anchorMax = new Vector2(0f, 0.5f);
            rtIcono.pivot = new Vector2(0f, 0.5f);
            rtIcono.anchoredPosition = new Vector2(15f, 0f);
            rtIcono.sizeDelta = new Vector2(90f, 90f);
            Image imgIcono = objIcono.GetComponent<Image>();
            imgIcono.preserveAspect = true;
            if (icono != null)
            {
                imgIcono.sprite = icono;
            }

            // Título
            GameObject objTit = new GameObject("Titulo", typeof(RectTransform), typeof(Text));
            objTit.transform.SetParent(interior.transform, false);
            RectTransform rtTit = objTit.GetComponent<RectTransform>();
            rtTit.anchorMin = new Vector2(0f, 1f);
            rtTit.anchorMax = new Vector2(1f, 1f);
            rtTit.pivot = new Vector2(0f, 1f);
            rtTit.anchoredPosition = new Vector2(120f, -20f);
            rtTit.sizeDelta = new Vector2(-135f, 30f);
            Text tTit = objTit.GetComponent<Text>();
            tTit.text = $"<b>{titulo}</b>";
            tTit.font = ConstructorUI.Fuente;
            tTit.fontSize = 15;
            tTit.alignment = TextAnchor.UpperLeft;
            tTit.color = Color.white;

            // Descripción
            GameObject objDesc = new GameObject("Desc", typeof(RectTransform), typeof(Text));
            objDesc.transform.SetParent(interior.transform, false);
            RectTransform rtDesc = objDesc.GetComponent<RectTransform>();
            rtDesc.anchorMin = new Vector2(0f, 0f);
            rtDesc.anchorMax = new Vector2(1f, 1f);
            rtDesc.offsetMin = new Vector2(120f, 35f);
            rtDesc.offsetMax = new Vector2(-15f, -50f);
            Text tDesc = objDesc.GetComponent<Text>();
            tDesc.text = descripcion;
            tDesc.font = ConstructorUI.Fuente;
            tDesc.fontSize = 12;
            tDesc.alignment = TextAnchor.UpperLeft;
            tDesc.color = new Color(0.75f, 0.8f, 0.9f, 0.9f);

            // Botón de acción rápido ("Poner en Impresora")
            GameObject objAccion = new GameObject("IndicadorAccion", typeof(RectTransform), typeof(Text));
            objAccion.transform.SetParent(interior.transform, false);
            RectTransform rtAcc = objAccion.GetComponent<RectTransform>();
            rtAcc.anchorMin = new Vector2(0f, 0f);
            rtAcc.anchorMax = new Vector2(1f, 0f);
            rtAcc.pivot = new Vector2(0f, 0f);
            rtAcc.anchoredPosition = new Vector2(120f, 12f);
            rtAcc.sizeDelta = new Vector2(-135f, 22f);
            Text tAcc = objAccion.GetComponent<Text>();
            tAcc.text = "➔ <i>Clic para poner en impresora</i>";
            tAcc.font = ConstructorUI.Fuente;
            tAcc.fontSize = 12;
            tAcc.alignment = TextAnchor.MiddleLeft;
            tAcc.color = new Color(0.4f, 0.8f, 1f, 1f);

            return btn;
        }

        #endregion
    }
}
