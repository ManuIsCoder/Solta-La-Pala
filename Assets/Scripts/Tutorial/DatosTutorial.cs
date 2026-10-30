using UnityEngine;

namespace SoltaLaPala.Tutorial
{
    public enum CondicionPaso
    {
        SoloManual,

        AlCompletarMision,
    }

    [System.Serializable]
    public class PasoTutorial
    {
        [TextArea(2, 4)]
        public string texto;

        public CondicionPaso condicion;

        public string idMisionAsociada;
    }

    [CreateAssetMenu(fileName = "DatosTutorial", menuName = "SoltaLaPala/Tutorial")]
    public class DatosTutorial : ScriptableObject
    {
        [Header("Pasos del tutorial")]
        public PasoTutorial[] pasos;
    }
}
