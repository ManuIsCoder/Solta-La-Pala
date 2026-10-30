using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Minijuegos
{
    [RequireComponent(typeof(ResaltadoInteractuable))]
    public class CafeteraInteractuable : MonoBehaviour, IInteractuable
    {
        public enum EstadoSwitch
        {
            Activado,
            Desactivado
        }

        [Header("Interruptor del minijuego")]
        [Tooltip("Permite habilitar o deshabilitar el minijuego desde el Inspector")]
        public EstadoSwitch estado = EstadoSwitch.Activado;

        public MinijuegoCafetera minijuego;

        public Transform Transform => transform;
        public string ObtenerTextoInteraccion() => estado == EstadoSwitch.Activado ? "[E] Usar cafetera" : string.Empty;
        public bool PuedeInteractuar() => estado == EstadoSwitch.Activado && GestorMinijuego.Instancia != null && !GestorMinijuego.Instancia.MinijuegoActivo;

        public void Interactuar(GameObject quienInteractua)
        {
            if (estado == EstadoSwitch.Desactivado) return;

            var inv = quienInteractua.GetComponent<InventarioJugador>();
            GestorMinijuego.Instancia?.AbrirMinijuego(minijuego, inv);
        }
    }
}