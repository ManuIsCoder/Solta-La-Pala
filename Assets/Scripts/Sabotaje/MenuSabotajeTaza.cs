using SoltaLaPala.Guardado;
using SoltaLaPala.Interaction;
using SoltaLaPala.Menus;
using SoltaLaPala.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Sabotaje
{
    // Menu emergente de sabotaje para preparar la taza de cafe segun el GDD.
    // Permite elegir entre 4 opciones con distinto nivel de Impacto y Sospecha.
    public class MenuSabotajeTaza : MonoBehaviour
    {
        public static MenuSabotajeTaza Instancia { get; private set; }

        [Header("UI")]
        public GameObject panel;
        public Text textoTitulo;
        public Text textoResultado;

        [Header("Botones de Opciones")]
        public Button botonNormal;
        public Button botonSal;
        public Button botonFrio;
        public Button botonDetergente;

        [Header("Controles")]
        [Tooltip("Tecla que cierra el menu sin sabotear.")]
        public KeyCode teclaSalir = KeyCode.X;

        // Congela al jugador mientras el panel esta abierto.
        private readonly ControlJugador controlJugador = new ControlJugador();

        private GameObject objetoTazaMundo;

        // Requisito del interactuable que abrio el menu, para gastarle el item
        // solo si el sabotaje se llega a completar.
        private RequisitoItem requisitoActual;

        // True mientras el panel esta abierto. Lo consulta la camara para no
        // girar con el raton mientras eliges opcion.
        public bool Abierto => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            if (panel != null) panel.SetActive(false);

            if (botonNormal != null) botonNormal.onClick.AddListener(() => ElegirOpcion("Café Normal", 0, 0));
            if (botonSal != null) botonSal.onClick.AddListener(() => ElegirOpcion("Café con Sal", 1, 5));
            if (botonFrio != null) botonFrio.onClick.AddListener(() => ElegirOpcion("Café Frío y Rancio", 2, 10));
            if (botonDetergente != null) botonDetergente.onClick.AddListener(() => ElegirOpcion("Café con Detergente", 3, 20));
        }

        public void Abrir(GameObject tazaEnMundo, RequisitoItem requisito = null)
        {
            objetoTazaMundo = tazaEnMundo;
            requisitoActual = requisito;

            // Congela al jugador y libera el cursor: sin esto el raton con el que
            // eliges opcion seguiria girando la camara, y se podria disparar otra
            // interaccion con el panel abierto.
            controlJugador.Bloquear(true);

            if (textoResultado != null) textoResultado.text = "";

            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        // Cierra el panel sin sabotear nada. Es lo que hace la tecla de salir.
        public void Cancelar()
        {
            Cerrar();
        }

        // La tecla de salir cierra el menu sin elegir opcion.
        private void Update()
        {
            if (!Abierto)
            {
                return;
            }

            if (Input.GetKeyDown(teclaSalir))
            {
                Cancelar();
            }
        }

        public void Cerrar()
        {
            // El guard evita desbloquear dos veces si Cerrar se llama repetido: el
            // contador de UI de la camara se descuadraria y la vista se quedaria
            // sin poder girar.
            if (!Abierto)
            {
                return;
            }

            panel.SetActive(false);
            requisitoActual = null;

            controlJugador.Bloquear(false);

            // Si el sabotaje termino la partida, el GestorMenus acaba de abrir la
            // pantalla de victoria/derrota: esa necesita el cursor libre, y el
            // desbloqueo de arriba acaba de capturarlo. Se le devuelve.
            if (GestorMenus.Instancia != null && GestorMenus.Instancia.HayMenuAbierto)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void ElegirOpcion(string nombreCafe, int impacto, int sospecha)
        {
            Debug.Log($"[Sabotaje Café] Resultado: '{nombreCafe}' | Impacto: +{impacto} | Sospecha: +{sospecha}");

            if (textoResultado != null)
            {
                textoResultado.text = $"Has preparado: {nombreCafe}\nImpacto: +{impacto} | Sospecha: +{sospecha}";
            }

            // Ocultar la taza de la mesa tras interactuar
            if (objetoTazaMundo != null)
            {
                objetoTazaMundo.SetActive(false);

                if (RegistroObjetosConsumidos.Instancia != null)
                {
                    RegistroObjetosConsumidos.Instancia.Marcar(objetoTazaMundo);
                }
            }

            // El item se gasta aqui y no al abrir el menu: cancelar con la tecla
            // de salir no debe costarte el objeto.
            requisitoActual?.ConsumirSiHaceFalta();

            Cerrar();

            // Se suma despues de Cerrar(): si este sabotaje termina la partida,
            // EstadoPartida abre la pantalla de victoria/derrota y esa tiene que
            // quedar encima, no cerrarse al vuelo.
            if (EstadoPartida.Instancia != null)
            {
                EstadoPartida.Instancia.RegistrarSabotaje(impacto, sospecha);
            }

            // Autoguardado al final del todo: un sabotaje es progreso que duele
            // repetir. Va despues de RegistrarSabotaje para que el guardado incluya
            // los contadores nuevos, y GuardarAhora ya se abstiene si el sabotaje
            // acabo de terminar la partida.
            if (GestorGuardado.Instancia != null)
            {
                GestorGuardado.Instancia.GuardarAhora();
            }
        }
    }
}
