using UnityEngine;
using UnityEngine.UI;
using SoltaLaPala.Menus;
using SoltaLaPala.Inventory;

namespace SoltaLaPala.Minijuegos
{
    public class MinijuegoDiscurso : MinijuegoBase
    {
        [Header("IDs de Ítems")]
        public string idTarjetas = "TarjetasDiscurso";
        public string idTarjetaSabotaje = "DiscursoFalso";

        [Header("Fondo UI")]
        public Image imagenFondo;

        [Header("Tarjetas de Discurso (Botones / Arrastrables)")]
        public Button tarjetaTrabajo1;
        public Button tarjetaTrabajo2;
        public Button tarjetaDisparate; // Tarjeta mal escrita (sabotaje)

        [Header("UI General")]
        public Text textoTemaSeleccionado;
        public Button botonDarDiscurso;
        public Button botonSalir;

        [Header("Ajustes")]
        public bool modoPrueba = false;

        private int tarjetaElegida = -1; // 0 o 1 = Trabajo, 2 = Sabotaje

        protected override void AlIniciar()
        {
            bool tieneTarjetas = modoPrueba || (Inventario != null && Inventario.TieneItem(idTarjetas));
            bool tieneSabotaje = modoPrueba || (Inventario != null && Inventario.TieneItem(idTarjetaSabotaje));
            tarjetaElegida = -1;

            if (textoTemaSeleccionado != null)
                textoTemaSeleccionado.text = "Selecciona una tarjeta para el discurso...";

            if (botonDarDiscurso != null)
            {
                botonDarDiscurso.interactable = false;
                botonDarDiscurso.onClick.RemoveAllListeners();
                botonDarDiscurso.onClick.AddListener(DarDiscurso);
            }

            if (botonSalir != null)
            {
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
            }

            ConfigurarBotonTarjeta(tarjetaTrabajo1, 0, tieneTarjetas, "Estrategia Financiera Q4");
            ConfigurarBotonTarjeta(tarjetaTrabajo2, 1, tieneTarjetas, "Optimización de Recursos");
            ConfigurarBotonTarjeta(tarjetaDisparate, 2, tieneSabotaje, "Poema a la Milanesa (Disparate)");
        }

        private void ConfigurarBotonTarjeta(Button boton, int indice, bool interactuable, string titulo)
        {
            if (boton == null) return;
            boton.interactable = interactuable;
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(() => SeleccionarTarjeta(indice, titulo));
        }

        public void SeleccionarTarjeta(int indice, string titulo)
        {
            tarjetaElegida = indice;
            if (textoTemaSeleccionado != null)
                textoTemaSeleccionado.text = $"Discurso seleccionado: {titulo}";

            if (botonDarDiscurso != null)
                botonDarDiscurso.interactable = true;
        }

        public void DarDiscurso()
        {
            if (tarjetaElegida == -1) return;

            bool esSabotaje = (tarjetaElegida == 2);
            int impacto = esSabotaje ? 3 : 1;
            int sospecha = esSabotaje ? 6 : 0;

            if (EstadoPartida.Instancia != null)
                EstadoPartida.Instancia.RegistrarSabotaje(impacto, sospecha);

            if (esSabotaje)
                Inventario?.QuitarItem(idTarjetaSabotaje);
            else
                Inventario?.QuitarItem(idTarjetas);

            Salir();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Salir();
        }
    }
}