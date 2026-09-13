using UnityEngine;

namespace SoltaLaPala.Player
{
    // Camara orbital de tercera persona: gira alrededor del jugador con el raton.
    // La direccion en la que mira esta camara es la que PlayerMovement usa como "adelante" (W).
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Objetivo")]
        [Tooltip("El jugador al que sigue la camara.")]
        public Transform target;
        [Tooltip("Desplazamiento sobre el pivote, para que la camara mire a la cabeza y no a los pies.")]
        public Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Raton")]
        public float mouseSensitivityX = 200f;
        public float mouseSensitivityY = 150f;
        [Tooltip("Invierte el eje vertical del raton.")]
        public bool invertY = false;

        [Header("Limites verticales")]
        [Tooltip("Cuanto puede mirar hacia arriba (grados negativos) y hacia abajo (positivos).")]
        public float minPitch = -35f;
        public float maxPitch = 70f;

        [Header("Distancia")]
        public float distance = 4f;
        [Tooltip("Suavizado del seguimiento de la camara al moverse el jugador.")]
        public float followSmoothTime = 0.05f;

        [Header("Colision")]
        [Tooltip("Capas contra las que la camara se acerca para no atravesar paredes.")]
        public LayerMask collisionLayers;
        [Tooltip("Margen para que la camara no quede pegada a la pared.")]
        public float collisionPadding = 0.2f;

        // Angulo horizontal acumulado (izquierda/derecha).
        private float yaw;
        // Angulo vertical acumulado (arriba/abajo).
        private float pitch;
        private bool cameraLocked;

        // Inicializa yaw/pitch con la rotacion actual y esconde/bloquea el cursor.
        private void Start()
        {
        }

        // Lee el raton cada frame y acumula yaw/pitch.
        // No hace nada si la camara esta bloqueada (ej: durante un dialogo).
        private void Update()
        {
        }

        // Coloca la camara despues de que el jugador se haya movido.
        // Va en LateUpdate para que no tiemble la imagen.
        private void LateUpdate()
        {
        }

        // Devuelve el movimiento del raton de este frame, ya escalado por sensibilidad.
        private Vector2 ReadMouseDelta()
        {
            return Vector2.zero;
        }

        // Suma el delta del raton a yaw/pitch y recorta el pitch entre minPitch y maxPitch
        // para que no se pueda dar la vuelta completa en vertical.
        private void ApplyRotationInput(Vector2 mouseDelta)
        {
        }

        // Calcula donde deberia estar la camara: parte del jugador + offset,
        // y retrocede 'distance' en la direccion contraria a la que mira.
        private Vector3 CalculateDesiredPosition()
        {
            return Vector3.zero;
        }

        // Lanza un rayo del jugador a la posicion deseada y, si choca con algo,
        // acerca la camara para no meterse dentro de paredes.
        private Vector3 ResolveCollision(Vector3 desiredPosition)
        {
            return desiredPosition;
        }

        // Bloquea o desbloquea el giro de la camara y libera el cursor.
        // Lo llama el sistema de dialogo junto con SetMovementLocked.
        public void SetCameraLocked(bool locked)
        {
        }

        // Muestra u oculta el cursor del raton.
        private void SetCursorLocked(bool locked)
        {
        }

        // Direccion horizontal hacia donde mira la camara (sin componente vertical).
        // Es el "adelante" que usa PlayerMovement para la tecla W.
        public Vector3 GetFlatForward()
        {
            return Vector3.forward;
        }

        // Direccion horizontal a la derecha de la camara. El "lateral" para las teclas A/D.
        public Vector3 GetFlatRight()
        {
            return Vector3.right;
        }
    }
}
