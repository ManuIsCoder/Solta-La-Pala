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

        // Devuelve "[E] Hablar con {nombre}".
        public string ObtenerTextoInteraccion()
        {
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
        public void Interactuar(GameObject quienInteractua)
        {
            if (GestorDialogos.Instancia != null)
            {
                GestorDialogos.Instancia.EmpezarDialogo(this, tipoActual);
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
