using UnityEngine;

namespace SoltaLaPala.Inventory
{
    [CreateAssetMenu(fileName = "NuevoItem", menuName = "Solta La Pala/Item")]
    public class DatosItem : ScriptableObject
    {
        [Tooltip("Identificador para el guardado. Se rellena solo con el nombre del asset. " +
                 "Si lo cambias, los guardados viejos dejan de reconocer este item.")]
        [SerializeField] private string id;

        public string nombre;
        public Sprite icono;
        public GameObject prefabEnMano;
        [TextArea] public string descripcion;

        [Tooltip("Si es true, llevar este item en la mano levantará sospechas si un NPC te ve.")]
        public bool esSospechoso = false;

        public string Id => id;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = name;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
