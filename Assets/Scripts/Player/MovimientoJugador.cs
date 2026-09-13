using UnityEngine;

namespace SoltaLaPala.Player
{
    // Movimiento en tercera persona del jugador (sin salto).
    // El movimiento es relativo a la camara: W va siempre hacia donde mira la camara,
    // que a su vez gira con el raton (ver CamaraTerceraPersona).
    [RequireComponent(typeof(CharacterController))]
    public class MovimientoJugador : MonoBehaviour
    {
        [Header("Movimiento")]
        public float velocidadCaminar = 4f;
        public float velocidadCorrer = 7f;
        [Tooltip("Cuanto tarda el PJ en girar hacia la direccion en la que se mueve.")]
        public float suavizadoRotacion = 0.1f;
        public float gravedad = -20f;

        [Header("Referencias")]
        [Tooltip("Camara que define el 'adelante'. Normalmente la que lleva el CamaraTerceraPersona.")]
        public CamaraTerceraPersona camaraJugador;

        private CharacterController controlador;
        private Vector3 velocidadVertical;
        private bool movimientoBloqueado;
        // Velocidad angular que va acumulando SmoothDampAngle al girar el PJ.
        private float velocidadGiro;

        // Cachea el CharacterController y busca la camara si no fue asignada en el inspector.
        private void Awake()
        {
            // GetComponent es caro para llamarlo cada frame, por eso se guarda una sola vez.
            // El [RequireComponent] de arriba garantiza que existe.
            controlador = GetComponent<CharacterController>();

            if (camaraJugador == null)
            {
                camaraJugador = FindFirstObjectByType<CamaraTerceraPersona>();

                if (camaraJugador == null)
                {
                    Debug.LogError("MovimientoJugador: no hay camara asignada y no se encontro ninguna CamaraTerceraPersona en la escena.", this);
                    enabled = false;
                }
            }
        }

        private void Update()
        {
            // Mientras hablas con un NPC no se lee input, pero la gravedad se sigue
            // aplicando para que el PJ no quede flotando si estaba en el aire.
            Vector2 input = movimientoBloqueado
                ? Vector2.zero
                : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            Vector3 direccion = ObtenerDireccionRelativaCamara(input);

            if (direccion != Vector3.zero)
            {
                GirarHacia(direccion);
            }

            float velocidad = Input.GetKey(KeyCode.LeftShift) ? velocidadCorrer : velocidadCaminar;

            AplicarGravedad();

            // Un unico Move() con horizontal + vertical: llamarlo dos veces por frame
            // da colisiones raras en rampas y bordes.
            Vector3 desplazamiento = direccion * velocidad + velocidadVertical;
            controlador.Move(desplazamiento * Time.deltaTime);
        }

        // Convierte el input 2D a una direccion en el mundo usando los ejes planos de la camara:
        // W va hacia ObtenerAdelante() y D hacia ObtenerDerecha().
        // Asi el PJ se mueve siempre relativo a hacia donde estas mirando.
        private Vector3 ObtenerDireccionRelativaCamara(Vector2 input)
        {
            // Normalizar el input evita que moverse en diagonal (W+D) sea mas rapido
            // que moverse recto, porque el vector (1,1) mide 1.41 y no 1.
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            return camaraJugador.ObtenerAdelante() * input.y + camaraJugador.ObtenerDerecha() * input.x;
        }

        // Acumula gravedad en velocidadVertical para que el PJ se pegue al suelo.
        // No hay salto: en cuanto toca el suelo la velocidad se resetea.
        private void AplicarGravedad()
        {
            if (controlador.isGrounded && velocidadVertical.y < 0f)
            {
                // Un valor pequeño en vez de 0 para que el CharacterController siga
                // detectando el suelo; con 0 exacto isGrounded parpadea.
                velocidadVertical.y = -2f;
            }
            else
            {
                velocidadVertical.y += gravedad * Time.deltaTime;
            }
        }

        // Rota suavemente al jugador para que mire hacia la direccion en la que se mueve.
        private void GirarHacia(Vector3 direccion)
        {
            // Atan2 da el angulo en el plano horizontal. Se usa (x, z) y no (z, x) porque
            // en Unity el eje Z es el "adelante" y los grados se miden desde ahi.
            float anguloObjetivo = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg;

            // SmoothDampAngle en vez de SmoothDamp: sabe que 350 y 10 grados estan a 20 de
            // distancia y no a 340, asi el PJ no da la vuelta larga al cruzar el cero.
            float anguloSuavizado = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, anguloObjetivo, ref velocidadGiro, suavizadoRotacion);

            transform.rotation = Quaternion.Euler(0f, anguloSuavizado, 0f);
        }

        // Bloquea o desbloquea el movimiento del jugador.
        // Lo llama el sistema de dialogo para que no te puedas mover mientras hablas con un NPC.
        public void BloquearMovimiento(bool bloqueado)
        {
            movimientoBloqueado = bloqueado;
        }
    }
}
