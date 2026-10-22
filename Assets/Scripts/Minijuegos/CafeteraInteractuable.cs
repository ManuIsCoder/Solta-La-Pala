using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Minijuegos
{
    [RequireComponent(typeof(ResaltadoInteractuable))]
    public class CafeteraInteractuable : MonoBehaviour, IInteractuable
    {
        public MinijuegoCafetera minijuego;

        public Transform Transform => transform;
        public string ObtenerTextoInteraccion() => "[E] Usar cafetera";
        public bool PuedeInteractuar() => GestorMinijuego.Instancia != null && !GestorMinijuego.Instancia.MinijuegoActivo;

        public void Interactuar(GameObject quienInteractua)
        {
            var inv = quienInteractua.GetComponent<InventarioJugador>();
            GestorMinijuego.Instancia?.AbrirMinijuego(minijuego, inv);
        }
    }
}