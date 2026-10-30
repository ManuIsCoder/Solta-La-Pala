using SoltaLaPala.Guardado;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Pantalla de inicio: titulo del juego y las opciones principales.
    public class MenuPrincipal : MenuBase
    {
        protected override int OrdenPantalla => 100;

        // El primer boton es "Jugar" o "Continuar" segun haya partida guardada.
        // Se guardan las referencias para poder reetiquetarlo en AlMostrar.
        [Header("Arte")]
        public Sprite spriteTitulo;

        // Con prefab el menu tiene dos pasos sobre el mismo fondo:
        //   inicio:  Jugar, Opciones, Salir
        //   partida: Nueva partida, Continuar partida (al pulsar Jugar)
        // No se elige nivel: Continuar retoma el ultimo guardado.
        [Header("Prefab (Resources/Menus)")]
        [Tooltip("Contiene Jugar, Opciones y Salir.")]
        public GameObject grupoInicio;
        [Tooltip("Contiene Nueva partida y Continuar partida. Se muestra al pulsar Jugar.")]
        public GameObject grupoPartida;
        [Tooltip("Jugar. Sin prefab es el boton Jugar/Continuar del menu generado por codigo.")]
        public Button botonPrincipal;
        public Text etiquetaPrincipal;
        public Button botonConfiguracion;
        public Button botonSalir;
        public Button botonNuevaPartida;
        public Button botonContinuarPartida;

        private bool UsaPasos => grupoInicio != null && grupoPartida != null;

        protected override void Enlazar()
        {
            Enganchar(botonConfiguracion, () => Gestor?.Abrir(PantallaMenu.Configuracion));
            Enganchar(botonSalir, () => Gestor?.SalirDelJuego());

            if (UsaPasos)
            {
                Enganchar(botonPrincipal, () => MostrarPaso(partida: true));
                Enganchar(botonNuevaPartida, EmpezarNuevaPartida);
                Enganchar(botonContinuarPartida, () => Gestor?.CargarPartidaGuardada());
            }
        }

        private void MostrarPaso(bool partida)
        {
            grupoInicio.SetActive(!partida);
            grupoPartida.SetActive(partida);

            if (partida && botonContinuarPartida != null)
            {
                // Sin guardado se ve apagado en vez de esconderse, para que el
                // diseno no cambie segun haya partida o no.
                botonContinuarPartida.interactable = GuardadoPartida.Existe();
            }
        }

        // Borra el guardado y el progreso de niveles y arranca desde el nivel 1.
        private void EmpezarNuevaPartida()
        {
            ProgresoNiveles.Reiniciar();
            Gestor?.ReiniciarYJugar();
        }

        // ESC en el segundo paso vuelve a Jugar/Opciones/Salir. El gestor no
        // cierra el menu principal con ESC, asi que no hay doble lectura.
        private void Update()
        {
            if (UsaPasos && grupoPartida.activeSelf && ControlesJuego.Pulsada(AccionJuego.Pausa))
            {
                MostrarPaso(partida: false);
            }
        }

        protected override void Construir(Transform raiz)
        {
            GameObject panelFondo = CrearFondoYPanel(raiz, new Vector2(560f, 620f));
            Transform padre = panelFondo.transform;

            if (spriteTitulo == null)
            {
                spriteTitulo = Resources.Load<Sprite>("Sprites/UI/TituloJuego");
            }

            if (spriteTitulo != null)
            {
                GameObject objTitulo = new GameObject("LogoTitulo");
                objTitulo.transform.SetParent(padre, false);
                RectTransform rectTitulo = objTitulo.AddComponent<RectTransform>();
                rectTitulo.anchorMin = new Vector2(0.5f, 1f);
                rectTitulo.anchorMax = new Vector2(0.5f, 1f);
                rectTitulo.pivot = new Vector2(0.5f, 1f);
                rectTitulo.anchoredPosition = new Vector2(0f, -24f);
                rectTitulo.sizeDelta = new Vector2(480f, 94f);

                Image imgTitulo = objTitulo.AddComponent<Image>();
                imgTitulo.sprite = spriteTitulo;
                imgTitulo.preserveAspect = true;
            }
            else
            {
                Text titulo = CrearTitulo(padre, "SOLTA LA PALA", 560f);
                titulo.fontSize = 52;
            }

            ConstructorUI.CrearTexto("Subtitulo", padre, "Sabotea sin que te pillen",
                20, TextAnchor.MiddleCenter, new Vector2(0f, -125f), new Vector2(480f, 30f))
                .color = ConstructorUI.ColorTextoApagado;

            Vector2 tamanoBoton = new Vector2(400f, 62f);
            float y = -170f;
            const float salto = 78f;

            // Se crea sin accion: el texto y el onClick los pone AlMostrar, que es
            // el unico que sabe si hay partida guardada en este momento.
            botonPrincipal = ConstructorUI.CrearBoton("BotonPrincipal", padre, "Jugar",
                new Vector2(0f, y), tamanoBoton, null);
            etiquetaPrincipal = botonPrincipal.GetComponentInChildren<Text>();
            y -= salto;

            ConstructorUI.CrearBoton("BotonNiveles", padre, "Seleccionar nivel",
                new Vector2(0f, y), tamanoBoton, () => Gestor?.Abrir(PantallaMenu.SeleccionNiveles));
            y -= salto;

            ConstructorUI.CrearBoton("BotonTutorial", padre, "Tutorial",
                new Vector2(0f, y), tamanoBoton, () => Gestor?.Abrir(PantallaMenu.Tutorial));
            y -= salto;

            ConstructorUI.CrearBoton("BotonConfiguracion", padre, "Configuracion",
                new Vector2(0f, y), tamanoBoton, () => Gestor?.Abrir(PantallaMenu.Configuracion));
            y -= salto;

            ConstructorUI.CrearBoton("BotonSalir", padre, "Salir",
                new Vector2(0f, y), tamanoBoton, () => Gestor?.SalirDelJuego());
        }

        // Con prefab siempre se abre en el primer paso.
        //
        // Sin prefab (menu generado por codigo): un solo boton que cambia de
        // nombre en vez de dos botones, uno de ellos apagado. La unica forma de
        // pisar una partida guardada es elegir un nivel a mano.
        protected override void AlMostrar()
        {
            if (UsaPasos)
            {
                MostrarPaso(partida: false);
                return;
            }

            if (botonPrincipal == null)
            {
                return;
            }

            bool hayGuardado = GuardadoPartida.Existe();

            if (etiquetaPrincipal != null)
            {
                etiquetaPrincipal.text = hayGuardado ? "Continuar" : "Jugar";
            }

            // AlMostrar corre cada vez que se abre la pantalla, asi que hay que
            // limpiar antes de asignar o los listeners se acumulan.
            botonPrincipal.onClick.RemoveAllListeners();

            if (hayGuardado)
            {
                botonPrincipal.onClick.AddListener(() => Gestor?.CargarPartidaGuardada());
            }
            else
            {
                botonPrincipal.onClick.AddListener(() => Gestor?.EmpezarJuego());
            }
        }
    }
}
