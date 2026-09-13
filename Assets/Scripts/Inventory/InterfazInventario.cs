using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Inventory
{
    // Dibuja los 3 slots del inventario como cuadrados abajo a la derecha de la pantalla
    // y resalta el que esta seleccionado.
    public class InterfazInventario : MonoBehaviour
    {
        [Header("Referencias")]
        public InventarioJugador inventario;
        [Tooltip("Los 3 cuadrados del inventario, en orden 1-2-3.")]
        public SlotInterfaz[] slots = new SlotInterfaz[InventarioJugador.CantidadSlots];

        // Se suscribe a los eventos del inventario para refrescar la UI solo cuando algo cambia.
        private void OnEnable()
        {
        }

        // Se desuscribe de los eventos del inventario.
        private void OnDisable()
        {
        }

        // Redibuja un slot concreto: pone el icono del item o, si esta vacio, esconde el icono.
        private void RefrescarSlot(int indiceSlot)
        {
        }

        // Marca visualmente que slot esta seleccionado y apaga el resto.
        // El slot seleccionado se resalta aunque este vacio.
        private void RefrescarSeleccion(int indiceSeleccionado)
        {
        }
    }

    // Un cuadrado individual del inventario: el icono del item y el marco de seleccion.
    [System.Serializable]
    public class SlotInterfaz
    {
        public Image icono;
        public GameObject marcoSeleccion;
    }
}
