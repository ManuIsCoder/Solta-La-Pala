using UnityEngine;

namespace SoltaLaPala.Tutorial
{
    /// <summary>
    /// Condición que determina cuándo un paso del tutorial se marca como completado.
    /// </summary>
    public enum CondicionPaso
    {
        /// <summary>Solo lo completa el jugador navegando con las flechas.</summary>
        SoloManual,

        /// <summary>Se marca automáticamente cuando la MisionEntrega asociada está completada.</summary>
        AlCompletarMision,
    }

    /// <summary>
    /// Datos de un paso individual del tutorial.
    /// </summary>
    [System.Serializable]
    public class PasoTutorial
    {
        [TextArea(2, 4)]
        public string texto;                    // Texto que se muestra en el panel

        public CondicionPaso condicion;         // Cómo se marca como completado

        /// <summary>
        /// Solo relevante si condicion == AlCompletarMision.
        /// Nombre del GameObject en escena que contiene el componente MisionEntrega.
        /// </summary>
        public string idMisionAsociada;
    }

    /// <summary>
    /// ScriptableObject que almacena todos los pasos del tutorial.
    /// Crear desde el menú: SoltaLaPala → Tutorial.
    /// </summary>
    [CreateAssetMenu(fileName = "DatosTutorial", menuName = "SoltaLaPala/Tutorial")]
    public class DatosTutorial : ScriptableObject
    {
        [Header("Pasos del tutorial")]
        public PasoTutorial[] pasos;
    }
}
