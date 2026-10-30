using SoltaLaPala.Guardado;
using SoltaLaPala.Interaction;
using UnityEngine;

namespace SoltaLaPala.Sabotaje
{
    [RequireComponent(typeof(ResaltadoInteractuable))]
    [RequireComponent(typeof(IdentificadorObjeto))]
    public class SabotajeTazaInteractuable : MonoBehaviour, IInteractuable
    {
        public Transform Transform => transform;

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
