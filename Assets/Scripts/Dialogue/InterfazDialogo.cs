using System;
using System.Collections;
using System.Collections.Generic;
using SoltaLaPala.Menus;
using UnityEngine;
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
        public int tamanoFuenteOpciones = 22;
        public float altoOpcion = 46f;
        public Color colorOpcion = new Color(0.10f, 0.10f, 0.16f, 0.95f);
        public Color colorOpcionResaltada = new Color(0.24f, 0.24f, 0.36f, 1f);
        public Color colorOpcionPulsada = new Color(0.34f, 0.34f, 0.50f, 1f);

        private readonly List<GameObject> botonesOpciones = new List<GameObject>();

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
        // indice (desde 0) de la que se toco con el mouse; los numeros del
        // teclado los lee GestorDialogos.
        public void MostrarOpciones(IList<string> textos, Action<int> alElegir)
        {
            OcultarOpciones();
            AsegurarContenedorOpciones();
            ConstructorUI.AsegurarEventSystem();

            for (int i = 0; i < textos.Count; i++)
            {
                int indice = i;
                botonesOpciones.Add(CrearBotonOpcion($"{i + 1}. {textos[i]}", () => alElegir?.Invoke(indice)));
            }

            contenedorOpciones.gameObject.SetActive(true);

            // Mientras hay que elegir, ESPACIO no avanza: no se ofrece.
            if (indicadorContinuar != null) indicadorContinuar.SetActive(false);
        }

        public void OcultarOpciones()
        {
            foreach (GameObject boton in botonesOpciones)
            {
                Destroy(boton);
            }
            botonesOpciones.Clear();

            if (contenedorOpciones != null) contenedorOpciones.gameObject.SetActive(false);
        }

        // Columna pegada al borde de arriba del cuadro, del mismo ancho, que
        // crece hacia arriba segun cuantas respuestas haya.
        private void AsegurarContenedorOpciones()
        {
            if (contenedorOpciones != null)
            {
                return;
            }

            GameObject objeto = new GameObject("Opciones", typeof(RectTransform));
            objeto.transform.SetParent(panel.transform, false);

            contenedorOpciones = (RectTransform)objeto.transform;
            contenedorOpciones.anchorMin = new Vector2(0f, 1f);
            contenedorOpciones.anchorMax = new Vector2(1f, 1f);
            contenedorOpciones.pivot = new Vector2(0.5f, 0f);
            contenedorOpciones.anchoredPosition = new Vector2(0f, 10f);
            contenedorOpciones.sizeDelta = Vector2.zero;

            VerticalLayoutGroup columna = objeto.AddComponent<VerticalLayoutGroup>();
            columna.spacing = 6f;
            columna.childControlWidth = true;
            columna.childControlHeight = true;
            columna.childForceExpandWidth = true;
            columna.childForceExpandHeight = false;

            objeto.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private GameObject CrearBotonOpcion(string texto, Action alPulsar)
        {
            GameObject objeto = new GameObject("Opcion", typeof(RectTransform));
            objeto.transform.SetParent(contenedorOpciones, false);

            objeto.AddComponent<LayoutElement>().preferredHeight = altoOpcion;

            Image fondo = objeto.AddComponent<Image>();
            fondo.color = Color.white;

            // Los colores van en el ColorBlock y no en la Image: el tinte multiplica,
            // asi que con la Image en blanco cada estado se ve con su color exacto.
            Button boton = objeto.AddComponent<Button>();
            boton.targetGraphic = fondo;
            ColorBlock colores = ColorBlock.defaultColorBlock;
            colores.normalColor = colorOpcion;
            colores.highlightedColor = colorOpcionResaltada;
            colores.pressedColor = colorOpcionPulsada;
            colores.selectedColor = colorOpcion;
            colores.fadeDuration = 0.05f;
            boton.colors = colores;
            boton.onClick.AddListener(() => alPulsar());

            GameObject objetoTexto = new GameObject("Texto", typeof(RectTransform));
            objetoTexto.transform.SetParent(objeto.transform, false);

            RectTransform rectTexto = (RectTransform)objetoTexto.transform;
            rectTexto.anchorMin = Vector2.zero;
            rectTexto.anchorMax = Vector2.one;
            rectTexto.offsetMin = new Vector2(20f, 0f);
            rectTexto.offsetMax = new Vector2(-20f, 0f);

            Text etiqueta = objetoTexto.AddComponent<Text>();
            etiqueta.text = texto;
            etiqueta.font = textoDialogo != null ? textoDialogo.font : ConstructorUI.Fuente;
            etiqueta.fontSize = tamanoFuenteOpciones;
            etiqueta.alignment = TextAnchor.MiddleLeft;
            etiqueta.color = Color.white;
            etiqueta.raycastTarget = false;

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
    }
}
