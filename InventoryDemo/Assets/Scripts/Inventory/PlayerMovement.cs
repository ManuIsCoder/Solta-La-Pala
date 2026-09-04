using UnityEngine;

// Stub minimo: el repo original (Deme94/InventorySystem) referencia esta clase
// desde MeleeWeaponItem.cs pero no la incluye. Sin esto el proyecto no compila.
// Cuando se integre la mecanica de movimiento real del jugador, reemplazar por esa.
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Vector2 _lookDirection = Vector2.down;

    public Vector2 LookDirection() => _lookDirection;
}
