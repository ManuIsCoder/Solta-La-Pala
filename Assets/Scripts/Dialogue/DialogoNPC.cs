using SoltaLaPala.Interaction;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    [RequireComponent(typeof(ResaltadoInteractuable))]
    public class DialogoNPC : MonoBehaviour, IInteractuable
    {
        [Header("Identidad")]
        [Tooltip("Nombre que sale en el cuadro de dialogo.")]
        public string nombre = "Compañero";
        [Tooltip("Nombre de la carpeta del NPC dentro de Resources/Nivel/Dialogos/.")]
        public string id = "Compañero";

        [Header("Estado")]
        [Tooltip("Tipo de dialogo con el que arranca el NPC en esta escena.")]
        public TipoDialogo tipoActual = TipoDialogo.Mision;

        [Header("Sonido")]
        [Tooltip("Voz de balbuceo que suena mientras el NPC habla. Ninguna = no suena nada.")]
        public VozNPC voz = VozNPC.Agudo;
        [Tooltip("Sonido de estornudo / tos del NPC. Ninguna = no estornuda.")]
        public TosNPC sonidoToz = TosNPC.Toz1;
        [Tooltip("Cada cuántos segundos estornuda/tose como mínimo, cuando nadie le habla.")]
        public float tosCadaMinimo = 14f;
        [Tooltip("Cada cuántos segundos estornuda/tose como máximo.")]
        public float tosCadaMaximo = 24f;

        public Transform Transform => transform;

        private MisionEntrega mision;

        private bool yaEscuchoElEncargo;

        private TipoDialogo tipoInicial;

        private void Awake()
        {
            mision = GetComponent<MisionEntrega>();
            tipoInicial = tipoActual;

            if (GetComponent<SonidosNPC>() == null)
            {
                gameObject.AddComponent<SonidosNPC>();
            }
        }

        public void ReiniciarConversacion()
        {
            tipoActual = tipoInicial;
            yaEscuchoElEncargo = false;
        }

        public string ObtenerTextoInteraccion()
        {
            if (mision != null && mision.HayEntregaPosible())
            {
                return $"[E] Entregar a {nombre}";
            }

            return $"[E] Hablar con {nombre}";
        }

        public bool PuedeInteractuar()
        {
            if (GestorDialogos.Instancia != null && GestorDialogos.Instancia.DialogoActivo)
                return false;

            return CargadorDialogos.TieneDialogo(id, tipoActual) || CargadorDialogos.TieneDialogo(id, TipoDialogo.Perpetuo);
        }

        public void Interactuar(GameObject quienInteractua)
        {
            if (GestorDialogos.Instancia == null)
            {
                return;
            }

            if (EncargoPendiente() && yaEscuchoElEncargo)
            {
                if (MenuEntrega.Instancia != null && mision.HayEntregaPosible()
                    && MenuEntrega.Instancia.Abrir(mision))
                {
                    return;
                }

                TipoDialogo recordatorio = CargadorDialogos.TieneDialogo(id, TipoDialogo.Apurate)
                    ? TipoDialogo.Apurate
                    : TipoDialogo.Mision;

                GestorDialogos.Instancia.EmpezarDialogo(this, recordatorio);
                return;
            }

            GestorDialogos.Instancia.EmpezarDialogo(this, tipoActual);
        }

        private bool EncargoPendiente()
        {
            return mision != null && !mision.Completada && tipoActual == TipoDialogo.Mision;
        }

        public void AlCompletarEncargo()
        {
            if (tipoActual == TipoDialogo.Mision)
            {
                tipoActual = ObtenerSiguienteTipo(TipoDialogo.Mision);
            }
        }

        public void DispararDetectado()
        {
            if (GestorDialogos.Instancia != null && !GestorDialogos.Instancia.DialogoActivo)
            {
                GestorDialogos.Instancia.EmpezarDialogo(this, TipoDialogo.Detectado);
            }
        }

        public void AlTerminarDialogo(TipoDialogo tipoTerminado)
        {
            if (tipoTerminado == TipoDialogo.Mision && mision != null && !mision.Completada)
            {
                yaEscuchoElEncargo = true;
                return;
            }

            if (tipoTerminado == TipoDialogo.Apurate)
            {
                return;
            }

            tipoActual = ObtenerSiguienteTipo(tipoTerminado);
        }

        private TipoDialogo ObtenerSiguienteTipo(TipoDialogo tipoTerminado)
        {
            switch (tipoTerminado)
            {
                case TipoDialogo.Mision:
                    if (CargadorDialogos.TieneDialogo(id, TipoDialogo.Charla))
                        return TipoDialogo.Charla;
                    return TipoDialogo.Perpetuo;

                case TipoDialogo.Charla:
                    return TipoDialogo.Perpetuo;

                case TipoDialogo.Perpetuo:
                    return TipoDialogo.Perpetuo;

                case TipoDialogo.Detectado:
                    return tipoActual;

                default:
                    return TipoDialogo.Perpetuo;
            }
        }
    }
}
