using UnityEngine;

namespace SoltaLaPala.Inventory
{
    // Datos estaticos de un item: como se llama, su icono en el slot
    // y el prefab que se instancia en la mano cuando esta sujeto.
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

        // El guardado solo escribe este id, nunca una referencia al asset.
        public string Id => id;

#if UNITY_EDITOR
        // El nombre del asset ya es unico dentro de una carpeta y es estable,
        // asi que sirve de id sin pedirle nada al usuario.
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
