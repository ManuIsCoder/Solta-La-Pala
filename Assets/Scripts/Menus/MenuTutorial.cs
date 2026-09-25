using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Pantalla que explica las mecanicas. El texto se arma leyendo las teclas reales
    // de ControlesJuego, asi que si el jugador reasigna una tecla el tutorial
    // se actualiza solo en vez de mentir.
    public class MenuTutorial : MenuBase
    {
        protected override int OrdenPantalla => 130;

        private Text textoContenido;

        protected override void Construir(Transform raiz)
        {
            GameObject panelFondo = CrearFondoYPanel(raiz, new Vector2(860f, 720f));
            Transform padre = panelFondo.transform;

            CrearTitulo(padre, "COMO SE JUEGA", 860f);

            textoContenido = ConstructorUI.CrearTexto("Contenido", padre, "",
                22, TextAnchor.UpperLeft, new Vector2(0f, -110f), new Vector2(760f, 500f));
            textoContenido.lineSpacing = 1.35f;

            ConstructorUI.CrearBoton("BotonVolver", padre, "Entendido",
                new Vector2(0f, -640f), new Vector2(260f, 56f), () => Gestor?.Volver());
        }

        // Cada vez que se abre se regenera el texto, por si cambiaron los controles.
        protected override void AlMostrar()
        {
            if (textoContenido != null)
            {
                textoContenido.text = ArmarTexto();
            }
        }

        // Explicacion de las mecanicas con las teclas actuales incrustadas.
        private static string ArmarTexto()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("OBJETIVO");
            sb.AppendLine("Sabotea el trabajo de la oficina sin levantar sospechas. Cada sabotaje suma Impacto, pero tambien Sospecha: si la Sospecha se dispara, te pillan.");
            sb.AppendLine();

            sb.AppendLine("MOVIMIENTO");
            sb.AppendLine($"Muevete con {Tecla(AccionJuego.Adelante)} {Tecla(AccionJuego.Izquierda)} {Tecla(AccionJuego.Atras)} {Tecla(AccionJuego.Derecha)} y mira con el raton.");
            sb.AppendLine($"Manten {Tecla(AccionJuego.Correr)} para correr. Cuidado: correr delante de un companero llama la atencion.");
            sb.AppendLine();

            sb.AppendLine("INTERACCION");
            sb.AppendLine($"Los objetos con los que puedes interactuar se marcan con un borde blanco. Acercate y pulsa {Tecla(AccionJuego.Interactuar)}.");
            sb.AppendLine();

            sb.AppendLine("INVENTARIO");
            sb.AppendLine($"Tienes 3 espacios. Recoge objetos con {Tecla(AccionJuego.Interactuar)} y cambia el seleccionado con {Tecla(AccionJuego.Slot1)}, {Tecla(AccionJuego.Slot2)} y {Tecla(AccionJuego.Slot3)}.");
            sb.AppendLine();

            sb.AppendLine("DIALOGOS");
            sb.AppendLine($"Habla con los NPC para sacar informacion. Pulsa {Tecla(AccionJuego.AvanzarDialogo)} para pasar de linea o completar la frase de golpe.");
            sb.AppendLine();

            sb.AppendLine("SABOTAJE");
            sb.AppendLine("Al sabotear eliges como hacerlo. Las opciones mas bestias dan mas Impacto pero disparan la Sospecha, asi que mide el riesgo.");
            sb.AppendLine();

            sb.AppendLine("VIGILANCIA");
            sb.AppendLine("Los NPC tienen un cono de vision. No sabotees mientras te miran.");
            sb.AppendLine();

            sb.AppendLine($"Pulsa {Tecla(AccionJuego.Pausa)} en cualquier momento para pausar.");

            return sb.ToString();
        }

        // Nombre de la tecla asignada a una accion, entre corchetes.
        private static string Tecla(AccionJuego accion)
        {
            KeyCode tecla = ControlesJuego.ObtenerTecla(accion);
            return tecla == KeyCode.None ? "[sin asignar]" : $"[{NombreTecla(tecla)}]";
        }

        // Pasa el KeyCode a algo legible: Alpha1 -> 1, LeftShift -> Shift izq.
        public static string NombreTecla(KeyCode tecla)
        {
            switch (tecla)
            {
                case KeyCode.None: return "---";
                case KeyCode.Alpha0: return "0";
                case KeyCode.Alpha1: return "1";
                case KeyCode.Alpha2: return "2";
                case KeyCode.Alpha3: return "3";
                case KeyCode.Alpha4: return "4";
                case KeyCode.Alpha5: return "5";
                case KeyCode.Alpha6: return "6";
                case KeyCode.Alpha7: return "7";
                case KeyCode.Alpha8: return "8";
                case KeyCode.Alpha9: return "9";
                case KeyCode.LeftShift: return "Shift izq";
                case KeyCode.RightShift: return "Shift der";
                case KeyCode.LeftControl: return "Ctrl izq";
                case KeyCode.RightControl: return "Ctrl der";
                case KeyCode.LeftAlt: return "Alt izq";
                case KeyCode.RightAlt: return "Alt der";
                case KeyCode.Space: return "Espacio";
                case KeyCode.Escape: return "Esc";
                case KeyCode.Return: return "Enter";
                case KeyCode.Tab: return "Tab";
                case KeyCode.UpArrow: return "Arriba";
                case KeyCode.DownArrow: return "Abajo";
                case KeyCode.LeftArrow: return "Izquierda";
                case KeyCode.RightArrow: return "Derecha";
                case KeyCode.Mouse0: return "Click izq";
                case KeyCode.Mouse1: return "Click der";
                case KeyCode.Mouse2: return "Click medio";
                default: return tecla.ToString();
            }
        }
    }
}
