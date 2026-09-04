using UnityEngine;
using UnityEngine.EventSystems;

// El repo original (Deme94/InventorySystem) tiene la logica de Drag/Drop en
// Inventory.cs y ActionBar.cs, pero no incluye el componente que conecta el
// mouse con esos metodos. Este script es ese enganche: se pone en cada slot
// de la UI y llama a Drag/Drop del contenedor (Inventory o ActionBar) al que
// pertenece ese slot.
public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public MonoBehaviour containerBehaviour; // Inventory o ActionBar (ambos implementan IItemContainer)
    public int slotIndex;
    public DragAndDrop dragAndDrop;

    private IItemContainer Container => containerBehaviour as IItemContainer;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[Inventory Demo] OnBeginDrag en {name} (slot {slotIndex})");
        Container.Drag(slotIndex);
    }

    // Vacio a proposito: el icono ya sigue al mouse solo por DragAndDrop.Update().
    // Esta implementacion vacia es necesaria para que Unity trate esto como un
    // drag real y le mande el OnDrop al slot correcto al soltar.
    public void OnDrag(PointerEventData eventData) { }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[Inventory Demo] OnDrop en {name} (slot {slotIndex})");
        Container.Drop(slotIndex);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[Inventory Demo] OnEndDrag en {name} (slot {slotIndex}), DraggedItem={(dragAndDrop.DraggedItem != null ? dragAndDrop.DraggedItem.Name : "null")}");
        // Si soltaste afuera de cualquier slot valido, no hubo OnDrop:
        // cancelamos y el item vuelve a su lugar de origen.
        if (dragAndDrop.DraggedItem != null)
            dragAndDrop.Drop(false);
    }
}
