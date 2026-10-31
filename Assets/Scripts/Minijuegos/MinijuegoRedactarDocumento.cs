using System;
using System.Collections;
using SoltaLaPala.Inventory;
using SoltaLaPala.Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoltaLaPala.Minijuegos
{
    /// <summary>
    /// Minijuego: Redactar documento (Nivel 3).
    ///
    /// Objetivo legítimo:
    ///   Elegir la birome y pasarla sobre la hoja en blanco para redactar el informe oficial de la empresa.
    ///   Al completarse, reduce la sospecha y entrega el documento redactado.
    ///
    /// Sabotaje (arruinar la tarea):
    ///   Elegir las crayolas infantiles y pintar sobre la hoja para revelar un dibujo de un niño.
    ///   Al completarse, comete sabotaje, aumenta la sospecha y entrega la hoja arruinada.
    /// </summary>
    public class MinijuegoRedactarDocumento : MinijuegoBase, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public enum HerramientaEscritura
        {
            Ninguna,
            Birome,
            Crayola
        }

        [Header("Herramienta activa")]
        public HerramientaEscritura herramientaActual = HerramientaEscritura.Ninguna;

        [Header("Texturas e Imágenes de Fondo")]
        public Sprite spriteFondoEscritorio;
        public Sprite spritePapelBlanco;
        public Texture2D texturaReporte;
        public Texture2D texturaDibujo;
        public Image imagenFondoEscritorio;
        public Image imagenPapelFondo;
        public RawImage rawImageRevelado;
        public RectTransform rectTransformPapel;

        [Header("Sprites de Herramientas")]
        public Sprite spriteBirome;
        public Sprite spriteCrayola;
        public Sprite spriteIconoDocumento;
        public Sprite spriteIconoDibujo;

        [Header("Botones de Selección")]
        public Button botonBirome;
        public Button botonCrayola;
        public Image marcoSeleccionBirome;
        public Image marcoSeleccionCrayola;
        public Text textoHerramientaActiva;

        [Header("Cursor de Herramienta")]
        public Image cursorHerramienta;
        public RectTransform rectCursor;

        [Header("Progreso y Controles")]
        public Slider barraProgreso;
        public Text textoProgreso;
        public Text textoInstrucciones;
        public Button botonSalir;

        [Header("UI - Ventana de Estado Final")]
        public GameObject ventanaPopup;
        public Text textoTituloVentana;
        public Text textoMensajeVentana;
        public Button botonAceptarVentana;
        public Image fondoBarraTituloVentana;
        public Image iconoVentanaResultado;

        [Header("IDs de Ítems e Inventario")]
        public string idBirome = "Birome";
        public string idCrayolas = "Crayolas";
        public string idDocumento = "Documento";
        public string idDibujo = "Dibujo";

        [Header("Ajustes")]
        [Tooltip("Si es true, permite probar el minijuego sin requerir tener birome o crayolas en el inventario")]
        public bool modoPrueba = false;
        public int radioBirome = 24;
        public int radioCrayola = 34;

        // Textura dinámica y mapa de revelado
        private const int AnchoTex = 400;
        private const int AltoTex = 500;
        private Texture2D texturaDinamica;
        private bool[,] mapaRevelado;
        private int totalPixelesRelevantes;
        private int pixelesReveladosTotal;
        private int pixelesReveladosBirome;
        private int pixelesReveladosCrayola;

        // Trazo continuo
        private Vector2? ultimaPosicionPixel;
        private bool ratonPresionadoSobrePapel = false;

        // Persistencia de estado
        private bool yaInicializado = false;
        private bool minijuegoTerminado = false;
        private bool resultadoExito = false;
        private string mensajeVentanaFinal = "";

        protected override void AlIniciar()
        {
            if (panelRaiz == null)
            {
                panelRaiz = gameObject;
            }

            ConstruirUIAutomaticaSiEsNecesario();
            CrearVentanaPopupSiNoExiste();
            InicializarTexturaDinamicaSiEsNecesario();

            ConfigurarBotonesHerramientas();

            if (botonSalir != null)
            {
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
            }

            // Si ya terminó previamente en esta misma sesión, mostrar su estado final
            if (minijuegoTerminado)
            {
                MostrarVentanaFinal(mensajeVentanaFinal, resultadoExito);
                ActualizarHUD();
                return;
            }

            // Seleccionar automáticamente la herramienta disponible
            bool puedeBirome = PuedeUsarHerramienta(HerramientaEscritura.Birome);
            bool puedeCrayola = PuedeUsarHerramienta(HerramientaEscritura.Crayola);

            if (herramientaActual == HerramientaEscritura.Ninguna)
            {
                if (puedeBirome)
                {
                    SeleccionarHerramienta(HerramientaEscritura.Birome);
                }
                else if (puedeCrayola)
                {
                    SeleccionarHerramienta(HerramientaEscritura.Crayola);
                }
            }

            ActualizarHUD();
        }

        private void OnEnable()
        {
            if (EstadoPartida.Instancia != null)
            {
                EstadoPartida.Instancia.AlEmpezarNivel -= Reiniciar;
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
            resultadoExito = false;
            mensajeVentanaFinal = "";
            herramientaActual = HerramientaEscritura.Ninguna;
            pixelesReveladosTotal = 0;
            pixelesReveladosBirome = 0;
            pixelesReveladosCrayola = 0;
            ultimaPosicionPixel = null;
            ratonPresionadoSobrePapel = false;

            if (ventanaPopup != null)
            {
                ventanaPopup.SetActive(false);
            }

            if (panelRaiz != null)
            {
                panelRaiz.SetActive(false);
            }

            LimpiarTexturaDinamica();
            ActualizarHUD();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Salir();
                return;
            }

            ActualizarCursorHerramienta();
        }

        #region Selección de Herramientas

        private bool PuedeUsarHerramienta(HerramientaEscritura herramienta)
        {
            if (modoPrueba) return true;
            if (Inventario == null) return false;

            switch (herramienta)
            {
                case HerramientaEscritura.Birome:
                    return Inventario.TieneItem(idBirome);
                case HerramientaEscritura.Crayola:
                    return Inventario.TieneItem(idCrayolas);
                default:
                    return false;
            }
        }

        private void ConfigurarBotonesHerramientas()
        {
            bool puedeBirome = PuedeUsarHerramienta(HerramientaEscritura.Birome);
            bool puedeCrayola = PuedeUsarHerramienta(HerramientaEscritura.Crayola);

            if (botonBirome != null)
            {
                botonBirome.interactable = !minijuegoTerminado && puedeBirome;
                botonBirome.onClick.RemoveAllListeners();
                botonBirome.onClick.AddListener(() => SeleccionarHerramienta(HerramientaEscritura.Birome));

                Text txt = botonBirome.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = puedeBirome ? "Escribir con Birome" : "Birome (Falta en inventario)";
                }
            }

            if (botonCrayola != null)
            {
                botonCrayola.interactable = !minijuegoTerminado && puedeCrayola;
                botonCrayola.onClick.RemoveAllListeners();
                botonCrayola.onClick.AddListener(() => SeleccionarHerramienta(HerramientaEscritura.Crayola));

                Text txt = botonCrayola.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = puedeCrayola ? "Dibujar con Crayolas" : "Crayolas (Falta en inventario)";
                }
            }

            ActualizarMarcosSeleccion();
        }

        public void SeleccionarHerramienta(HerramientaEscritura herramienta)
        {
            if (minijuegoTerminado) return;
            if (!PuedeUsarHerramienta(herramienta)) return;

            herramientaActual = herramienta;
            ActualizarMarcosSeleccion();
            ActualizarHUD();
        }

        private void ActualizarMarcosSeleccion()
        {
            if (marcoSeleccionBirome != null)
            {
                marcoSeleccionBirome.enabled = (herramientaActual == HerramientaEscritura.Birome);
            }

            if (marcoSeleccionCrayola != null)
            {
                marcoSeleccionCrayola.enabled = (herramientaActual == HerramientaEscritura.Crayola);
            }
        }

        #endregion

        #region Lógica de Raspado y Revelado

        private void InicializarTexturaDinamicaSiEsNecesario()
        {
            if (texturaDinamica == null)
            {
                texturaDinamica = new Texture2D(AnchoTex, AltoTex, TextureFormat.RGBA32, false);
                texturaDinamica.filterMode = FilterMode.Bilinear;
                texturaDinamica.wrapMode = TextureWrapMode.Clamp;
            }

            if (mapaRevelado == null || mapaRevelado.GetLength(0) != AnchoTex || mapaRevelado.GetLength(1) != AltoTex)
            {
                mapaRevelado = new bool[AnchoTex, AltoTex];
                totalPixelesRelevantes = AnchoTex * AltoTex;
            }

            if (rawImageRevelado != null)
            {
                rawImageRevelado.texture = texturaDinamica;
            }

            if (!yaInicializado)
            {
                LimpiarTexturaDinamica();
                yaInicializado = true;
            }
        }

        private void LimpiarTexturaDinamica()
        {
            if (texturaDinamica == null) return;

            Color32 transparente = new Color32(0, 0, 0, 0);
            Color32[] pixeles = new Color32[AnchoTex * AltoTex];
            for (int i = 0; i < pixeles.Length; i++)
            {
                pixeles[i] = transparente;
            }

            texturaDinamica.SetPixels32(pixeles);
            texturaDinamica.Apply();

            if (mapaRevelado != null)
            {
                Array.Clear(mapaRevelado, 0, mapaRevelado.Length);
            }

            pixelesReveladosTotal = 0;
            pixelesReveladosBirome = 0;
            pixelesReveladosCrayola = 0;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (minijuegoTerminado) return;
            ratonPresionadoSobrePapel = true;
            ultimaPosicionPixel = null;
            ProcesarTrazoEnPunto(eventData.position, eventData.pressEventCamera);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (minijuegoTerminado) return;
            if (ratonPresionadoSobrePapel)
            {
                ProcesarTrazoEnPunto(eventData.position, eventData.pressEventCamera);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ratonPresionadoSobrePapel = false;
            ultimaPosicionPixel = null;
        }

        private void ProcesarTrazoEnPunto(Vector2 screenPoint, Camera cam)
        {
            if (minijuegoTerminado) return;

            if (herramientaActual == HerramientaEscritura.Ninguna)
            {
                if (textoInstrucciones != null)
                {
                    textoInstrucciones.text = "<color=#FFAA00>¡Selecciona primero la Birome o las Crayolas para escribir!</color>";
                }
                return;
            }

            if (rectTransformPapel == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransformPapel, screenPoint, cam, out Vector2 localPoint))
            {
                return;
            }

            Rect rect = rectTransformPapel.rect;
            float normX = (localPoint.x - rect.xMin) / rect.width;
            float normY = (localPoint.y - rect.yMin) / rect.height;

            if (normX < 0f || normX > 1f || normY < 0f || normY > 1f)
            {
                ultimaPosicionPixel = null;
                return;
            }

            int px = Mathf.Clamp((int)(normX * AnchoTex), 0, AnchoTex - 1);
            int py = Mathf.Clamp((int)(normY * AltoTex), 0, AltoTex - 1);
            Vector2 posActual = new Vector2(px, py);

            int radio = (herramientaActual == HerramientaEscritura.Crayola) ? radioCrayola : radioBirome;

            if (ultimaPosicionPixel.HasValue)
            {
                // Interpolar puntos para trazo continuo suave
                Vector2 anterior = ultimaPosicionPixel.Value;
                float dist = Vector2.Distance(anterior, posActual);
                int pasos = Mathf.Max(1, Mathf.CeilToInt(dist / (radio * 0.35f)));
                for (int i = 1; i <= pasos; i++)
                {
                    Vector2 interp = Vector2.Lerp(anterior, posActual, (float)i / pasos);
                    EstamparRevelado((int)interp.x, (int)interp.y, radio);
                }
            }
            else
            {
                EstamparRevelado(px, py, radio);
            }

            ultimaPosicionPixel = posActual;
            texturaDinamica.Apply();

            VerificarProgreso();
        }

        private Color ObtenerColorFuente(int x, int y, bool esCrayola)
        {
            if (esCrayola)
            {
                if (texturaDibujo == null) return Color.clear;

                // El dibujo se escala a 0.60f y se centra en la hoja para que sea más pequeño
                const float escala = 0.60f;
                float normX = (float)x / AnchoTex;
                float normY = (float)y / AltoTex;
                float u = (normX - 0.5f) / escala + 0.5f;
                float v = (normY - 0.5f) / escala + 0.5f;

                if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
                {
                    return texturaDibujo.GetPixelBilinear(u, v);
                }

                return Color.clear;
            }
            else
            {
                if (texturaReporte == null) return Color.clear;

                float u = (float)x / AnchoTex;
                float v = (float)y / AltoTex;
                return texturaReporte.GetPixelBilinear(u, v);
            }
        }

        private void EstamparRevelado(int centroX, int centroY, int radio)
        {
            int radioSqr = radio * radio;
            int minX = Mathf.Max(0, centroX - radio);
            int maxX = Mathf.Min(AnchoTex - 1, centroX + radio);
            int minY = Mathf.Max(0, centroY - radio);
            int maxY = Mathf.Min(AltoTex - 1, centroY + radio);
            bool esCrayola = (herramientaActual == HerramientaEscritura.Crayola);

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - centroY;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - centroX;
                    if (dx * dx + dy * dy <= radioSqr)
                    {
                        // Registrar píxel raspado/revelado
                        if (!mapaRevelado[x, y])
                        {
                            mapaRevelado[x, y] = true;
                            pixelesReveladosTotal++;

                            if (esCrayola)
                            {
                                pixelesReveladosCrayola++;
                            }
                            else
                            {
                                pixelesReveladosBirome++;
                            }
                        }

                        // Estampar color si hay información visual
                        Color colFuente = ObtenerColorFuente(x, y, esCrayola);
                        if (colFuente.a > 0.05f)
                        {
                            texturaDinamica.SetPixel(x, y, colFuente);
                        }
                    }
                }
            }
        }

        private void VerificarProgreso()
        {
            // Calibrado: revelar el área principal redactable (~48% de la superficie total) equivale al 100% de la tarea
            int objetivoPixeles = (int)(totalPixelesRelevantes * 0.48f);
            float progreso = objetivoPixeles > 0 ? Mathf.Clamp01((float)pixelesReveladosTotal / objetivoPixeles) : 0f;

            if (barraProgreso != null)
            {
                barraProgreso.value = progreso;
            }

            if (textoProgreso != null)
            {
                textoProgreso.text = $"{Mathf.RoundToInt(progreso * 100f)}%";
            }

            if (progreso >= 1f && !minijuegoTerminado)
            {
                FinalizarMinijuego();
            }
        }

        private void FinalizarMinijuego()
        {
            minijuegoTerminado = true;

            // Revelar completamente el arte correspondiente para un acabado limpio y uniforme
            bool fueSabotaje = pixelesReveladosCrayola > 0;
            RevelarCompleto(fueSabotaje);

            if (barraProgreso != null) barraProgreso.value = 1f;
            if (textoProgreso != null) textoProgreso.text = "100%";

            if (cursorHerramienta != null) cursorHerramienta.gameObject.SetActive(false);

            if (fueSabotaje)
            {
                // Resultado Sabotaje: dibujo infantil
                resultadoExito = false;
                mensajeVentanaFinal = "¡Has arruinado el documento oficial llenándolo con un dibujo de nene!\nUn informe así perjudica a la empresa.";

                if (EstadoPartida.Instancia != null)
                {
                    EstadoPartida.Instancia.RegistrarSabotaje(3, 10);
                }

                if (Inventario != null)
                {
                    Inventario.IntentarAgregarItem(Resources.Load<DatosItem>("Items/" + idDibujo));
                }

                MostrarVentanaFinal(mensajeVentanaFinal, false);
            }
            else
            {
                // Resultado Exitoso: informe prolijo con birome
                resultadoExito = true;
                mensajeVentanaFinal = "Limpieza de oficina: Has redactado el documento de la empresa con éxito.\nLa sospecha sobre ti ha disminuido.";

                if (EstadoPartida.Instancia != null)
                {
                    EstadoPartida.Instancia.ReducirSospecha(15);
                }

                if (Inventario != null)
                {
                    Inventario.IntentarAgregarItem(Resources.Load<DatosItem>("Items/" + idDocumento));
                }

                MostrarVentanaFinal(mensajeVentanaFinal, true);
            }

            ActualizarHUD();
        }

        private void RevelarCompleto(bool fueSabotaje)
        {
            if (texturaDinamica == null) return;

            for (int y = 0; y < AltoTex; y++)
            {
                for (int x = 0; x < AnchoTex; x++)
                {
                    Color col = ObtenerColorFuente(x, y, fueSabotaje);
                    if (col.a > 0.05f)
                    {
                        texturaDinamica.SetPixel(x, y, col);
                    }
                }
            }

            texturaDinamica.Apply();
        }

        #endregion

        #region Cursor y HUD

        private void ActualizarCursorHerramienta()
        {
            if (cursorHerramienta == null || rectCursor == null || rectTransformPapel == null) return;

            if (minijuegoTerminado || herramientaActual == HerramientaEscritura.Ninguna)
            {
                cursorHerramienta.gameObject.SetActive(false);
                return;
            }

            Vector2 mousePos = Input.mousePosition;
            Camera cam = null;
            Canvas c = GetComponentInParent<Canvas>();
            if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = c.worldCamera;
            }

            bool estaSobrePapel = RectTransformUtility.RectangleContainsScreenPoint(rectTransformPapel, mousePos, cam);

            cursorHerramienta.gameObject.SetActive(estaSobrePapel);

            if (estaSobrePapel)
            {
                // Centrado exacto del cursor sobre el punto del ratón
                rectCursor.pivot = new Vector2(0.5f, 0.5f);

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)cursorHerramienta.transform.parent, mousePos, cam, out Vector2 localPos))
                {
                    rectCursor.anchoredPosition = localPos;
                }

                cursorHerramienta.sprite = (herramientaActual == HerramientaEscritura.Crayola) ? spriteCrayola : spriteBirome;
            }
        }

        private void ActualizarHUD()
        {
            if (textoHerramientaActiva != null)
            {
                switch (herramientaActual)
                {
                    case HerramientaEscritura.Birome:
                        textoHerramientaActiva.text = "Herramienta: <color=#66CCFF>Birome</color>";
                        break;
                    case HerramientaEscritura.Crayola:
                        textoHerramientaActiva.text = "Herramienta: <color=#FFAA33>Crayolas</color>";
                        break;
                    default:
                        textoHerramientaActiva.text = "Herramienta: <color=#AAAAAA>Ninguna seleccionada</color>";
                        break;
                }
            }

            if (textoInstrucciones != null)
            {
                if (minijuegoTerminado)
                {
                    textoInstrucciones.text = resultadoExito
                        ? "<color=#55FF88><b>¡DOCUMENTO REDACTADO CON ÉXITO!</b></color>"
                        : "<color=#FF4444><b>¡DOCUMENTO ARRUINADO CON DIBUJO!</b></color>";
                }
                else
                {
                    switch (herramientaActual)
                    {
                        case HerramientaEscritura.Birome:
                            textoInstrucciones.text = "Mantén presionado el clic y pasa la <b>Birome</b> sobre la hoja para redactar el informe.";
                            break;
                        case HerramientaEscritura.Crayola:
                            textoInstrucciones.text = "Mantén presionado el clic y pasa las <b>Crayolas</b> sobre la hoja para dibujar...";
                            break;
                        default:
                            textoInstrucciones.text = "Elige con qué escribir: <b>Birome</b> (trabajo legítimo) o <b>Crayolas</b> (sabotaje).";
                            break;
                    }
                }
            }

            int objetivoPixeles = (int)(totalPixelesRelevantes * 0.48f);
            float progreso = objetivoPixeles > 0 ? Mathf.Clamp01((float)pixelesReveladosTotal / objetivoPixeles) : 0f;
            if (barraProgreso != null) barraProgreso.value = progreso;
            if (textoProgreso != null) textoProgreso.text = $"{Mathf.RoundToInt(progreso * 100f)}%";
        }

        #endregion

        #region Ventana Popup Modal

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
                textoTituloVentana.text = exito ? "Informe Completado" : "Alerta de Sabotaje";
            }

            if (fondoBarraTituloVentana != null)
            {
                fondoBarraTituloVentana.color = exito
                    ? new Color(0.12f, 0.45f, 0.22f, 0.95f)
                    : new Color(0.55f, 0.15f, 0.15f, 0.95f);
            }

            if (iconoVentanaResultado != null)
            {
                iconoVentanaResultado.gameObject.SetActive(true);
                iconoVentanaResultado.sprite = exito ? spriteIconoDocumento : spriteIconoDibujo;
            }
        }

        private void CrearVentanaPopupSiNoExiste()
        {
            if (ventanaPopup != null) return;

            Transform contenedor = panelRaiz != null ? panelRaiz.transform : transform;

            GameObject modal = new GameObject("VentanaPopupResultado", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(contenedor, false);
            modal.layer = LayerMask.NameToLayer("UI");

            RectTransform rtModal = modal.GetComponent<RectTransform>();
            rtModal.anchorMin = new Vector2(0.5f, 0.5f);
            rtModal.anchorMax = new Vector2(0.5f, 0.5f);
            rtModal.pivot = new Vector2(0.5f, 0.5f);
            rtModal.sizeDelta = new Vector2(480, 240);

            Image imgModal = modal.GetComponent<Image>();
            imgModal.color = new Color(0.14f, 0.17f, 0.22f, 0.98f);

            // Barra de título
            GameObject objBarra = new GameObject("BarraTitulo", typeof(RectTransform), typeof(Image));
            objBarra.transform.SetParent(modal.transform, false);
            RectTransform rtBarra = objBarra.GetComponent<RectTransform>();
            rtBarra.anchorMin = new Vector2(0, 1);
            rtBarra.anchorMax = new Vector2(1, 1);
            rtBarra.pivot = new Vector2(0.5f, 1);
            rtBarra.sizeDelta = new Vector2(0, 36);
            rtBarra.anchoredPosition = Vector2.zero;

            fondoBarraTituloVentana = objBarra.GetComponent<Image>();
            fondoBarraTituloVentana.color = new Color(0.12f, 0.45f, 0.22f, 0.95f);

            // Texto título
            GameObject objTitulo = new GameObject("TextoTitulo", typeof(RectTransform), typeof(Text));
            objTitulo.transform.SetParent(objBarra.transform, false);
            RectTransform rtTitulo = objTitulo.GetComponent<RectTransform>();
            EstirarConMargenes(rtTitulo, 10, 0, 10, 0);

            textoTituloVentana = objTitulo.GetComponent<Text>();
            textoTituloVentana.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoTituloVentana.fontSize = 17;
            textoTituloVentana.fontStyle = FontStyle.Bold;
            textoTituloVentana.color = Color.white;
            textoTituloVentana.alignment = TextAnchor.MiddleLeft;

            // Icono resultado
            GameObject objIcono = new GameObject("IconoResultado", typeof(RectTransform), typeof(Image));
            objIcono.transform.SetParent(modal.transform, false);
            RectTransform rtIcono = objIcono.GetComponent<RectTransform>();
            rtIcono.anchorMin = new Vector2(0, 0.5f);
            rtIcono.anchorMax = new Vector2(0, 0.5f);
            rtIcono.pivot = new Vector2(0.5f, 0.5f);
            rtIcono.sizeDelta = new Vector2(64, 64);
            rtIcono.anchoredPosition = new Vector2(55, 10);
            iconoVentanaResultado = objIcono.GetComponent<Image>();

            // Texto mensaje
            GameObject objMensaje = new GameObject("TextoMensaje", typeof(RectTransform), typeof(Text));
            objMensaje.transform.SetParent(modal.transform, false);
            RectTransform rtMensaje = objMensaje.GetComponent<RectTransform>();
            rtMensaje.anchorMin = new Vector2(0, 0);
            rtMensaje.anchorMax = new Vector2(1, 1);
            rtMensaje.offsetMin = new Vector2(100, 60);
            rtMensaje.offsetMax = new Vector2(-20, -45);

            textoMensajeVentana = objMensaje.GetComponent<Text>();
            textoMensajeVentana.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoMensajeVentana.fontSize = 15;
            textoMensajeVentana.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            textoMensajeVentana.alignment = TextAnchor.MiddleLeft;

            // Botón Aceptar
            GameObject objBoton = new GameObject("BotonAceptar", typeof(RectTransform), typeof(Image), typeof(Button));
            objBoton.transform.SetParent(modal.transform, false);
            RectTransform rtBoton = objBoton.GetComponent<RectTransform>();
            rtBoton.anchorMin = new Vector2(0.5f, 0);
            rtBoton.anchorMax = new Vector2(0.5f, 0);
            rtBoton.pivot = new Vector2(0.5f, 0);
            rtBoton.sizeDelta = new Vector2(140, 36);
            rtBoton.anchoredPosition = new Vector2(0, 15);

            Image imgBoton = objBoton.GetComponent<Image>();
            imgBoton.color = new Color(0.25f, 0.35f, 0.5f, 1f);
            botonAceptarVentana = objBoton.GetComponent<Button>();
            botonAceptarVentana.onClick.AddListener(() =>
            {
                if (ventanaPopup != null) ventanaPopup.SetActive(false);
                Salir();
            });

            GameObject objTxtBtn = new GameObject("TextoBoton", typeof(RectTransform), typeof(Text));
            objTxtBtn.transform.SetParent(objBoton.transform, false);
            RectTransform rtTxtBtn = objTxtBtn.GetComponent<RectTransform>();
            ConstructorUI.Estirar(rtTxtBtn);
            Text txtBtn = objTxtBtn.GetComponent<Text>();
            txtBtn.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtBtn.fontSize = 14;
            txtBtn.fontStyle = FontStyle.Bold;
            txtBtn.color = Color.white;
            txtBtn.alignment = TextAnchor.MiddleCenter;
            txtBtn.text = "Aceptar";

            modal.SetActive(false);
            ventanaPopup = modal;
        }

        #endregion

        #region Generación UI Automática Si Es Necesario

        public void ConstruirUIAutomaticaSiEsNecesario()
        {
            if (rectTransformPapel != null && botonBirome != null) return;

            Transform contenedor = panelRaiz != null ? panelRaiz.transform : transform;

            // 1. Imagen de fondo de escritorio
            if (imagenFondoEscritorio == null)
            {
                GameObject objFondo = new GameObject("FondoEscritorio", typeof(RectTransform), typeof(Image));
                objFondo.transform.SetParent(contenedor, false);
                RectTransform rtFondo = objFondo.GetComponent<RectTransform>();
                ConstructorUI.Estirar(rtFondo);
                imagenFondoEscritorio = objFondo.GetComponent<Image>();
                imagenFondoEscritorio.sprite = spriteFondoEscritorio;
                imagenFondoEscritorio.color = Color.white;
            }

            // 2. Contenedor de la Hoja
            if (rectTransformPapel == null)
            {
                GameObject objPapel = new GameObject("PapelBlanco", typeof(RectTransform), typeof(Image));
                objPapel.transform.SetParent(contenedor, false);
                rectTransformPapel = objPapel.GetComponent<RectTransform>();
                rectTransformPapel.anchorMin = new Vector2(0.55f, 0.5f);
                rectTransformPapel.anchorMax = new Vector2(0.55f, 0.5f);
                rectTransformPapel.pivot = new Vector2(0.5f, 0.5f);
                rectTransformPapel.sizeDelta = new Vector2(440, 560);

                imagenPapelFondo = objPapel.GetComponent<Image>();
                imagenPapelFondo.sprite = spritePapelBlanco;
                imagenPapelFondo.color = Color.white;

                // RawImage sobre el papel para el revelado
                GameObject objRevelado = new GameObject("CapaRevelado", typeof(RectTransform), typeof(RawImage));
                objRevelado.transform.SetParent(objPapel.transform, false);
                RectTransform rtRev = objRevelado.GetComponent<RectTransform>();
                EstirarConMargenes(rtRev, 15, 15, 15, 15);
                rawImageRevelado = objRevelado.GetComponent<RawImage>();
            }

            // 3. Panel Lateral Izquierdo de Controles y Herramientas
            GameObject panelHerramientas = new GameObject("PanelHerramientas", typeof(RectTransform), typeof(Image));
            panelHerramientas.transform.SetParent(contenedor, false);
            RectTransform rtPH = panelHerramientas.GetComponent<RectTransform>();
            rtPH.anchorMin = new Vector2(0.04f, 0.12f);
            rtPH.anchorMax = new Vector2(0.30f, 0.88f);
            rtPH.offsetMin = Vector2.zero;
            rtPH.offsetMax = Vector2.zero;

            Image imgPH = panelHerramientas.GetComponent<Image>();
            imgPH.color = new Color(0.1f, 0.12f, 0.16f, 0.88f);

            // Título lateral
            GameObject objTit = new GameObject("Titulo", typeof(RectTransform), typeof(Text));
            objTit.transform.SetParent(panelHerramientas.transform, false);
            RectTransform rtTit = objTit.GetComponent<RectTransform>();
            rtTit.anchorMin = new Vector2(0, 1);
            rtTit.anchorMax = new Vector2(1, 1);
            rtTit.pivot = new Vector2(0.5f, 1);
            rtTit.sizeDelta = new Vector2(0, 40);
            rtTit.anchoredPosition = new Vector2(0, -10);
            Text txtTit = objTit.GetComponent<Text>();
            txtTit.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtTit.fontSize = 20;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.text = "Redactar Documento";
            txtTit.color = Color.white;

            // Botón Birome
            GameObject objBtnBio = CrearBotonHerramienta(panelHerramientas.transform, "BotonBirome", "Escribir con Birome", spriteBirome, new Vector2(0, -90), out marcoSeleccionBirome);
            botonBirome = objBtnBio.GetComponent<Button>();

            // Botón Crayola
            GameObject objBtnCra = CrearBotonHerramienta(panelHerramientas.transform, "BotonCrayola", "Dibujar con Crayolas", spriteCrayola, new Vector2(0, -170), out marcoSeleccionCrayola);
            botonCrayola = objBtnCra.GetComponent<Button>();

            // Texto herramienta activa
            GameObject objTxtHer = new GameObject("TextoHerramientaActiva", typeof(RectTransform), typeof(Text));
            objTxtHer.transform.SetParent(panelHerramientas.transform, false);
            RectTransform rtTxtHer = objTxtHer.GetComponent<RectTransform>();
            rtTxtHer.anchorMin = new Vector2(0, 0.5f);
            rtTxtHer.anchorMax = new Vector2(1, 0.5f);
            rtTxtHer.pivot = new Vector2(0.5f, 0.5f);
            rtTxtHer.sizeDelta = new Vector2(-20, 30);
            rtTxtHer.anchoredPosition = new Vector2(0, -10);
            textoHerramientaActiva = objTxtHer.GetComponent<Text>();
            textoHerramientaActiva.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoHerramientaActiva.fontSize = 14;
            textoHerramientaActiva.alignment = TextAnchor.MiddleCenter;
            textoHerramientaActiva.color = Color.white;

            // Barra de progreso
            GameObject objBarraProg = new GameObject("BarraProgreso", typeof(RectTransform), typeof(Slider));
            objBarraProg.transform.SetParent(panelHerramientas.transform, false);
            RectTransform rtBP = objBarraProg.GetComponent<RectTransform>();
            rtBP.anchorMin = new Vector2(0.1f, 0.35f);
            rtBP.anchorMax = new Vector2(0.9f, 0.35f);
            rtBP.pivot = new Vector2(0.5f, 0.5f);
            rtBP.sizeDelta = new Vector2(0, 20);

            barraProgreso = objBarraProg.GetComponent<Slider>();
            barraProgreso.minValue = 0f;
            barraProgreso.maxValue = 1f;

            // Fondo barra
            GameObject bgBarra = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgBarra.transform.SetParent(objBarraProg.transform, false);
            ConstructorUI.Estirar(bgBarra.GetComponent<RectTransform>());
            bgBarra.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // Fill area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(objBarraProg.transform, false);
            ConstructorUI.Estirar(fillArea.GetComponent<RectTransform>());

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            ConstructorUI.Estirar(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.4f, 1f);
            barraProgreso.fillRect = fill.GetComponent<RectTransform>();

            // Texto porcentaje progreso
            GameObject objTxtProg = new GameObject("TextoProgreso", typeof(RectTransform), typeof(Text));
            objTxtProg.transform.SetParent(panelHerramientas.transform, false);
            RectTransform rtTP = objTxtProg.GetComponent<RectTransform>();
            rtTP.anchorMin = new Vector2(0.1f, 0.30f);
            rtTP.anchorMax = new Vector2(0.9f, 0.30f);
            rtTP.pivot = new Vector2(0.5f, 0.5f);
            rtTP.sizeDelta = new Vector2(0, 24);
            textoProgreso = objTxtProg.GetComponent<Text>();
            textoProgreso.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoProgreso.fontSize = 14;
            textoProgreso.alignment = TextAnchor.MiddleCenter;
            textoProgreso.text = "0%";
            textoProgreso.color = Color.white;

            // Texto instrucciones
            GameObject objInst = new GameObject("TextoInstrucciones", typeof(RectTransform), typeof(Text));
            objInst.transform.SetParent(panelHerramientas.transform, false);
            RectTransform rtInst = objInst.GetComponent<RectTransform>();
            rtInst.anchorMin = new Vector2(0.08f, 0.16f);
            rtInst.anchorMax = new Vector2(0.92f, 0.28f);
            rtInst.offsetMin = Vector2.zero;
            rtInst.offsetMax = Vector2.zero;
            textoInstrucciones = objInst.GetComponent<Text>();
            textoInstrucciones.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoInstrucciones.fontSize = 13;
            textoInstrucciones.alignment = TextAnchor.MiddleCenter;
            textoInstrucciones.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            textoInstrucciones.text = "Selecciona una herramienta y manten presionado sobre la hoja para revelar.";

            botonSalir = ConstructorUI.CrearBotonSalir(
                panelHerramientas.transform,
                "Salir (Esc)",
                new Vector2(0.1f, 0.04f),
                new Vector2(0.9f, 0.04f),
                new Vector2(0.5f, 0f),
                Vector2.zero,
                new Vector2(0f, 38f));

            // 4. Cursor flotante de herramienta centrado
            GameObject objCursor = new GameObject("CursorHerramienta", typeof(RectTransform), typeof(Image));
            objCursor.transform.SetParent(contenedor, false);
            rectCursor = objCursor.GetComponent<RectTransform>();
            rectCursor.sizeDelta = new Vector2(36, 36);
            rectCursor.pivot = new Vector2(0.5f, 0.5f);
            cursorHerramienta = objCursor.GetComponent<Image>();
            cursorHerramienta.raycastTarget = false;
            objCursor.SetActive(false);
        }

        private GameObject CrearBotonHerramienta(Transform padre, string nombre, string texto, Sprite icono, Vector2 posAnchored, out Image marco)
        {
            GameObject botonObj = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
            botonObj.transform.SetParent(padre, false);
            RectTransform rt = botonObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 1);
            rt.anchorMax = new Vector2(0.9f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, 56);
            rt.anchoredPosition = posAnchored;

            Image img = botonObj.GetComponent<Image>();
            img.color = new Color(0.2f, 0.24f, 0.3f, 1f);

            // Marco de selección activa
            GameObject objMarco = new GameObject("MarcoSeleccion", typeof(RectTransform), typeof(Image));
            objMarco.transform.SetParent(botonObj.transform, false);
            RectTransform rtMarco = objMarco.GetComponent<RectTransform>();
            EstirarConMargenes(rtMarco, -3, -3, -3, -3);
            marco = objMarco.GetComponent<Image>();
            marco.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            marco.enabled = false;

            // Icono
            if (icono != null)
            {
                GameObject objIcono = new GameObject("Icono", typeof(RectTransform), typeof(Image));
                objIcono.transform.SetParent(botonObj.transform, false);
                RectTransform rtIco = objIcono.GetComponent<RectTransform>();
                rtIco.anchorMin = new Vector2(0, 0.5f);
                rtIco.anchorMax = new Vector2(0, 0.5f);
                rtIco.pivot = new Vector2(0.5f, 0.5f);
                rtIco.sizeDelta = new Vector2(32, 32);
                rtIco.anchoredPosition = new Vector2(25, 0);

                Image imgIco = objIcono.GetComponent<Image>();
                imgIco.sprite = icono;
            }

            // Texto botón
            GameObject objTxt = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            objTxt.transform.SetParent(botonObj.transform, false);
            RectTransform rtTxt = objTxt.GetComponent<RectTransform>();
            EstirarConMargenes(rtTxt, 48, 4, 8, 4);

            Text txt = objTxt.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.white;
            txt.text = texto;

            return botonObj;
        }

        private static void EstirarConMargenes(RectTransform rect, float izquierda, float abajo, float derecha, float arriba)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(izquierda, abajo);
            rect.offsetMax = new Vector2(-derecha, -arriba);
        }

        #endregion
    }
}
