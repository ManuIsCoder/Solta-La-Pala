using SoltaLaPala.Guardado;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Menu de pausa in-game. Lo abre el gestor con la tecla de Pausa (ESC por defecto).
    // El tiempo ya queda congelado por GestorMenus, aca solo van las opciones.
    public class MenuPausa : MenuBase
    {
        protected override int OrdenPantalla => 120;

        // Confirma al jugador que el guardado manual se escribio.
        private Text textoAviso;

        protected override void Construir(Transform raiz)
        {
            // Mas alto que las otras pantallas: son 6 botones mas el aviso de guardado.
            GameObject panelFondo = CrearFondoYPanel(raiz, new Vector2(520f, 640f));
            Transform padre = panelFondo.transform;

            CrearTitulo(padre, "PAUSA", 520f);

            Vector2 tamanoBoton = new Vector2(380f, 60f);
            float y = -140f;
            const float salto = 76f;

            ConstructorUI.CrearBoton("BotonContinuar", padre, "Continuar",
                new Vector2(0f, y), tamanoBoton, ContinuarJugando);
            y -= salto;

            ConstructorUI.CrearBoton("BotonGuardar", padre, "Guardar",
                new Vector2(0f, y), tamanoBoton, Guardar);
            y -= salto;

            ConstructorUI.CrearBoton("BotonReiniciar", padre, "Reiniciar nivel",
                new Vector2(0f, y), tamanoBoton, ReiniciarNivel);
            y -= salto;

            ConstructorUI.CrearBoton("BotonConfiguracion", padre, "Configuracion",
                new Vector2(0f, y), tamanoBoton, () => Gestor?.Abrir(PantallaMenu.Configuracion));
            y -= salto;

            ConstructorUI.CrearBoton("BotonResultados", padre, "Ver resultados",
                new Vector2(0f, y), tamanoBoton, () => Gestor?.Abrir(PantallaMenu.Resultados));
            y -= salto;

            ConstructorUI.CrearBoton("BotonMenuPrincipal", padre, "Menu principal",
                new Vector2(0f, y), tamanoBoton, VolverAlMenuPrincipal);
            y -= salto;

            // Confirmacion de que el guardado se escribio. Arranca vacio.
            textoAviso = ConstructorUI.CrearTexto("Aviso", padre, "",
                18, TextAnchor.MiddleCenter, new Vector2(0f, y), new Vector2(440f, 28f));
            textoAviso.color = ConstructorUI.ColorTextoApagado;
        }

        // El aviso es de un guardado concreto: al reabrir la pausa ya no aplica.
        protected override void AlMostrar()
        {
            if (textoAviso != null)
            {
                textoAviso.text = "";
            }
        }

        // Autoguardado al volver al juego: el jugador ya paso por el menu, es el
        // momento natural para asegurar el progreso sin que tenga que pedirlo.
        private void ContinuarJugando()
        {
            Gestor?.GuardarPartida();
            Gestor?.CerrarTodo();
        }

        // Empieza el nivel de cero: contadores, reloj, objetos, misiones,
        // inventario y posicion del jugador.
        private void ReiniciarNivel()
        {
            Gestor?.ReiniciarYJugar();
        }

        // Guardado manual, para quien prefiere no confiar en el automatico.
        private void Guardar()
        {
            if (textoAviso == null)
            {
                return;
            }

            bool guardado = Gestor != null && Gestor.GuardarPartida();

            textoAviso.text = guardado
                ? "Partida guardada."
                : "No se pudo guardar la partida.";
            textoAviso.color = guardado
                ? ConstructorUI.ColorTextoApagado
                : ConstructorUI.ColorAcento;
        }

        // Vuelve al menu principal descartando el historial, para que "Volver"
        // desde ahi no devuelva a la pausa de una partida que ya se abandono.
        private void VolverAlMenuPrincipal()
        {
            if (Gestor == null)
            {
                return;
            }

            Gestor.CerrarTodo();
            Gestor.Abrir(PantallaMenu.Principal);
        }
    }
}
