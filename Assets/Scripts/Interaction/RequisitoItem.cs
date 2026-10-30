using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Exige llevar cierto item para poder usar este interactuable.
    //
    // Va en el mismo objeto que el IInteractuable (una taza que solo se sabotea
    // con detergente, un NPC al que hay que entregarle algo). Los interactuables
    // lo consultan antes de dejarse usar, y el prompt dice que falta.
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

        // True si el jugador cumple el requisito ahora mismo.
        public bool SeCumple()
        {
            // Sin item configurado no hay nada que exigir: el interactuable es libre.
            if (itemRequerido == null)
            {
                return true;
            }

            return BuscarSlot() != -1;
        }

        // Texto del prompt cuando falta el item.
        public string ObtenerTextoBloqueado()
        {
            string nombre = itemRequerido != null && !string.IsNullOrEmpty(itemRequerido.nombre)
                ? itemRequerido.nombre
                : "un objeto";

            return textoSinItem.Replace("{item}", nombre);
        }

        // Gasta el item si asi esta configurado. Se llama al completar la accion,
        // no al empezarla: un sabotaje que se cancela no debe costar el objeto.
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

        // Indice del slot donde esta el item requerido, o -1 si no lo lleva.
        private int BuscarSlot()
        {
            InventarioJugador inventario = InventarioJugador.DelJugador();

            return inventario != null
                ? inventario.BuscarSlotCon(itemRequerido, debeEstarEnLaMano)
                : -1;
        }
    }
}
