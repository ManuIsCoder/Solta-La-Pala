using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Inventory
{
    // Dibuja los 3 slots del inventario como cuadrados abajo a la derecha de la pantalla
    // y resalta el que esta seleccionado.
    public class InventoryUI : MonoBehaviour
    {
        [Header("Referencias")]
        public PlayerInventory inventory;
        [Tooltip("Los 3 cuadrados del inventario, en orden 1-2-3.")]
        public InventorySlotUI[] slotWidgets = new InventorySlotUI[PlayerInventory.SlotCount];

        // Se suscribe a los eventos del inventario para refrescar la UI solo cuando algo cambia.
        private void OnEnable()
        {
        }

        // Se desuscribe de los eventos del inventario.
        private void OnDisable()
        {
        }

        // Redibuja un slot concreto: pone el icono del item o, si esta vacio, esconde el icono.
        private void RefreshSlot(int slotIndex)
        {
        }

        // Marca visualmente que slot esta seleccionado y apaga el resto.
        // El slot seleccionado se resalta aunque este vacio.
        private void RefreshSelection(int selectedIndex)
        {
        }

        // Redibuja los 3 slots y la seleccion de golpe (al arrancar la escena).
        private void RefreshAll()
        {
        }
    }

    // Un cuadrado individual del inventario: fondo, icono del item y marco de seleccion.
    [System.Serializable]
    public class InventorySlotUI
    {
        public Image background;
        public Image iconImage;
        public GameObject selectionFrame;
    }
}
