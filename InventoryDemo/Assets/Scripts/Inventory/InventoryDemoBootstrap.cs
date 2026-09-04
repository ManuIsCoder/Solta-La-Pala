using UnityEngine;

// Carga un par de items de ejemplo en el inventario al arrancar la escena de prueba,
// asi hay algo para arrastrar a la ActionBar apenas se le da Play.
public class InventoryDemoBootstrap : MonoBehaviour
{
    public Inventory inventory;
    public Item[] starterItems;
    public int[] starterQuantities;

    void Start()
    {
        if (inventory == null || starterItems == null) return;

        for (int i = 0; i < starterItems.Length; i++)
        {
            int quantity = (starterQuantities != null && i < starterQuantities.Length) ? starterQuantities[i] : 1;
            inventory.Add(starterItems[i], quantity);
        }
    }
}
