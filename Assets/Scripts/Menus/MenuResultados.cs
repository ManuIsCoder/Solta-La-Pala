using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Resumen de la partida en curso y del progreso guardado en cada nivel.
    public class MenuResultados : MenuBase
    {
        protected override int OrdenPantalla => 150;

        private Text textoPartidaActual;
        private Text textoHistorial;

        protected override void Construir(Transform raiz)
        {
            GameObject panelFondo = CrearFondoYPanel(raiz, new Vector2(760f, 680f));
            Transform padre = panelFondo.transform;

            CrearTitulo(padre, "RESULTADOS", 760f);

            ConstructorUI.CrearTexto("SubtituloActual", padre, "PARTIDA ACTUAL",
                24, TextAnchor.MiddleCenter, new Vector2(0f, -110f), new Vector2(660f, 30f))
                .color = ConstructorUI.ColorAcento;

            textoPartidaActual = ConstructorUI.CrearTexto("Actual", padre, "",
                22, TextAnchor.UpperLeft, new Vector2(0f, -155f), new Vector2(620f, 130f));
            textoPartidaActual.lineSpacing = 1.4f;

            ConstructorUI.CrearTexto("SubtituloHistorial", padre, "POR NIVEL",
                24, TextAnchor.MiddleCenter, new Vector2(0f, -310f), new Vector2(660f, 30f))
                .color = ConstructorUI.ColorAcento;

            textoHistorial = ConstructorUI.CrearTexto("Historial", padre, "",
                20, TextAnchor.UpperLeft, new Vector2(0f, -355f), new Vector2(620f, 200f));
            textoHistorial.lineSpacing = 1.4f;

            ConstructorUI.CrearBoton("BotonVolver", padre, "Volver",
                new Vector2(0f, -600f), new Vector2(260f, 56f), () => Gestor?.Volver());
        }

        // Los datos se leen al abrir, no al construir: mientras el menu esta cerrado
        // el jugador sigue sumando impacto y sospecha.
        protected override void AlMostrar()
        {
            RefrescarPartidaActual();
            RefrescarHistorial();
        }

        private void RefrescarPartidaActual()
        {
            if (textoPartidaActual == null)
            {
                return;
            }

            EstadoPartida estado = EstadoPartida.Instancia;

            if (estado == null)
            {
                textoPartidaActual.text = "No hay ninguna partida en curso.";
                textoPartidaActual.color = ConstructorUI.ColorTextoApagado;
                return;
            }

            textoPartidaActual.color = ConstructorUI.ColorTexto;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Nivel: {ProgresoNiveles.NivelActual}");
            sb.AppendLine($"Impacto: {estado.Impacto} / {estado.ImpactoObjetivo}");
            sb.AppendLine($"Sospecha: {estado.Sospecha} / {estado.SospechaMaxima}");

            textoPartidaActual.text = sb.ToString();
        }

        private void RefrescarHistorial()
        {
            if (textoHistorial == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();

            for (int nivel = 1; nivel <= ProgresoNiveles.CantidadNiveles; nivel++)
            {
                if (!ProgresoNiveles.EstaDesbloqueado(nivel))
                {
                    sb.AppendLine($"Nivel {nivel}:  bloqueado");
                    continue;
                }

                int impacto = ProgresoNiveles.ObtenerImpacto(nivel);
                int sospecha = ProgresoNiveles.ObtenerSospecha(nivel);

                // Un nivel desbloqueado pero sin impacto guardado es uno al que
                // todavia no se jugo (se desbloqueo al ganar el anterior).
                if (impacto == 0 && sospecha == 0)
                {
                    sb.AppendLine($"Nivel {nivel}:  sin jugar");
                }
                else
                {
                    sb.AppendLine($"Nivel {nivel}:  impacto {impacto}  ·  sospecha {sospecha}");
                }
            }

            textoHistorial.text = sb.ToString();
        }
    }
}
