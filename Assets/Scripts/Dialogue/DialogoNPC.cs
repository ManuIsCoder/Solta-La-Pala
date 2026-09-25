using SoltaLaPala.Interaction;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    // Componente que va en cada NPC. Guarda que tipo de dialogo le toca decir ahora
    // y se encarga de la transicion entre tipos cuando una conversacion termina.
    // Es tambien el punto de interaccion del NPC ("[E] Hablar con ...").
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

        public Transform Transform => transform;

        // Encargo opcional. Si lo hay, el dialogo de Mision no avanza hasta que
        // le lleves algo: ni lo correcto ni un sustituto.
        private MisionEntrega mision;

        // True desde que el jugador escucho el encargo una vez. A partir de ahi
        // no hace falta repetirselo entero cada vez que se acerca.
        private bool yaEscuchoElEncargo;

        // Tipo con el que arranco el NPC en la escena, para poder volver a el al
        // reiniciar el nivel.
        private TipoDialogo tipoInicial;

        private void Awake()
        {
            mision = GetComponent<MisionEntrega>();
            tipoInicial = tipoActual;
        }

        // Deja al NPC como al empezar el nivel: su dialogo inicial y sin recordar
        // que ya te conto el encargo.
        public void ReiniciarConversacion()
        {
            tipoActual = tipoInicial;
            yaEscuchoElEncargo = false;
        }

        // Devuelve "[E] Hablar con {nombre}", o el texto de entrega si le puedes
        // dar lo que te pidio.
        public string ObtenerTextoInteraccion()
        {
            if (mision != null && mision.HayEntregaPosible())
            {
                return $"[E] Entregar a {nombre}";
            }

            return $"[E] Hablar con {nombre}";
        }

        // Se puede hablar si el NPC tiene alguna linea cargada y no hay otro dialogo activo.
        public bool PuedeInteractuar()
        {
            if (GestorDialogos.Instancia != null && GestorDialogos.Instancia.DialogoActivo)
                return false;

            return CargadorDialogos.TieneDialogo(id, tipoActual) || CargadorDialogos.TieneDialogo(id, TipoDialogo.Perpetuo);
        }

        // Arranca la conversacion pidiendole al GestorDialogos que muestre
        // el dialogo del tipo actual.
        //
        // Con un encargo pendiente el flujo cambia a partir de la segunda visita:
        // si llevas algo que sirve se abre el menu de entrega directamente, y si
        // no, el NPC te mete prisa en vez de repetirte el encargo entero.
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

                // Sin archivo de Apurate se repite el encargo. Dejarlo caer al
                // Perpetuo del gestor sonaria a "ya te lo conte todo" teniendo
                // una mision abierta.
                TipoDialogo recordatorio = CargadorDialogos.TieneDialogo(id, TipoDialogo.Apurate)
                    ? TipoDialogo.Apurate
                    : TipoDialogo.Mision;

                GestorDialogos.Instancia.EmpezarDialogo(this, recordatorio);
                return;
            }

            GestorDialogos.Instancia.EmpezarDialogo(this, tipoActual);
        }

        // True si este NPC tiene un encargo sin resolver.
        private bool EncargoPendiente()
        {
            return mision != null && !mision.Completada && tipoActual == TipoDialogo.Mision;
        }

        // Lo llama MisionEntrega al cerrarse el encargo: el NPC pasa a su
        // siguiente dialogo, que antes hacia AlTerminarDialogo.
        public void AlCompletarEncargo()
        {
            if (tipoActual == TipoDialogo.Mision)
            {
                tipoActual = ObtenerSiguienteTipo(TipoDialogo.Mision);
            }
        }

        // Fuerza el dialogo de tipo Detectado (lo llama el sistema de vision del NPC).
        // No cambia el tipo actual: al acabar se vuelve al que estaba.
        public void DispararDetectado()
        {
            if (GestorDialogos.Instancia != null && !GestorDialogos.Instancia.DialogoActivo)
            {
                GestorDialogos.Instancia.EmpezarDialogo(this, TipoDialogo.Detectado);
            }
        }

        // Lo llama el GestorDialogos cuando termina una conversacion.
        // Aplica la transicion de tipos:
        // - Mision  -> Charla si existe, si no Perpetuo
        // - Charla  -> Perpetuo
        // - Perpetuo -> se queda en Perpetuo (bucle)
        // - Detectado -> no cambia nada
        public void AlTerminarDialogo(TipoDialogo tipoTerminado)
        {
            // Escuchar el encargo no lo completa: hay que volver con algo y
            // elegirlo en el menu de entrega. El NPC se queda en Mision, pero ya
            // no repetira el encargo entero: a partir de ahora mete prisa.
            if (tipoTerminado == TipoDialogo.Mision && mision != null && !mision.Completada)
            {
                yaEscuchoElEncargo = true;
                return;
            }

            // Apurate es un recordatorio: no avanza el estado del NPC.
            if (tipoTerminado == TipoDialogo.Apurate)
            {
                return;
            }

            tipoActual = ObtenerSiguienteTipo(tipoTerminado);
        }

        // Calcula a que tipo de dialogo hay que pasar despues del que acaba de terminar.
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
                    // Detectado no avanza la historia ni el tipo
                    return tipoActual;

                default:
                    return TipoDialogo.Perpetuo;
            }
        }
    }
}
