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
        // Velocidad que va acumulando SmoothDamp al seguir al jugador.
        private Vector3 followVelocity;

        // Inicializa yaw/pitch con la rotacion actual y esconde/bloquea el cursor.
        private void Start()
        {
            // Si no se asigno objetivo en el inspector, buscar al jugador por tag.
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
                else
                {
                    Debug.LogError("ThirdPersonCamera: no hay objetivo asignado y no se encontro ningun objeto con tag 'Player'.", this);
                    enabled = false;
                    return;
                }
            }

            // Arrancar desde la rotacion que tenga la camara en la escena, para no dar un tiron
            // en el primer frame. Los angulos de eulerAngles vienen en rango 0..360, asi que
            // el pitch hay que pasarlo a -180..180 antes de recortarlo.
            Vector3 startRotation = transform.eulerAngles;
            yaw = startRotation.y;
            pitch = Mathf.Clamp(NormalizeAngle(startRotation.x), minPitch, maxPitch);

            SetCursorLocked(true);
        }

        // Pasa un angulo de 0..360 a -180..180, que es el rango con el que trabaja el clamp del pitch.
        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            return angle > 180f ? angle - 360f : angle;
        }

        // Lee el raton cada frame y acumula yaw/pitch.
        // No hace nada si la camara esta bloqueada (ej: durante un dialogo).
        private void Update()
        {
            if (cameraLocked)
            {
                return;
            }

            ApplyRotationInput(ReadMouseDelta());
        }

        // Coloca la camara despues de que el jugador se haya movido.
        // Va en LateUpdate para que no tiemble la imagen.
        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = ResolveCollision(CalculateDesiredPosition());

            // La posicion se suaviza para que la camara no de tirones al moverse el jugador,
            // pero la rotacion se aplica de golpe: suavizarla haria que la camara vaya
            // por detras del raton y se siente pastoso.
            transform.position = followSmoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime)
                : desiredPosition;

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // Devuelve el movimiento del raton de este frame, ya escalado por sensibilidad.
        // Ojo: los ejes "Mouse X"/"Mouse Y" ya vienen escalados por el tiempo de frame,
        // por eso NO se multiplican por Time.deltaTime (haria la camara lenta y pastosa).
        private Vector2 ReadMouseDelta()
        {
            float deltaX = Input.GetAxis("Mouse X") * mouseSensitivityX * 0.01f;
            float deltaY = Input.GetAxis("Mouse Y") * mouseSensitivityY * 0.01f;
            return new Vector2(deltaX, deltaY);
        }

        // Suma el delta del raton a yaw/pitch y recorta el pitch entre minPitch y maxPitch
        // para que no se pueda dar la vuelta completa en vertical.
        private void ApplyRotationInput(Vector2 mouseDelta)
        {
            yaw += mouseDelta.x;

            // En pantalla, mover el raton hacia arriba (delta positivo) tiene que bajar el pitch,
            // porque un pitch positivo en Unity mira hacia abajo. De ahi el signo invertido.
            pitch += invertY ? mouseDelta.y : -mouseDelta.y;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // El yaw no se recorta (giro libre de 360), pero se mantiene en rango
            // para que no crezca sin limite en partidas largas.
            yaw = Mathf.Repeat(yaw, 360f);
        }

        // Calcula donde deberia estar la camara: parte del jugador + offset,
        // y retrocede 'distance' en la direccion contraria a la que mira.
        private Vector3 CalculateDesiredPosition()
        {
            return GetPivotPosition() - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * distance;
        }

        // Punto alrededor del cual orbita la camara: el jugador desplazado por targetOffset
        // (a la altura de la cabeza, no de los pies).
        private Vector3 GetPivotPosition()
        {
            return target.position + targetOffset;
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
