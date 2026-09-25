using SoltaLaPala.Guardado;
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

        private MovimientoJugador movimientoJugador;
        private GameObject objetoTazaMundo;

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

        public void Abrir(GameObject tazaEnMundo)
        {
            objetoTazaMundo = tazaEnMundo;
            movimientoJugador = FindFirstObjectByType<MovimientoJugador>();

            if (movimientoJugador != null)
            {
                movimientoJugador.BloquearMovimiento(true);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (textoResultado != null) textoResultado.text = "";
            if (panel != null) panel.SetActive(true);
        }

        public void Cerrar()
        {
            if (panel != null) panel.SetActive(false);

            if (movimientoJugador != null)
            {
                movimientoJugador.BloquearMovimiento(false);
            }

            // Si el sabotaje disparo el final de la partida, el GestorMenus acaba de
            // abrir la pantalla de victoria/derrota: recapturar el cursor aqui dejaria
            // esa pantalla sin raton. En ese caso manda el gestor.
            if (GestorMenus.Instancia != null && GestorMenus.Instancia.HayMenuAbierto)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
