using System.Collections;
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
