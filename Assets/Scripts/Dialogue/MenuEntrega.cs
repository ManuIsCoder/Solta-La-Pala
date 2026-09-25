using System.Collections.Generic;
using SoltaLaPala.Interaction;
using SoltaLaPala.Menus;
using SoltaLaPala.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Dialogue
{
    // Menu para elegir que le entregas a un NPC que te encargo algo.
    //
    // Muestra todo lo que el NPC aceptaria: lo que llevas encima se puede pulsar,
    // lo que te falta sale en gris. Asi se ve que hay mas de una forma de cerrar
    // el encargo sin tener que adivinarlo.
    //
    // Se genera por codigo como el resto de la UI del prototipo.
    public class MenuEntrega : MonoBehaviour
    {
        public static MenuEntrega Instancia { get; private set; }

        [Header("Controles")]
        [Tooltip("Tecla que cierra el menu sin entregar nada.")]
        public KeyCode teclaSalir = KeyCode.X;

        // Por encima del dialogo (20) y por debajo de los menus de pausa (100+).
        private const int OrdenCanvas = 40;

        private GameObject panel;
        private Text titulo;
        private Transform contenedorBotones;

        private MisionEntrega misionActual;
        private readonly List<GameObject> botones = new List<GameObject>();

        public bool Abierto => panel != null && panel.activeSelf;

        // Congela al jugador mientras el menu esta abierto.
        private readonly ControlJugador controlJugador = new ControlJugador();

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            Construir();
        }

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
            }
        }

        private void Update()
        {
            if (Abierto && Input.GetKeyDown(teclaSalir))
            {
                Cerrar();
            }
        }

        // Abre el menu con las opciones de esa mision. Devuelve false si no hay
        // nada que ofrecer, para que quien llama siga con el dialogo normal.
        public bool Abrir(MisionEntrega mision)
        {
            if (mision == null || mision.Completada)
            {
                return false;
            }

            List<MisionEntrega.Opcion> opciones = mision.ObtenerOpciones();

            if (opciones.Count == 0)
            {
                return false;
            }

            misionActual = mision;

            titulo.text = $"ENTREGAR A {mision.name.ToUpperInvariant()}";
            RellenarBotones(opciones);

            panel.SetActive(true);
            controlJugador.Bloquear(true);

            return true;
        }

        public void Cerrar()
        {
            if (!Abierto)
            {
                return;
            }

            panel.SetActive(false);
            misionActual = null;

            controlJugador.Bloquear(false);
        }

        private void Elegir(MisionEntrega.Opcion opcion)
        {
            MisionEntrega mision = misionActual;

            // Se cierra antes de entregar: si la entrega termina la partida, la
            // pantalla de derrota tiene que quedar encima de este menu.
            Cerrar();

            mision?.EntregarOpcion(opcion);
        }

        // Un boton por opcion. Los que no llevas quedan apagados y sin accion.
        private void RellenarBotones(List<MisionEntrega.Opcion> opciones)
        {
            foreach (GameObject viejo in botones)
            {
                Destroy(viejo);
            }
            botones.Clear();

            float y = -90f;

            foreach (MisionEntrega.Opcion opcion in opciones)
            {
                string nombre = !string.IsNullOrEmpty(opcion.item.nombre)
                    ? opcion.item.nombre
                    : opcion.item.name;

                string etiqueta = opcion.disponible
                    ? nombre
                    : $"{nombre}  (no lo llevas)";

                Button boton = ConstructorUI.CrearBoton($"Opcion{nombre}", contenedorBotones,
                    etiqueta, new Vector2(0f, y), new Vector2(420f, 58f), null);

                if (opcion.disponible)
                {
                    // La copia local evita que todos los botones capturen la misma
                    // variable del bucle.
                    MisionEntrega.Opcion elegida = opcion;
                    boton.onClick.AddListener(() => Elegir(elegida));
                }
                else
                {
                    boton.interactable = false;

                    Image fondo = boton.GetComponent<Image>();
                    if (fondo != null)
                    {
                        fondo.color = ConstructorUI.ColorBotonApagado;
                    }

                    Text texto = boton.GetComponentInChildren<Text>();
                    if (texto != null)
                    {
                        texto.color = ConstructorUI.ColorTextoApagado;
                    }
                }

                botones.Add(boton.gameObject);
                y -= 70f;
            }
        }

        private void Construir()
        {
            ConstructorUI.AsegurarEventSystem();

            Canvas canvas = ConstructorUI.CrearCanvas("CanvasEntrega", transform, OrdenCanvas);

            panel = ConstructorUI.CrearPanel("PanelEntrega", canvas.transform,
                new Vector2(520f, 440f));
            contenedorBotones = panel.transform;

            titulo = ConstructorUI.CrearTexto("Titulo", panel.transform, "ENTREGAR",
                30, TextAnchor.MiddleCenter, new Vector2(0f, -30f), new Vector2(460f, 44f));
            titulo.color = ConstructorUI.ColorAcento;

            Text ayuda = ConstructorUI.CrearTexto("Ayuda", panel.transform,
                "[X] Cancelar", 18, TextAnchor.MiddleCenter,
                new Vector2(0f, -380f), new Vector2(300f, 26f));
            ayuda.color = ConstructorUI.ColorTextoApagado;

            panel.SetActive(false);
        }
    }
}
