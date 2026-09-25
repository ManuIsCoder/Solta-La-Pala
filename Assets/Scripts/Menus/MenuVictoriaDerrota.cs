using System.Text;
using SoltaLaPala.Guardado;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Pantalla de final de partida. Victoria y derrota comparten el mismo panel
    // y solo cambian titulo, color y el boton de "siguiente nivel", que en la
    // derrota no tiene sentido.
    public class MenuVictoriaDerrota : MenuBase
    {
        protected override int OrdenPantalla => 160;

        private Text titulo;
        private Text textoDetalle;
        private Text textoMarcador;
        private Button botonSiguiente;

        // Se guarda el ultimo final configurado para poder redibujarlo al mostrarse.
        private bool fueVictoria;
        private string detalleActual;

        protected override void Construir(Transform raiz)
        {
            GameObject panelFondo = CrearFondoYPanel(raiz, new Vector2(640f, 600f));
            Transform padre = panelFondo.transform;

            titulo = CrearTitulo(padre, "VICTORIA", 640f);

            textoDetalle = ConstructorUI.CrearTexto("Detalle", padre, "",
                22, TextAnchor.MiddleCenter, new Vector2(0f, -120f), new Vector2(540f, 70f));
            textoDetalle.color = ConstructorUI.ColorTextoApagado;

            textoMarcador = ConstructorUI.CrearTexto("Marcador", padre, "",
                24, TextAnchor.MiddleCenter, new Vector2(0f, -210f), new Vector2(540f, 90f));
            textoMarcador.lineSpacing = 1.4f;

            Vector2 tamanoBoton = new Vector2(380f, 58f);
            float y = -330f;
            const float salto = 72f;

            botonSiguiente = ConstructorUI.CrearBoton("BotonSiguiente", padre, "Siguiente nivel",
                new Vector2(0f, y), tamanoBoton, IrAlSiguienteNivel);
            y -= salto;

            ConstructorUI.CrearBoton("BotonReintentar", padre, "Reintentar nivel",
                new Vector2(0f, y), tamanoBoton, ReintentarNivel);
            y -= salto;

            ConstructorUI.CrearBoton("BotonNiveles", padre, "Seleccionar nivel",
                new Vector2(0f, y), tamanoBoton, () => AbrirDesdeFinal(PantallaMenu.SeleccionNiveles));
            y -= salto;

            ConstructorUI.CrearBoton("BotonMenuPrincipal", padre, "Menu principal",
                new Vector2(0f, y), tamanoBoton, () => AbrirDesdeFinal(PantallaMenu.Principal));
        }

        // La llama el GestorMenus antes de mostrar la pantalla, para saber si
        // toca pintar victoria o derrota.
        public void Configurar(bool victoria, string detalle)
        {
            AsegurarConstruida();

            fueVictoria = victoria;
            detalleActual = detalle;

            Refrescar();
        }

        protected override void AlMostrar()
        {
            Refrescar();
        }

        private void Refrescar()
        {
            if (titulo != null)
            {
                titulo.text = fueVictoria ? "SABOTAJE COMPLETADO" : "TE PILLARON";
                titulo.fontSize = 40;
                titulo.color = fueVictoria ? ConstructorUI.ColorExito : ConstructorUI.ColorFallo;
            }

            if (textoDetalle != null)
            {
                textoDetalle.text = string.IsNullOrEmpty(detalleActual)
                    ? (fueVictoria ? "Nadie sospecho nada." : "La sospecha llego al limite.")
                    : detalleActual;
            }

            if (textoMarcador != null)
            {
                textoMarcador.text = ArmarMarcador();
            }

            // Solo se ofrece el siguiente nivel si se gano y de verdad existe uno mas.
            if (botonSiguiente != null)
            {
                bool hayMas = ProgresoNiveles.NivelActual < ProgresoNiveles.CantidadNiveles;
                botonSiguiente.gameObject.SetActive(fueVictoria && hayMas);
            }
        }

        private static string ArmarMarcador()
        {
            EstadoPartida estado = EstadoPartida.Instancia;
            int nivel = ProgresoNiveles.NivelActual;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Nivel {nivel}");

            if (estado != null)
            {
                sb.AppendLine($"Impacto: {estado.Impacto}   Sospecha: {estado.Sospecha}");
            }
            else
            {
                sb.AppendLine($"Impacto: {ProgresoNiveles.ObtenerImpacto(nivel)}   Sospecha: {ProgresoNiveles.ObtenerSospecha(nivel)}");
            }

            return sb.ToString();
        }

        // Pasa al nivel siguiente y devuelve el control al juego.
        private void IrAlSiguienteNivel()
        {
            int siguiente = Mathf.Min(ProgresoNiveles.NivelActual + 1, ProgresoNiveles.CantidadNiveles);
            ProgresoNiveles.NivelActual = siguiente;

            EstadoPartida.Instancia?.Reiniciar();
            RegistroObjetosConsumidos.Instancia?.Limpiar();

            Debug.Log($"[Menus] Empezando nivel {siguiente}.");

            // EmpezarJuego y no CerrarTodo: es el que arranca el reloj del nivel.
            Gestor?.EmpezarJuego();
        }

        // Vuelve a empezar el mismo nivel con los contadores a cero.
        private void ReintentarNivel()
        {
            EstadoPartida.Instancia?.Reiniciar();
            RegistroObjetosConsumidos.Instancia?.Limpiar();

            Debug.Log($"[Menus] Reintentando nivel {ProgresoNiveles.NivelActual}.");
            Gestor?.EmpezarJuego();
        }

        // Desde el final de partida se limpia el historial: no tiene sentido
        // que "Volver" devuelva a la pantalla de derrota.
        private void AbrirDesdeFinal(PantallaMenu pantalla)
        {
            if (Gestor == null)
            {
                return;
            }

            EstadoPartida.Instancia?.Reiniciar();
            RegistroObjetosConsumidos.Instancia?.Limpiar();

            Gestor.CerrarTodo();
            Gestor.Abrir(pantalla);
        }
    }
}
