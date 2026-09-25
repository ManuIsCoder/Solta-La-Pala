using SoltaLaPala.Guardado;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Selector de los 4 niveles. Los que todavia no se desbloquearon salen
    // apagados y no se pueden pulsar.
    public class MenuSeleccionNiveles : MenuBase
    {
        protected override int OrdenPantalla => 110;

        // Botones e imagenes de cada nivel, para poder refrescarlos al abrir la pantalla.
        private readonly Button[] botonesNivel = new Button[ProgresoNiveles.CantidadNiveles];
        private readonly Text[] etiquetasNivel = new Text[ProgresoNiveles.CantidadNiveles];
        private Text textoAviso;

        protected override void Construir(Transform raiz)
        {
            GameObject panelFondo = CrearFondoYPanel(raiz, new Vector2(680f, 520f));
            Transform padre = panelFondo.transform;

            CrearTitulo(padre, "SELECCIONAR NIVEL", 680f);

            // Los 4 niveles en una fila de cuadrados centrada.
            const float lado = 120f;
            const float separacion = 24f;
            float anchoTotal = ProgresoNiveles.CantidadNiveles * lado
                + (ProgresoNiveles.CantidadNiveles - 1) * separacion;
            float xInicial = -anchoTotal * 0.5f + lado * 0.5f;

            for (int i = 0; i < ProgresoNiveles.CantidadNiveles; i++)
            {
                int nivel = i + 1;
                float x = xInicial + i * (lado + separacion);

                Button boton = ConstructorUI.CrearBoton($"Nivel{nivel}", padre, nivel.ToString(),
                    new Vector2(x, -150f), new Vector2(lado, lado), () => JugarNivel(nivel));

                Text etiqueta = boton.GetComponentInChildren<Text>();
                if (etiqueta != null)
                {
                    etiqueta.fontSize = 44;
                }

                botonesNivel[i] = boton;
                etiquetasNivel[i] = etiqueta;
            }

            textoAviso = ConstructorUI.CrearTexto("Aviso", padre,
                "Completa un nivel para desbloquear el siguiente.",
                18, TextAnchor.MiddleCenter, new Vector2(0f, -300f), new Vector2(600f, 30f));
            textoAviso.color = ConstructorUI.ColorTextoApagado;

            ConstructorUI.CrearBoton("BotonVolver", padre, "Volver",
                new Vector2(0f, -400f), new Vector2(240f, 56f), () => Gestor?.Volver());
        }

        // Al abrirse, marca que niveles estan disponibles segun el progreso guardado.
        protected override void AlMostrar()
        {
            RefrescarBloqueos();
            RefrescarAviso();
        }

        // Esta pantalla es la unica forma de pisar una partida guardada, asi que
        // tiene que decirlo antes de que el jugador pulse. Se reutiliza el texto
        // que ya estaba al pie en vez de añadir un panel de confirmacion: el juego
        // es corto y un modal por cada nivel elegido es friccion.
        private void RefrescarAviso()
        {
            if (textoAviso == null)
            {
                return;
            }

            if (GuardadoPartida.Existe())
            {
                textoAviso.text = "Ojo: empezar un nivel borra tu partida guardada.";
                textoAviso.color = ConstructorUI.ColorAcento;
            }
            else
            {
                textoAviso.text = "Completa un nivel para desbloquear el siguiente.";
                textoAviso.color = ConstructorUI.ColorTextoApagado;
            }
        }

        private void RefrescarBloqueos()
        {
            for (int i = 0; i < botonesNivel.Length; i++)
            {
                int nivel = i + 1;
                bool desbloqueado = ProgresoNiveles.EstaDesbloqueado(nivel);

                if (botonesNivel[i] != null)
                {
                    botonesNivel[i].interactable = desbloqueado;

                    Image fondo = botonesNivel[i].GetComponent<Image>();
                    if (fondo != null)
                    {
                        fondo.color = desbloqueado
                            ? ConstructorUI.ColorBoton
                            : ConstructorUI.ColorBotonApagado;
                    }
                }

                if (etiquetasNivel[i] != null)
                {
                    // Un candado marca de un vistazo cual no se puede jugar todavia.
                    etiquetasNivel[i].text = desbloqueado ? nivel.ToString() : "?";
                    etiquetasNivel[i].color = desbloqueado
                        ? ConstructorUI.ColorTexto
                        : ConstructorUI.ColorTextoApagado;
                }
            }
        }

        // Arranca el nivel elegido. Por ahora solo lo registra y cierra los menus,
        // porque todo el juego vive en una sola escena: cuando haya una escena por
        // nivel, aca va la carga de escena.
        private void JugarNivel(int nivel)
        {
            if (!ProgresoNiveles.EstaDesbloqueado(nivel))
            {
                return;
            }

            ProgresoNiveles.NivelActual = nivel;
            Debug.Log($"[Menus] Empezando nivel {nivel}.");

            // Descarta la partida guardada y limpia todo lo de la anterior. Se
            // avisa en RefrescarAviso antes de llegar aqui.
            Gestor?.ReiniciarYJugar();
        }
    }
}
