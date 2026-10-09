using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Dialogue
{
    // Cuadro de dialogo: rectangulo con el nombre del NPC arriba
    // y el texto de la frase escribiendose palabra a palabra.
    public class InterfazDialogo : MonoBehaviour
    {
        [Header("Referencias")]
        public GameObject panel;
        public Text textoNombre;
        public Text textoDialogo;
        [Tooltip("Flechita o icono que indica que se puede pasar con espacio.")]
        public GameObject indicadorContinuar;

        [Header("Ajustes")]
        [Tooltip("Palabras que aparecen por segundo. Un valor mas alto escribe mas rapido.")]
        public float palabrasPorSegundo = 5f;

        private Coroutine rutinaEscritura;
        private string lineaActualCompleta = "";

        // True mientras el texto se esta escribiendo palabra a palabra.
        public bool Escribiendo { get; private set; }

        // Se dispara al empezar a escribir una linea. Parametro: segundos que va a
        // tardar en escribirse entera. Es el gancho para el sonido de balbuceo: que
        // suene lo mismo que dura el texto.
        public event System.Action<float> AlEmpezarLinea;

        // Se dispara cuando la linea deja de escribirse: porque termino sola, porque
        // el jugador la completo con espacio o porque se cerro el cuadro.
        // El balbuceo tiene que cortarse aca.
        public event System.Action AlTerminarLinea;

        private void Awake()
        {
            if (panel == null) panel = gameObject;
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
            if (panel != null) panel.SetActive(false);
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
            if (indicadorContinuar != null) indicadorContinuar.SetActive(true);
            AlTerminarLinea?.Invoke();
        }

        // Corrutina que va anadiendo una palabra por vez al texto segun palabrasPorSegundo.
        //
        // Espera en tiempo real y no en tiempo de juego: mientras dura el dialogo el
        // juego esta congelado (timeScale 0) y con WaitForSeconds el texto no avanzaria.
        private IEnumerator EscribirLinea(string linea)
        {
            Escribiendo = true;
            lineaActualCompleta = linea;
            if (textoDialogo != null) textoDialogo.text = "";
            if (indicadorContinuar != null) indicadorContinuar.SetActive(false);

            string[] palabras = linea.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            float delay = 1f / Mathf.Max(palabrasPorSegundo, 0.1f);

            AlEmpezarLinea?.Invoke(palabras.Length * delay);

            for (int i = 0; i < palabras.Length; i++)
            {
                if (textoDialogo != null)
                {
                    textoDialogo.text += (i > 0 ? " " : "") + palabras[i];
                }
                yield return new WaitForSecondsRealtime(delay);
            }

            Escribiendo = false;
            rutinaEscritura = null;
            if (indicadorContinuar != null) indicadorContinuar.SetActive(true);
            AlTerminarLinea?.Invoke();
        }
    }
}
