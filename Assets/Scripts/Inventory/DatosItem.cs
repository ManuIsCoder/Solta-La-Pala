using UnityEngine;

namespace SoltaLaPala.Inventory
{
    // Datos estaticos de un item: como se llama, su icono en el slot
    // y el prefab que se instancia en la mano cuando esta sujeto.
    [CreateAssetMenu(fileName = "NuevoItem", menuName = "Solta La Pala/Item")]
    public class DatosItem : ScriptableObject
    {
        public string nombre;
        public Sprite icono;
        public GameObject prefabEnMano;
        [TextArea] public string descripcion;
    }
}
