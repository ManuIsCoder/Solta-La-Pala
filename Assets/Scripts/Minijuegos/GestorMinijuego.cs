using UnityEngine;
using SoltaLaPala.Menus;
using SoltaLaPala.Player;
using SoltaLaPala.Inventory;

namespace SoltaLaPala.Minijuegos
{

    public class GestorMinijuego : MonoBehaviour
    {

        public static GestorMinijuego Instancia { get; private set; }
        [Header("HUD a ocultar durante el minijuego")]
        [Tooltip("Arrastrá aquí todos los GameObjects del HUD que deben ocultarse " + "(inventario, ojo de visión, barra de sospecha, etc). " + "El HudTiempo NO va aquí: ese debe seguir visible.")]
        public GameObject[] elementosHudAOcultar;
        public bool MinijuegoActivo { get; private set; }
        private MinijuegoBase minijuegoActual;
        private readonly ControlJugador controlJugador = new ControlJugador();

        private void Awake()
        {
            if (Instancia != null && Instancia != this )
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
        }

        public void AbrirMinijuego(MinijuegoBase minijuego, InventarioJugador inventario)
        {
            if(MinijuegoActivo) return;

            minijuegoActual = minijuego;
            MinijuegoActivo = true;

            OcultarHud(true);
            controlJugador.Bloquear(true);

            minijuego.Iniciar(inventario, this);
        }

        // Cierra el minijuego abierto, si lo hay. Lo usa el reinicio de nivel: si se
        // acaba el tiempo con uno abierto, sin esto el jugador seguiria bloqueado y
        // el HUD oculto en el nivel nuevo.
        public void ForzarCierre()
        {
            if (!MinijuegoActivo)
            {
                return;
            }

            if (minijuegoActual != null)
            {
                minijuegoActual.Salir();
            }

            // Salir() ya llama a CerrarMinijuego; esto cubre que no hubiera minijuego.
            CerrarMinijuego();
        }

        public void CerrarMinijuego()
        {
            if(!MinijuegoActivo) return;

            MinijuegoActivo = false;
            minijuegoActual = null;
            
            OcultarHud(false);
            controlJugador.Bloquear(false);
        }

        private void OcultarHud(bool ocultar)
        {
            foreach (var elemento in elementosHudAOcultar)
            {
                if (elemento != null)
                    elemento.SetActive(!ocultar);
            }
        }
    }
}