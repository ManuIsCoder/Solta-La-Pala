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
        public string nombre;
        [Tooltip("Nombre de la carpeta del NPC dentro de Resources/Nivel/Dialogos/.")]
        public string id;

        [Header("Estado")]
        [Tooltip("Tipo de dialogo con el que arranca el NPC en esta escena.")]
        public TipoDialogo tipoActual = TipoDialogo.Charla;

        public Transform Transform => transform;

        // Devuelve "[E] Hablar con {nombre}".
        public string ObtenerTextoInteraccion()
        {
            return string.Empty;
        }

        // Se puede hablar si el NPC tiene alguna linea cargada y no hay otro dialogo activo.
        public bool PuedeInteractuar()
        {
            return false;
        }

        // Arranca la conversacion pidiendole al GestorDialogos que muestre
        // el dialogo del tipo actual.
        public void Interactuar(GameObject quienInteractua)
        {
        }

        // Fuerza el dialogo de tipo Detectado (lo llama el sistema de vision del NPC).
        // No cambia el tipo actual: al acabar se vuelve al que estaba.
        public void DispararDetectado()
        {
        }

        // Lo llama el GestorDialogos cuando termina una conversacion.
        // Aplica la transicion de tipos:
        // - Mision  -> Charla si existe, si no Perpetuo
        // - Charla  -> Perpetuo
        // - Perpetuo -> se queda en Perpetuo (bucle)
        // - Detectado -> no cambia nada
        public void AlTerminarDialogo(TipoDialogo tipoTerminado)
        {
        }

        // Calcula a que tipo de dialogo hay que pasar despues del que acaba de terminar.
        private TipoDialogo ObtenerSiguienteTipo(TipoDialogo tipoTerminado)
        {
            return TipoDialogo.Perpetuo;
        }
    }
}
