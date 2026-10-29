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

        [Header("Ajustes")]
        [Tooltip("Permite jugar sin inventario ni restricciones")]
        public bool modoPrueba = false;

        private int archivosIndebidosTotales;
        private int archivosIndebidosBorrados;
        private bool sabotajeCometido;
        private List<ArchivoArrastrable> instanciasArchivos = new List<ArchivoArrastrable>();
        private Coroutine corrutinaPapelera;
        private Vector3 escalaOriginalPapelera = Vector3.one;

        protected override void AlIniciar()
        {
            sabotajeCometido = false;
            archivosIndebidosBorrados = 0;

            if (panelRaiz == null)
            {
                panelRaiz = gameObject;
            }

            ConstruirUIAutomaticaSiEsNecesario();

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

            GenerarArchivosEnEscritorio();
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
            LimpiarInstanciasArchivos();

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

            // Disposición en cuadrícula sobre el escritorio virtual
            int columnas = 4;
            float espaciadoX = 140f;
            float espaciadoY = 130f;
            float inicioX = -220f;
            float inicioY = 110f;

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
                int col = i % columnas;
                int fila = i / columnas;
                rt.anchoredPosition = new Vector2(inicioX + (col * espaciadoX), inicioY - (fila * espaciadoY));

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

            if (archivo.Datos.esImportante)
            {
                sabotajeCometido = true;
                if (textoInstrucciones != null)
                {
                    textoInstrucciones.text = $"<color=#FF5555>¡Eliminaste '{archivo.Datos.nombreArchivo}'! (Archivo de la empresa)</color>";
                }
            }
            else
            {
                archivosIndebidosBorrados++;
                if (textoInstrucciones != null)
                {
                    textoInstrucciones.text = $"<color=#55FF88>Eliminado: {archivo.Datos.nombreArchivo}</color>";
                }
            }

            instanciasArchivos.Remove(archivo);
            ActualizarTextos();

            // Si se eliminaron todos los indebidos, se puede completar o habilitar cierre
            if (archivosIndebidosBorrados >= archivosIndebidosTotales)
            {
                if (botonListo != null)
                {
                    botonListo.interactable = true;
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
            rt.sizeDelta = new Vector2(100f, 100f);

            // Imagen del icono
            GameObject imgObj = new GameObject("Imagen", typeof(RectTransform), typeof(Image));
            imgObj.transform.SetParent(iconoObj.transform, false);
            RectTransform imgRt = imgObj.GetComponent<RectTransform>();
            imgRt.sizeDelta = new Vector2(64f, 64f);
            imgRt.anchoredPosition = new Vector2(0f, 14f);
            Image img = imgObj.GetComponent<Image>();
            img.preserveAspect = true;

            // Texto etiqueta
            GameObject txtObj = new GameObject("Etiqueta", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(iconoObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.sizeDelta = new Vector2(110f, 32f);
            txtRt.anchoredPosition = new Vector2(0f, -32f);
            Text txt = txtObj.GetComponent<Text>();
            txt.font = ConstructorUI.Fuente;
            txt.fontSize = 12;
            txt.alignment = TextAnchor.UpperCenter;
            txt.color = Color.white;

            ArchivoArrastrable arr = iconoObj.AddComponent<ArchivoArrastrable>();
            arr.imagenIcono = img;
            arr.textoEtiqueta = txt;

            return iconoObj;
        }

        private void ConstruirUIAutomaticaSiEsNecesario()
        {
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
