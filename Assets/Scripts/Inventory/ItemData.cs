using UnityEngine;

namespace SoltaLaPala.Inventory
{
    // Datos estaticos de un item: como se llama, su icono en el slot
    // y el prefab que se instancia en la mano cuando esta sujeto.
    [CreateAssetMenu(fileName = "NuevoItem", menuName = "Solta La Pala/Item")]
    public class ItemData : ScriptableObject
    {
        public string itemName;
        public Sprite icon;
        public GameObject heldPrefab;
        [TextArea] public string description;

        public string ItemName => itemName;
        public Sprite Icon => icon;
        public GameObject HeldPrefab => heldPrefab;
        public string Description => description;
    }
}
