using SoltaLaPala.Guardado;
using SoltaLaPala.Interaction;
using UnityEngine;

namespace SoltaLaPala.Sabotaje
{
    // Objeto Taza / Cafetera interactuable que abre el menu de sabotaje.
    [RequireComponent(typeof(ResaltadoInteractuable))]
    [RequireComponent(typeof(IdentificadorObjeto))]
    public class SabotajeTazaInteractuable : MonoBehaviour, IInteractuable
    {
        public Transform Transform => transform;

        // Requisito opcional: si hay un RequisitoItem al lado, hace falta llevar
        // ese objeto para poder sabotear.
        private RequisitoItem requisito;

        private void Awake()
        {
            requisito = GetComponent<RequisitoItem>();
        }

        public string ObtenerTextoInteraccion()
        {
            if (requisito != null && !requisito.SeCumple())
            {
                return requisito.ObtenerTextoBloqueado();
            }

            return "[E] Sabotear Café";
        }

        public bool PuedeInteractuar()
        {
            if (MenuSabotajeTaza.Instancia != null && MenuSabotajeTaza.Instancia.panel != null)
            {
                return !MenuSabotajeTaza.Instancia.panel.activeSelf;
            }
            return true;
        }

        // True si el jugador lleva lo que hace falta. Se separa de PuedeInteractuar
        // para que el objeto siga resaltandose y mostrando que le falta, en vez de
        // volverse invisible al sistema de interaccion.
        public bool CumpleRequisitos()
        {
            return requisito == null || requisito.SeCumple();
        }

        public void Interactuar(GameObject quienInteractua)
        {
            if (!CumpleRequisitos())
            {
                return;
            }

            if (MenuSabotajeTaza.Instancia != null)
            {
                MenuSabotajeTaza.Instancia.Abrir(gameObject, requisito);
            }
            else
            {
                Debug.LogWarning("[SabotajeTazaInteractuable] No se encontró MenuSabotajeTaza en la escena.");
            }
        }
    }
}
