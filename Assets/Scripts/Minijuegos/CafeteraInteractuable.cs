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
        public string ObtenerTextoInteraccion()
        {
            if (estado == EstadoSwitch.Desactivado || minijuego != null && minijuego.Completado)
            {
                return string.Empty;
            }

            InventarioJugador inventario = InventarioJugador.DelJugador();
            return inventario == null || minijuego != null
                && !inventario.TieneItem(minijuego.idTaza) && !minijuego.TazaEnCafetera
                ? "Necesitas una taza"
                : "[E] Usar cafetera";
        }

        public bool PuedeInteractuar()
        {
            InventarioJugador inventario = InventarioJugador.DelJugador();
            return estado == EstadoSwitch.Activado
                && minijuego != null
                && !minijuego.Completado
                && inventario != null
                && (inventario.TieneItem(minijuego.idTaza) || minijuego.TazaEnCafetera)
                && GestorMinijuego.Instancia != null
                && !GestorMinijuego.Instancia.MinijuegoActivo;
        }

        public void Interactuar(GameObject quienInteractua)
        {
            if (!PuedeInteractuar()) return;

            var inv = quienInteractua.GetComponent<InventarioJugador>();
            GestorMinijuego.Instancia?.AbrirMinijuego(minijuego, inv);
        }
    }
}
