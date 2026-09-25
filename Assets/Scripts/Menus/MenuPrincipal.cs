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
        private Button botonPrincipal;
        private Text etiquetaPrincipal;

        protected override void Construir(Transform raiz)
        {
            GameObject panelFondo = CrearFondoYPanel(raiz, new Vector2(560f, 620f));
            Transform padre = panelFondo.transform;

            Text titulo = CrearTitulo(padre, "SOLTA LA PALA", 560f);
            titulo.fontSize = 52;

            ConstructorUI.CrearTexto("Subtitulo", padre, "Sabotea sin que te pillen",
                20, TextAnchor.MiddleCenter, new Vector2(0f, -100f), new Vector2(480f, 30f))
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

        // Un solo boton que cambia de nombre en vez de dos botones, uno de ellos
        // apagado: asi nunca ofrece algo que no va a hacer. La unica forma de
        // pisar una partida guardada es elegir un nivel a mano.
        protected override void AlMostrar()
        {
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
