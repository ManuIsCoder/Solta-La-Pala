using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Minijuegos
{
    /// <summary>
    /// Interactuable para la hoja de papel donde se redactan documentos (Nivel 3).
    /// Posee un switch de estado ("Activado" / "Desactivado") editable desde el Inspector.
    /// </summary>
    [RequireComponent(typeof(ResaltadoInteractuable))]
    public class HojaInteractuable : MonoBehaviour, IInteractuable
    {
        public enum EstadoSwitch
        {
            Activado,
            Desactivado
        }

        [Header("Interruptor del minijuego")]
        [Tooltip("Switch de Activado / Desactivado: habilita o deshabilita la interacción con este objeto")]
        public EstadoSwitch estado = EstadoSwitch.Activado;

        [Header("Referencia al Minijuego")]
        [Tooltip("Minijuego de redactar documento")]
        public MinijuegoRedactarDocumento minijuego;

        public Transform Transform => transform;

        public string ObtenerTextoInteraccion()
        {
            if (estado == EstadoSwitch.Desactivado)
                return string.Empty;

            return "[E] Redactar documento";
        }

        public bool PuedeInteractuar()
        {
            return estado == EstadoSwitch.Activado
                && GestorMinijuego.Instancia != null
                && !GestorMinijuego.Instancia.MinijuegoActivo;
        }

        public void Interactuar(GameObject quienInteractua)
        {
            if (estado == EstadoSwitch.Desactivado) return;

            var inv = quienInteractua.GetComponent<InventarioJugador>();

            if (minijuego == null)
            {
                minijuego = FindObjectOfType<MinijuegoRedactarDocumento>(true);
            }

            if (minijuego != null)
            {
                GestorMinijuego.Instancia?.AbrirMinijuego(minijuego, inv);
            }
            else
            {
                Debug.LogWarning("[HojaInteractuable] MinijuegoRedactarDocumento no está asignado ni se encontró en la escena.");
            }
        }
    }
}
