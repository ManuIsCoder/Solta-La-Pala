using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Objeto del mundo que se puede recoger y guardar en el inventario.
    [RequireComponent(typeof(ResaltadoInteractuable))]
    public class ObjetoRecogible : MonoBehaviour, IInteractuable
    {
        public DatosItem item;

        public Transform Transform => transform;

        // Devuelve "[E] Recoger {nombre del item}".
        public string ObtenerTextoInteraccion()
        {
            return string.Empty;
        }

        // Solo se puede recoger si el inventario del jugador tiene algun slot libre.
        public bool PuedeInteractuar()
        {
            return false;
        }

        // Mete el item en el inventario del jugador y destruye este objeto del mundo.
        public void Interactuar(GameObject quienInteractua)
        {
        }
    }
}
