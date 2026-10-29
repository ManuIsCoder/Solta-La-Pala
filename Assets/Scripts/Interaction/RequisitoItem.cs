using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Interaction
{
    public class RequisitoItem : MonoBehaviour
    {
        [Header("Requisito")]
        [Tooltip("Item que hay que llevar para poder interactuar.")]
        public DatosItem itemRequerido;

        [Tooltip("Si esta marcado, el item tiene que estar en la mano (slot " +
                 "seleccionado). Si no, basta con llevarlo en cualquier slot.")]
        public bool debeEstarEnLaMano = true;

        [Tooltip("Si esta marcado, usar el interactuable gasta el item.")]
        public bool consumirAlUsar = true;

        [Header("Texto")]
        [Tooltip("Que mostrar cuando falta el item. {item} se sustituye por su nombre.")]
        public string textoSinItem = "Necesitas {item}";

        public bool SeCumple()
        {
            if (itemRequerido == null)
            {
                return true;
            }

            return BuscarSlot() != -1;
        }

        public string ObtenerTextoBloqueado()
        {
            string nombre = itemRequerido != null && !string.IsNullOrEmpty(itemRequerido.nombre)
                ? itemRequerido.nombre
                : "un objeto";

            return textoSinItem.Replace("{item}", nombre);
        }

        public void ConsumirSiHaceFalta()
        {
            if (!consumirAlUsar || itemRequerido == null)
            {
                return;
            }

            InventarioJugador inventario = InventarioJugador.DelJugador();

            if (inventario == null)
            {
                return;
            }

            int slot = inventario.BuscarSlotCon(itemRequerido, debeEstarEnLaMano);

            if (slot != -1)
            {
                inventario.QuitarItem(slot);
            }
        }

        private int BuscarSlot()
        {
            InventarioJugador inventario = InventarioJugador.DelJugador();

            return inventario != null
                ? inventario.BuscarSlotCon(itemRequerido, debeEstarEnLaMano)
                : -1;
        }
    }
}
