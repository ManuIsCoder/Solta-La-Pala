using SoltaLaPala.Interaction;
using UnityEngine;

namespace SoltaLaPala.Player
{
    // Las tres piezas del jugador que hay que congelar a la vez cuando algo toma
    // la pantalla: un menu, un dialogo o un panel.
    //
    // Existe porque ese trio (buscar las referencias, comprobar null, llamar a los
    // tres Bloquear) estaba copiado en cuatro sitios, y cada copia habia derivado
    // un poco: una se olvidaba del cursor, otra del contador de UI.
    //
    // No es un MonoBehaviour: se guarda como campo en quien lo necesite.
    public class ControlJugador
    {
        private MovimientoJugador movimiento;
        private CamaraTerceraPersona camara;
        private InteractorJugador interactor;

        // Congela o libera movimiento, camara e interaccion.
        //
        // avisarUi decide si ademas se apunta en el contador que la camara consulta
        // para no girar con el raton. Los menus no lo usan: GestorMenus ya se
        // consulta aparte, y contarlo dos veces dejaria la camara trabada.
        public void Bloquear(bool bloqueado, bool avisarUi = true)
        {
            BuscarReferencias();

            if (movimiento != null)
            {
                movimiento.BloquearMovimiento(bloqueado);
            }

            if (interactor != null)
            {
                interactor.BloquearInteraccion(bloqueado);
            }

            // La camara va la ultima: BloquearCamara tambien toca el cursor, asi
            // el estado final del cursor es el que ella deja.
            if (camara != null)
            {
                camara.BloquearCamara(bloqueado);
            }
            else
            {
                // Sin camara nadie libera el cursor, y un panel sin raton no se
                // puede usar.
                Cursor.lockState = bloqueado ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = bloqueado;
            }

            if (avisarUi)
            {
                CamaraTerceraPersona.RegistrarUiAbierta(bloqueado);
            }
        }

        // Se reintenta cada vez porque el jugador puede no existir todavia (menu
        // principal al arrancar) o haber sido recreado.
        private void BuscarReferencias()
        {
            if (movimiento == null)
                movimiento = Object.FindFirstObjectByType<MovimientoJugador>();
            if (camara == null)
                camara = Object.FindFirstObjectByType<CamaraTerceraPersona>();
            if (interactor == null)
                interactor = Object.FindFirstObjectByType<InteractorJugador>();
        }
    }
}
