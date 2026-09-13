using System;
using UnityEngine;

namespace SoltaLaPala.Inventory
{
    // Inventario del jugador estilo PEAK / hotbar de Minecraft: 3 slots fijos,
    // se cambia de slot con las teclas 1, 2 y 3.
    // Siempre hay un slot seleccionado, pero ese slot puede estar vacio:
    // al usar o soltar un item el slot se queda vacio y el PJ queda con las manos vacias.
    // Solo cuando los 3 slots estan llenos es imposible tener las manos vacias.
    public class InventarioJugador : MonoBehaviour
    {
        public const int CantidadSlots = 3;

        [Header("Mano")]
        [Tooltip("Punto delante del jugador donde flota el item sujeto (luego sera el hueso de la mano).")]
        public Transform anclaMano;

        private readonly DatosItem[] slots = new DatosItem[CantidadSlots];
        private int slotSeleccionado = 0;
        private GameObject instanciaEnMano;

        // Se dispara cuando cambia el contenido de un slot. Parametro: indice del slot.
        public event Action<int> AlCambiarSlot;

        // Se dispara cuando cambia el slot seleccionado. Parametro: nuevo indice.
        public event Action<int> AlCambiarSeleccion;

        public int SlotSeleccionado => slotSeleccionado;

        // Item del slot seleccionado, o null si ese slot esta vacio (manos vacias).
        public DatosItem ItemSeleccionado => slots[slotSeleccionado];

        // Lee las teclas 1/2/3 y cambia el slot seleccionado.
        private void Update()
        {
        }

        // Mete el item en el slot seleccionado si esta libre; si no, en el primer slot vacio.
        // Devuelve false si los 3 slots estan ocupados.
        // Si acaba en el slot seleccionado, el item pasa directamente a la mano.
        public bool IntentarAgregarItem(DatosItem item)
        {
            return false;
        }

        // Vacia el slot y devuelve el item que habia (para soltarlo, usarlo o darselo a un NPC).
        // La seleccion no se mueve: si era el slot seleccionado, el PJ queda con las manos vacias.
        public DatosItem QuitarItem(int indiceSlot)
        {
            return null;
        }

        // Selecciona el slot indicado y actualiza lo que hay en la mano.
        // Ignora indices fuera de rango.
        public void SeleccionarSlot(int indiceSlot)
        {
        }

        // Devuelve el item de un slot, o null si esta vacio.
        public DatosItem ObtenerItem(int indiceSlot)
        {
            return null;
        }

        // True si los 3 slots estan ocupados (no se puede recoger nada mas).
        public bool EstaLleno()
        {
            return false;
        }

        // Devuelve el primer indice de slot vacio, o -1 si no hay ninguno.
        private int ObtenerPrimerSlotVacio()
        {
            return -1;
        }

        // Sincroniza la mano con el slot seleccionado: destruye la instancia anterior y,
        // si el slot tiene item, instancia su prefab en la anclaMano
        // (de momento flotando delante del PJ; mas adelante disparara la animacion de sujetar).
        // Si el slot esta vacio simplemente deja las manos vacias.
        private void ActualizarItemEnMano()
        {
        }
    }
}
