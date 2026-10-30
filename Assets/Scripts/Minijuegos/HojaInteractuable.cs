using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Minijuegos
{
    /// <summary>
    /// Interactuable para la hoja de papel donde se redactan documentos (Nivel 3).
    /// Posee un switch de estado ("Activado" / "Desactivado") editable desde el Inspector.
    /// Requiere que el jugador tenga la Birome o las Crayolas en el inventario para poder interactuar.
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

        [Header("IDs de ítems necesarios")]
        public string idBirome = "Birome";
        public string idCrayolas = "Crayolas";

        public Transform Transform => transform;

        private void Awake()
        {
            if (minijuego == null)
            {
                minijuego = FindFirstObjectByType<MinijuegoRedactarDocumento>(FindObjectsInactive.Include);
            }
        }

        private bool JugadorTieneItemNecesario(GameObject quien)
        {
            if (minijuego != null && minijuego.modoPrueba) return true;

            var jugador = quien != null ? quien : GameObject.FindGameObjectWithTag("Player");
            if (jugador == null) return false;

            var inv = jugador.GetComponent<InventarioJugador>();
            if (inv == null) return false;

            return inv.TieneItem(idBirome) || inv.TieneItem(idCrayolas);
        }

        public string ObtenerTextoInteraccion()
        {
            if (estado == EstadoSwitch.Desactivado)
                return string.Empty;

            if (!JugadorTieneItemNecesario(null))
            {
                return "[E] Redactar documento (Requiere Birome o Crayolas)";
            }

            return "[E] Redactar documento";
        }

        public bool PuedeInteractuar()
        {
            if (estado != EstadoSwitch.Activado) return false;
            if (GestorMinijuego.Instancia == null || GestorMinijuego.Instancia.MinijuegoActivo) return false;

            return true;
        }

        public void Interactuar(GameObject quienInteractua)
        {
            if (estado == EstadoSwitch.Desactivado) return;

            if (!JugadorTieneItemNecesario(quienInteractua))
            {
                Debug.Log("[HojaInteractuable] No tienes Birome ni Crayolas en el inventario.");
                return;
            }

            var inv = quienInteractua != null ? quienInteractua.GetComponent<InventarioJugador>() : null;

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
