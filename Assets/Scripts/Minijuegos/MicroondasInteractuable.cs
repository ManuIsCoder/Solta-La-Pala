using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Minijuegos
{
    [RequireComponent(typeof(ResaltadoInteractuable))]
    public class MicroondasInteractuable : MonoBehaviour, IInteractuable
    {
        public MinijuegoMicroondas minijuego;

        public Transform Transform => transform;
        public string ObtenerTextoInteraccion() => "[E] Usar microondas";
        public bool PuedeInteractuar() => GestorMinijuego.Instancia != null && !GestorMinijuego.Instancia.MinijuegoActivo;

        public void Interactuar(GameObject quienInteractua)
        {
            var inv = quienInteractua.GetComponent<InventarioJugador>();
            GestorMinijuego.Instancia?.AbrirMinijuego(minijuego, inv);
        }
    }
}