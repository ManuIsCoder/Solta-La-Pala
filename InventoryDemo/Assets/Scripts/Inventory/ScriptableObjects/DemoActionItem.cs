using UnityEngine;

// Item de prueba simple, sin dependencias de Animator/PlayerMovement/Physics2D,
// para poder probar el equipar (teclas 1/2/3) de la ActionBar sin armar animaciones.
[CreateAssetMenu(fileName = "DemoActionItem", menuName = "ScriptableObjects/Item/DemoAction", order = 2)]
public class DemoActionItem : ActionItem
{
    public override void Equip(GameObject player) => Debug.Log($"[Inventory Demo] Equipando: {Name}");
    public override void Unequip(GameObject player) => Debug.Log($"[Inventory Demo] Desequipando: {Name}");
    public override void TriggerAction(GameObject player) => Debug.Log($"[Inventory Demo] Usando: {Name}");
}
