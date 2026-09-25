using SoltaLaPala.Guardado;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Objeto del mundo que se puede recoger y guardar en el inventario.
    // Necesita un IdentificadorObjeto para que el guardado recuerde que ya se recogio.
    [RequireComponent(typeof(ResaltadoInteractuable))]
    [RequireComponent(typeof(IdentificadorObjeto))]
    public class ObjetoRecogible : MonoBehaviour, IInteractuable
    {
        public DatosItem item;

        public Transform Transform => transform;

        // Devuelve "[E] Recoger {nombre del item}".
        public string ObtenerTextoInteraccion()
        {
            return item != null ? $"[E] Recoger {item.nombre}" : "[E] Recoger objeto";
        }

        // Solo se puede recoger si el inventario del jugador tiene algun slot libre.
        public bool PuedeInteractuar()
        {
            var jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
            {
                var inv = jugador.GetComponent<InventarioJugador>();
                if (inv != null && inv.EstaLleno()) return false;
            }
            return true;
        }

        // Mete el item en el inventario del jugador y destruye este objeto del mundo.
        public void Interactuar(GameObject quienInteractua)
        {
            if (quienInteractua != null)
            {
                var inv = quienInteractua.GetComponent<InventarioJugador>();
                if (inv != null && item != null)
                {
                    inv.IntentarAgregarItem(item);
                }
            }
            gameObject.SetActive(false);

            // Se anota antes de que el jugador pueda guardar, para que al cargar
            // el objeto no reaparezca en el suelo.
            if (RegistroObjetosConsumidos.Instancia != null)
            {
                RegistroObjetosConsumidos.Instancia.Marcar(gameObject);
            }
        }
    }
}
