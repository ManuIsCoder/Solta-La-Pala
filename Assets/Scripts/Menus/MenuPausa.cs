using SoltaLaPala.Guardado;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    public class MenuPausa : MenuBase
    {
        protected override int OrdenPantalla => 120;

        [Header("Prefab (Resources/Menus)")]
        public Button botonContinuar;
        public Button botonGuardar;
        public Button botonReiniciar;
        public Button botonConfiguracion;
        public Button botonResultados;
        public Button botonMenuPrincipal;
        [Tooltip("Confirma al jugador que el guardado manual se escribio.")]
        public Text textoAviso;

        protected override void Enlazar()
        {
            Enganchar(botonContinuar, ContinuarJugando);
            Enganchar(botonGuardar, Guardar);
            Enganchar(botonReiniciar, ReiniciarNivel);
            Enganchar(botonConfiguracion, () => Gestor?.Abrir(PantallaMenu.Configuracion));
            Enganchar(botonResultados, () => Gestor?.Abrir(PantallaMenu.Resultados));
            Enganchar(botonMenuPrincipal, VolverAlMenuPrincipal);

            AjustarEscala();
        }

        private void AjustarEscala()
        {
            Transform fondo = transform.Find("Canvas/Fondo");
            if (fondo == null) fondo = transform.Find("Fondo");
            if (fondo != null)
            {
                fondo.localScale = new Vector3(0.5f, 0.5f, 1f);
            }
        }

        protected override void Construir(Transform raiz)
        {
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

            textoAviso = ConstructorUI.CrearTexto("Aviso", padre, "",
                18, TextAnchor.MiddleCenter, new Vector2(0f, y), new Vector2(440f, 28f));
            textoAviso.color = ConstructorUI.ColorTextoApagado;
        }

        protected override void AlMostrar()
        {
            if (textoAviso != null)
            {
                textoAviso.text = "";
            }
        }

        private void ContinuarJugando()
        {
            Gestor?.GuardarPartida();
            Gestor?.CerrarTodo();
        }

        private void ReiniciarNivel()
        {
            Gestor?.ReiniciarYJugar();
        }

        private void Guardar()
        {
            bool guardado = Gestor != null && Gestor.GuardarPartida();

            if (textoAviso == null)
            {
                return;
            }

            textoAviso.text = guardado
                ? "Partida guardada."
                : "No se pudo guardar la partida.";
            textoAviso.color = guardado
                ? ConstructorUI.ColorTextoApagado
                : ConstructorUI.ColorAcento;
        }

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
