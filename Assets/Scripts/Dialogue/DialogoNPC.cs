using SoltaLaPala.Interaction;
using SoltaLaPala.Menus;
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
        private TareaNivel tareaNivel;

        private bool yaEscuchoElEncargo;

        private TipoDialogo tipoInicial;

        private void Awake()
        {
            mision = GetComponent<MisionEntrega>();
            tareaNivel = GetComponent<TareaNivel>();
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

        public void EstablecerTipoDialogoInicial(TipoDialogo tipo)
        {
            tipoInicial = tipo;
            tipoActual = tipo;
            yaEscuchoElEncargo = false;
        }

        public string ObtenerTextoInteraccion()
        {
            MisionEntrega misionActual = ObtenerMision();
            if (misionActual != null && misionActual.HayEntregaPosible())
            {
                return $"[E] Entregar a {nombre}";
            }

            return $"[E] Hablar con {nombre}";
        }

        public bool PuedeInteractuar()
        {
            if (GestorDialogos.Instancia != null && GestorDialogos.Instancia.DialogoActivo)
                return false;

            if (tareaNivel == null)
            {
                tareaNivel = GetComponent<TareaNivel>();
            }

            if (tareaNivel != null)
            {
                return true;
            }

            return CargadorDialogos.TieneDialogo(id, tipoActual) || CargadorDialogos.TieneDialogo(id, TipoDialogo.Perpetuo);
        }

        public void Interactuar(GameObject quienInteractua)
        {
            if (GestorDialogos.Instancia == null)
            {
                return;
            }

            if (tareaNivel == null)
            {
                tareaNivel = GetComponent<TareaNivel>();
            }

            if (tareaNivel != null && !tareaNivel.Aceptada)
            {
                tareaNivel.Aceptar();
                GestorDialogos.Instancia.MostrarFraseSuelta(this, tareaNivel.ObtenerDialogo());
                return;
            }

            if (tareaNivel != null && tareaNivel.Completada)
            {
                GestorDialogos.Instancia.MostrarFraseSuelta(this, tareaNivel.ObtenerDialogo());
                return;
            }

            MisionEntrega misionActual = ObtenerMision();
            if (tareaNivel != null && misionActual == null)
            {
                GestorDialogos.Instancia.MostrarFraseSuelta(this, tareaNivel.ObtenerDialogo());
                return;
            }

            if (EncargoPendiente(misionActual) && yaEscuchoElEncargo)
            {
                if (MenuEntrega.Instancia != null && misionActual.HayEntregaPosible()
                    && MenuEntrega.Instancia.Abrir(misionActual))
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

        private bool EncargoPendiente(MisionEntrega misionActual)
        {
            return misionActual != null && !misionActual.Completada && tipoActual == TipoDialogo.Mision;
        }

        private MisionEntrega ObtenerMision()
        {
            if (mision == null)
            {
                mision = GetComponent<MisionEntrega>();
            }

            return mision;
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
            MisionEntrega misionActual = ObtenerMision();
            if (tipoTerminado == TipoDialogo.Mision && misionActual != null && !misionActual.Completada)
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
