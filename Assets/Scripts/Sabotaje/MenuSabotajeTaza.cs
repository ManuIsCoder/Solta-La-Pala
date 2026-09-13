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
            }

            Cerrar();
        }
    }
}
