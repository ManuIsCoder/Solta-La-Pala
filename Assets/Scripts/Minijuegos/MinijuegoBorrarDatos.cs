using System.Collections;
using System.Collections.Generic;
using SoltaLaPala.Menus;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Minijuegos
{
    /// <summary>
    /// Minijuego: Borrar datos de la computadora.
    ///
    /// Objetivo legítimo:
    ///   Borrar datos indebidos que no pertenecen a la oficina (fotos de viaje, cartas personales, etc.)
    ///   arrastrándolos a la Papelera de reciclaje.
    ///
    /// Sabotaje (arruinar la tarea):
    ///   Borrar archivos críticos del trabajo (Base de datos SQL, Reporte de ventas XLSX o Video publicitario MP4).
    /// </summary>
    public class MinijuegoBorrarDatos : MinijuegoBase
    {
        [System.Serializable]
        public class DatosArchivo
        {
            public string nombreArchivo;
            public Sprite icono;
            [Tooltip("Si es true, este archivo pertenece a la empresa y borrarlo arruina la tarea (sabotaje)")]
            public bool esImportante;

            public DatosArchivo(string nombre, Sprite spriteIcono, bool importante)
            {
                nombreArchivo = nombre;
                icono = spriteIcono;
                esImportante = importante;
            }
        }

        [Header("Fondo y Estética")]
        [Tooltip("Sprite de 'fondo computadora'")]
        public Sprite spriteFondoComputadora;
        public Image imagenFondo;

        [Header("Papelera")]
        public RectTransform zonaPapelera;
        public Image imagenPapelera;
        public Sprite spritePapelera;

        [Header("Archivos")]
        [Tooltip("Prefab o plantilla para cada icono de archivo arrastrable")]
        public GameObject prefabIconoArchivo;
        [Tooltip("Área del escritorio donde se distribuyen los iconos")]
        public RectTransform contenedorArchivos;
        public List<DatosArchivo> listaArchivos = new List<DatosArchivo>();

        [Header("Controles y Textos")]
        public Button botonListo;
        public Button botonSalir;
        public Text textoInstrucciones;
        public Text textoContador;

        [Header("UI - Ventana de Estado Final")]
        public GameObject ventanaPopup;
        public Text textoTituloVentana;
        public Text textoMensajeVentana;
        public Button botonAceptarVentana;
        public Image fondoBarraTituloVentana;

        [Header("Ajustes")]
        [Tooltip("Permite jugar sin inventario ni restricciones")]
        public bool modoPrueba = false;

        private int archivosIndebidosTotales;
        private int archivosIndebidosBorrados;
        private bool sabotajeCometido;
        private List<ArchivoArrastrable> instanciasArchivos = new List<ArchivoArrastrable>();
        private Coroutine corrutinaPapelera;
        private Vector3 escalaOriginalPapelera = Vector3.one;

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

            if (zonaPapelera != null)
            {
                escalaOriginalPapelera = zonaPapelera.localScale;
            }

            if (botonListo != null)
            {
                botonListo.onClick.RemoveAllListeners();
                botonListo.onClick.AddListener(OnBotonListoPresionado);
            }

            if (botonSalir != null)
            {
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
            }

            // Si el minijuego ya fue completado o malogrado antes, no se reinicia:
            // se muestra tal como quedo con su ventana de resultado.
            if (minijuegoTerminado)
            {
                BloquearInteraccionesArchivos();
                MostrarVentanaFinal(mensajeVentanaFinal, resultadoExito);
                ActualizarTextos();
                return;
            }

            // Si es la primera vez que se interactua, generamos los archivos
            if (!yaInicializado)
            {
                yaInicializado = true;
                sabotajeCometido = false;
                archivosIndebidosBorrados = 0;
                GenerarArchivosEnEscritorio();
            }

            if (ventanaPopup != null)
            {
                ventanaPopup.SetActive(false);
            }

            ActualizarTextos();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Salir();
            }
        }

        protected override void AlSalir()
        {
            // NO destruimos instanciasArchivos para conservar los archivos en el escritorio
            if (corrutinaPapelera != null)
            {
                StopCoroutine(corrutinaPapelera);
                corrutinaPapelera = null;
            }

            if (zonaPapelera != null)
            {
                zonaPapelera.localScale = escalaOriginalPapelera;
            }
        }

        private void GenerarArchivosEnEscritorio()
        {
            LimpiarInstanciasArchivos();

            if (listaArchivos == null || listaArchivos.Count == 0)
            {
                CargarArchivosPorDefecto();
            }

            archivosIndebidosTotales = 0;
            foreach (var archivo in listaArchivos)
            {
                if (!archivo.esImportante)
                {
                    archivosIndebidosTotales++;
                }
            }

            if (contenedorArchivos == null || listaArchivos == null) return;

            // Separamos la distribución:
            // 1. Archivos indebidos (a borrar): Arriba a la izquierda (desplazados a la derecha).
            // 2. Archivos de empresa (no borrar): Abajo a la izquierda (desplazados a la derecha).
            float espacioEntreIconos = 185f;
            float margenIzquierdo = 200f;

            int contadorBorrar = 0;
            int contadorEmpresa = 0;

            for (int i = 0; i < listaArchivos.Count; i++)
            {
                var dato = listaArchivos[i];
                GameObject nuevoObj;

                if (prefabIconoArchivo != null)
                {
                    nuevoObj = Instantiate(prefabIconoArchivo, contenedorArchivos);
                }
                else
                {
                    nuevoObj = CrearIconoGenerico(contenedorArchivos);
                }

                RectTransform rt = nuevoObj.GetComponent<RectTransform>();

                if (!dato.esImportante)
                {
                    // Arriba a la izquierda
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(margenIzquierdo + (contadorBorrar * espacioEntreIconos), -125f);
                    contadorBorrar++;
                }
                else
                {
                    // Abajo a la izquierda
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(0f, 0f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(margenIzquierdo + (contadorEmpresa * espacioEntreIconos), 125f);
                    contadorEmpresa++;
                }

                ArchivoArrastrable arrastrable = nuevoObj.GetComponent<ArchivoArrastrable>();
                if (arrastrable != null)
                {
                    arrastrable.Configurar(dato, this);
                    arrastrable.GuardarPosicionActualComoOriginal();
                    instanciasArchivos.Add(arrastrable);
                }
            }
        }

        public bool EstaSobrePapelera(Vector2 posicionPantalla, Camera camara)
        {
            if (zonaPapelera == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(zonaPapelera, posicionPantalla, camara);
        }

        public void NotificarHoverPapelera(bool hover)
        {
            if (zonaPapelera == null) return;

            Vector3 escalaObjetivo = hover ? (escalaOriginalPapelera * 1.18f) : escalaOriginalPapelera;
            zonaPapelera.localScale = Vector3.Lerp(zonaPapelera.localScale, escalaObjetivo, 0.4f);
        }

        public void ProcesarArchivoEliminado(ArchivoArrastrable archivo)
        {
            if (archivo == null || archivo.Datos == null) return;

            if (corrutinaPapelera != null) StopCoroutine(corrutinaPapelera);
            corrutinaPapelera = StartCoroutine(AnimarSacudidaPapelera());

            instanciasArchivos.Remove(archivo);

            // Si borra un solo dato "de la empresa": MALOGRA el minijuego de inmediato
            if (archivo.Datos.esImportante)
            {
                sabotajeCometido = true;
                minijuegoTerminado = true;
                resultadoExito = false;
                mensajeVentanaFinal = "Base de datos de empresa perjudicada";

                BloquearInteraccionesArchivos();

                if (textoInstrucciones != null)
                {
                    textoInstrucciones.text = "<color=#FF4444><b>¡MALOGRASTE LA TAREA!</b> Borraste un archivo de la empresa.</color>";
                }

                if (textoContador != null)
                {
                    textoContador.text = "<color=#FF4444>¡TAREA FALLIDA!</color>";
                }

                // Malograr aumenta la sospecha
                if (EstadoPartida.Instancia != null)
                {
                    EstadoPartida.Instancia.RegistrarSabotaje(0, 15);
                }

                Debug.LogWarning("[MinijuegoBorrarDatos] Minijuego malogrado: se borro un archivo de la empresa. Sospecha aumentada.");
                MostrarVentanaFinal(mensajeVentanaFinal, resultadoExito);
                return;
            }

            // Archivo indebido eliminado
            archivosIndebidosBorrados++;
            ActualizarTextos();

            // Si borra todos los archivos que debia borrar: GANA (reduce la sospecha)
            if (archivosIndebidosBorrados >= archivosIndebidosTotales && !sabotajeCometido)
            {
                minijuegoTerminado = true;
                resultadoExito = true;
                mensajeVentanaFinal = "Limpieza de computadora terminada";

                BloquearInteraccionesArchivos();

                if (textoInstrucciones != null)
                {
                    textoInstrucciones.text = "<color=#55FF88><b>¡GANASTE!</b> Archivos indebidos eliminados. Sospecha reducida.</color>";
                }

                if (textoContador != null)
                {
                    textoContador.text = "<color=#55FF88>¡COMPLETADO!</color>";
                }

                // Ganar resta a la barra de sospecha
                if (EstadoPartida.Instancia != null)
                {
                    EstadoPartida.Instancia.ReducirSospecha(15);
                }

                Debug.Log("[MinijuegoBorrarDatos] ¡Minijuego ganado! Se limpio la PC y se redujo la sospecha.");
                MostrarVentanaFinal(mensajeVentanaFinal, resultadoExito);
            }
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
            rtVentana.sizeDelta = new Vector2(500f, 220f);

            Image imgFondo = ventanaPopup.GetComponent<Image>();
            imgFondo.color = new Color(0.13f, 0.15f, 0.20f, 0.98f);

            // Barra de Titulo
            GameObject objBarra = new GameObject("BarraTitulo", typeof(RectTransform), typeof(Image));
            objBarra.transform.SetParent(ventanaPopup.transform, false);
            RectTransform rtBarra = objBarra.GetComponent<RectTransform>();
            rtBarra.anchorMin = new Vector2(0f, 1f);
            rtBarra.anchorMax = new Vector2(1f, 1f);
            rtBarra.pivot = new Vector2(0.5f, 1f);
            rtBarra.anchoredPosition = Vector2.zero;
            rtBarra.sizeDelta = new Vector2(0f, 38f);
            fondoBarraTituloVentana = objBarra.GetComponent<Image>();
            fondoBarraTituloVentana.color = new Color(0.18f, 0.22f, 0.32f, 1f);

            // Texto Titulo
            GameObject objTxtTit = new GameObject("Titulo", typeof(RectTransform), typeof(Text));
            objTxtTit.transform.SetParent(objBarra.transform, false);
            RectTransform rtTit = objTxtTit.GetComponent<RectTransform>();
            rtTit.anchorMin = Vector2.zero;
            rtTit.anchorMax = Vector2.one;
            rtTit.offsetMin = new Vector2(15f, 0f);
            rtTit.offsetMax = new Vector2(-15f, 0f);
            textoTituloVentana = objTxtTit.GetComponent<Text>();
            textoTituloVentana.font = ConstructorUI.Fuente;
            textoTituloVentana.fontSize = 15;
            textoTituloVentana.alignment = TextAnchor.MiddleLeft;
            textoTituloVentana.color = Color.white;
            textoTituloVentana.text = "Aviso del Sistema";

            // Texto Mensaje Central
            GameObject objTxtMsg = new GameObject("Mensaje", typeof(RectTransform), typeof(Text));
            objTxtMsg.transform.SetParent(ventanaPopup.transform, false);
            RectTransform rtMsg = objTxtMsg.GetComponent<RectTransform>();
            rtMsg.anchorMin = new Vector2(0f, 0.3f);
            rtMsg.anchorMax = new Vector2(1f, 0.85f);
            rtMsg.offsetMin = new Vector2(20f, 0f);
            rtMsg.offsetMax = new Vector2(-20f, 0f);
            textoMensajeVentana = objTxtMsg.GetComponent<Text>();
            textoMensajeVentana.font = ConstructorUI.Fuente;
            textoMensajeVentana.fontSize = 18;
            textoMensajeVentana.fontStyle = FontStyle.Bold;
            textoMensajeVentana.alignment = TextAnchor.MiddleCenter;
            textoMensajeVentana.color = Color.white;

            // Boton Aceptar
            GameObject objBtnAceptar = new GameObject("BotonAceptar", typeof(RectTransform), typeof(Image), typeof(Button));
            objBtnAceptar.transform.SetParent(ventanaPopup.transform, false);
            RectTransform rtBtn = objBtnAceptar.GetComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(0.5f, 0f);
            rtBtn.anchorMax = new Vector2(0.5f, 0f);
            rtBtn.pivot = new Vector2(0.5f, 0f);
            rtBtn.anchoredPosition = new Vector2(0f, 20f);
            rtBtn.sizeDelta = new Vector2(140f, 38f);

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
            tBtn.alignment = TextAnchor.MiddleCenter;
            tBtn.color = Color.white;
            tBtn.text = "Aceptar";

            ventanaPopup.SetActive(false);
        }

        private void BloquearInteraccionesArchivos()
        {
            foreach (var inst in instanciasArchivos)
            {
                if (inst != null)
                {
                    var cg = inst.GetComponent<CanvasGroup>();
                    if (cg != null) cg.blocksRaycasts = false;
                }
            }
        }

        private IEnumerator AnimarSacudidaPapelera()
        {
            if (zonaPapelera == null) yield break;

            Vector3 posOriginal = zonaPapelera.localPosition;
            float duracion = 0.35f;
            float tiempo = 0f;

            zonaPapelera.localScale = escalaOriginalPapelera * 1.25f;

            while (tiempo < duracion)
            {
                tiempo += Time.deltaTime;
                float rot = Mathf.Sin(tiempo * 40f) * 12f * (1f - (tiempo / duracion));
                zonaPapelera.localEulerAngles = new Vector3(0, 0, rot);
                yield return null;
            }

            zonaPapelera.localEulerAngles = Vector3.zero;
            zonaPapelera.localScale = escalaOriginalPapelera;
            zonaPapelera.localPosition = posOriginal;
            corrutinaPapelera = null;
        }

        public void OnBotonListoPresionado()
        {
            Completar();
        }

        private void Completar()
        {
            int impacto = sabotajeCometido ? 3 : 1;
            int sospecha = sabotajeCometido ? 10 : 0;

            if (EstadoPartida.Instancia != null)
            {
                EstadoPartida.Instancia.RegistrarSabotaje(impacto, sospecha);
            }

            Debug.Log($"[MinijuegoBorrarDatos] Tarea completada. Sabotaje: {sabotajeCometido}. Impacto: {impacto}, Sospecha: {sospecha}");

            Salir();
        }

        private void ActualizarTextos()
        {
            if (textoContador != null)
            {
                int restantes = Mathf.Max(0, archivosIndebidosTotales - archivosIndebidosBorrados);
                textoContador.text = $"Archivos indebidos restantes: {restantes}";
            }

            if (textoInstrucciones != null && !sabotajeCometido && archivosIndebidosBorrados == 0)
            {
                textoInstrucciones.text = "Arrastra a la Papelera los archivos personales sin borrar los de la empresa.";
            }
        }

        private void LimpiarInstanciasArchivos()
        {
            for (int i = instanciasArchivos.Count - 1; i >= 0; i--)
            {
                if (instanciasArchivos[i] != null)
                {
                    Destroy(instanciasArchivos[i].gameObject);
                }
            }
            instanciasArchivos.Clear();
        }

        private GameObject CrearIconoGenerico(Transform padre)
        {
            GameObject iconoObj = new GameObject("IconoArchivo", typeof(RectTransform), typeof(CanvasGroup));
            iconoObj.transform.SetParent(padre, false);
            RectTransform rt = iconoObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(130f, 130f);

            // Imagen del icono (mas grande y limpia, sin texto)
            GameObject imgObj = new GameObject("Imagen", typeof(RectTransform), typeof(Image));
            imgObj.transform.SetParent(iconoObj.transform, false);
            RectTransform imgRt = imgObj.GetComponent<RectTransform>();
            imgRt.sizeDelta = new Vector2(120f, 120f);
            imgRt.anchoredPosition = Vector2.zero;
            Image img = imgObj.GetComponent<Image>();
            img.preserveAspect = true;

            ArchivoArrastrable arr = iconoObj.AddComponent<ArchivoArrastrable>();
            arr.imagenIcono = img;
            arr.textoEtiqueta = null;

            return iconoObj;
        }

        private void ConstruirUIAutomaticaSiEsNecesario()
        {
            // Agrandar la pantalla / fondo un 15%
            RectTransform rtPanel = panelRaiz != null ? panelRaiz.GetComponent<RectTransform>() : GetComponent<RectTransform>();
            if (rtPanel != null)
            {
                // Si tiene tamaño fijo (ej. 1000x650), lo escalamos un 15% -> 1150x748
                if (rtPanel.anchorMin == rtPanel.anchorMax)
                {
                    rtPanel.sizeDelta = new Vector2(1150f, 748f);
                }
            }

            if (imagenFondo == null)
            {
                Image[] imagenes = GetComponentsInChildren<Image>(true);
                foreach (var img in imagenes)
                {
                    if (img.gameObject.name.ToLower().Contains("fondo"))
                    {
                        imagenFondo = img;
                        break;
                    }
                }
            }

            if (imagenFondo != null && spriteFondoComputadora != null)
            {
                imagenFondo.sprite = spriteFondoComputadora;
            }

            if (zonaPapelera == null)
            {
                Transform papeleraTr = transform.Find("Papelera") ?? transform.Find("ZonaPapelera");
                if (papeleraTr != null)
                {
                    zonaPapelera = papeleraTr.GetComponent<RectTransform>();
                    imagenPapelera = papeleraTr.GetComponent<Image>();
                }
            }

            // Aseguramos que la papelera este claramente dentro de la pantalla y desplazada 1.5 veces su ancho a la izquierda
            if (zonaPapelera != null)
            {
                zonaPapelera.anchorMin = new Vector2(1f, 0.5f);
                zonaPapelera.anchorMax = new Vector2(1f, 0.5f);
                zonaPapelera.pivot = new Vector2(0.5f, 0.5f);
                zonaPapelera.anchoredPosition = new Vector2(-365f, 0f);
                zonaPapelera.sizeDelta = new Vector2(145f, 145f);
            }

            // Mejorar estilo del Boton Salir
            if (botonSalir != null)
            {
                RectTransform rtSalir = botonSalir.GetComponent<RectTransform>();
                if (rtSalir != null)
                {
                    rtSalir.anchorMin = new Vector2(1f, 1f);
                    rtSalir.anchorMax = new Vector2(1f, 1f);
                    rtSalir.pivot = new Vector2(1f, 1f);
                    rtSalir.anchoredPosition = new Vector2(-25f, -25f);
                    rtSalir.sizeDelta = new Vector2(110f, 40f);
                }

                Image imgSalir = botonSalir.GetComponent<Image>();
                if (imgSalir != null)
                {
                    imgSalir.color = new Color(0.78f, 0.20f, 0.20f, 0.95f);
                }

                ColorBlock cb = botonSalir.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
                cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                botonSalir.colors = cb;

                Text txtSalir = botonSalir.GetComponentInChildren<Text>();
                if (txtSalir != null)
                {
                    txtSalir.text = "✕ Salir";
                    txtSalir.fontSize = 15;
                    txtSalir.alignment = TextAnchor.MiddleCenter;
                    txtSalir.color = Color.white;
                    txtSalir.font = ConstructorUI.Fuente;
                }
            }

            if (imagenPapelera != null && spritePapelera != null)
            {
                imagenPapelera.sprite = spritePapelera;
            }

            if (contenedorArchivos == null)
            {
                Transform cont = transform.Find("AreaArchivos") ?? transform.Find("Escritorio") ?? transform.Find("AreaEscritorio");
                if (cont != null)
                {
                    contenedorArchivos = cont.GetComponent<RectTransform>();
                }
            }

            if (spriteFondoComputadora == null)
            {
                spriteFondoComputadora = Resources.Load<Sprite>("Sprites/Minijuegos/fondo computadora");
                if (imagenFondo != null && spriteFondoComputadora != null)
                {
                    imagenFondo.sprite = spriteFondoComputadora;
                }
            }

            if (spritePapelera == null)
            {
                spritePapelera = Resources.Load<Sprite>("Sprites/Minijuegos/logo_papelera");
                if (imagenPapelera != null && spritePapelera != null)
                {
                    imagenPapelera.sprite = spritePapelera;
                }
            }
        }

        private void CargarArchivosPorDefecto()
        {
            Sprite sCarta = Resources.Load<Sprite>("Sprites/Minijuegos/icono_carta_a_mama_txt");
            Sprite sFoto = Resources.Load<Sprite>("Sprites/Minijuegos/icono_gato_foto_png");
            Sprite sSql = Resources.Load<Sprite>("Sprites/Minijuegos/logo_sql");
            Sprite sXlsx = Resources.Load<Sprite>("Sprites/Minijuegos/icono_reporteEmpresa2026_xlsx");
            Sprite sMp4 = Resources.Load<Sprite>("Sprites/Minijuegos/logo_mp4");

            listaArchivos = new List<DatosArchivo>
            {
                new DatosArchivo("CartaDeAmor.txt", sCarta, false),
                new DatosArchivo("Fotos_Viaje.png", sFoto, false),
                new DatosArchivo("BaseDeDatos.sql", sSql, true),
                new DatosArchivo("Reporte2026.xlsx", sXlsx, true),
                new DatosArchivo("VideoComercial.mp4", sMp4, true)
            };
        }
    }
}
