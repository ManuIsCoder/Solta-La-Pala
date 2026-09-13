using UnityEngine;

namespace SoltaLaPala.Player
{
    // Movimiento en tercera persona del jugador (sin salto).
    // El movimiento es relativo a la camara: W va siempre hacia donde mira la camara,
    // que a su vez gira con el raton (ver ThirdPersonCamera).
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movimiento")]
        public float walkSpeed = 4f;
        public float runSpeed = 7f;
        [Tooltip("Cuanto tarda el PJ en girar hacia la direccion en la que se mueve.")]
        public float rotationSmoothTime = 0.1f;
        public float gravity = -20f;

        [Header("Referencias")]
        [Tooltip("Camara que define el 'adelante'. Normalmente la que lleva el ThirdPersonCamera.")]
        public ThirdPersonCamera playerCamera;

        private CharacterController controller;
        private Vector3 verticalVelocity;
        private bool movementLocked;

        // Cachea el CharacterController y busca la camara si no fue asignada en el inspector.
        private void Awake()
        {
        }

        private void Update()
        {
            // 1. Si el movimiento esta bloqueado (dialogo), no leer input.
            // 2. Leer WASD con Input.GetAxisRaw("Horizontal") y ("Vertical").
            // 3. Convertir ese input a direccion de mundo con GetCameraRelativeDirection().
            // 4. Girar al PJ hacia esa direccion con RotateTowards().
            // 5. Velocidad: runSpeed si se pulsa Shift, si no walkSpeed.
            // 6. Acumular gravedad en verticalVelocity (no hay salto, solo pega al suelo).
            // 7. Un unico controller.Move() con el movimiento horizontal + el vertical.
        }

        // Convierte el input 2D a una direccion en el mundo usando los ejes planos de la camara:
        // W va hacia GetFlatForward() y D hacia GetFlatRight().
        // Asi el PJ se mueve siempre relativo a hacia donde estas mirando.
        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            return Vector3.zero;
        }

        // Rota suavemente al jugador para que mire hacia la direccion en la que se mueve.
        private void RotateTowards(Vector3 direction)
        {
        }

        // Bloquea o desbloquea el movimiento del jugador.
        // Lo llama el sistema de dialogo para que no te puedas mover mientras hablas con un NPC.
        public void SetMovementLocked(bool locked)
        {
        }
    }
}
