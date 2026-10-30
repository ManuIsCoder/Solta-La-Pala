using SoltaLaPala.Interaction;
using UnityEngine;

namespace SoltaLaPala.Player
{
    public class ControlJugador
    {
        private MovimientoJugador movimiento;
        private CamaraTerceraPersona camara;
        private InteractorJugador interactor;

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

            if (camara != null)
            {
                camara.BloquearCamara(bloqueado);
            }
            else
            {
                Cursor.lockState = bloqueado ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = bloqueado;
            }

            if (avisarUi)
            {
                CamaraTerceraPersona.RegistrarUiAbierta(bloqueado);
            }
        }

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
