using SoltaLaPala.Guardado;
using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.Player
{
    // Movimiento en tercera persona del jugador (sin salto).
    // El movimiento es relativo a la camara: W va siempre hacia donde mira la camara,
    // que a su vez gira con el raton (ver CamaraTerceraPersona).
    [RequireComponent(typeof(CharacterController))]
    public class MovimientoJugador : MonoBehaviour, IGuardable
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

        // Donde estaba el jugador al cargar la escena, para poder devolverlo ahi
        // al reiniciar el nivel.
        private Vector3 posicionInicial;
        private float rotacionInicialY;

        // Cachea el CharacterController y busca la camara si no fue asignada en el inspector.
        private void Awake()
        {
            controlador = GetComponent<CharacterController>();

            posicionInicial = transform.position;
            rotacionInicialY = transform.eulerAngles.y;

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
            // El input se lee desde ControlesJuego y no desde los ejes fijos del proyecto
            // para que las teclas se puedan reasignar desde el menu de configuracion.
            Vector2 input = movimientoBloqueado
                ? Vector2.zero
                : new Vector2(
                    ControlesJuego.Eje(AccionJuego.Izquierda, AccionJuego.Derecha),
                    ControlesJuego.Eje(AccionJuego.Atras, AccionJuego.Adelante));

            // Zona muerta para evitar que ruido o descalibracion hagan temblar / oscilar al jugador
            if (input.sqrMagnitude < 0.05f)
            {
                input = Vector2.zero;
            }

            Vector3 direccion = ObtenerDireccionRelativaCamara(input);

            // Solo girar si realmente hay intencion de moverse
            if (input != Vector2.zero && direccion.sqrMagnitude > 0.01f)
            {
                GirarHacia(direccion);
            }
            else
            {
                // Resetear la velocidad residual de giro para que no oscile en el lugar
                velocidadGiro = 0f;
            }

            float velocidad = ControlesJuego.Mantenida(AccionJuego.Correr)
                ? velocidadCorrer
                : velocidadCaminar;

            AplicarGravedad();

            // Si no hay input horizontal, el desplazamiento horizontal es exactamente 0
            Vector3 desplazamientoHorizontal = (input != Vector2.zero) ? direccion * velocidad : Vector3.zero;
            Vector3 desplazamiento = desplazamientoHorizontal + velocidadVertical;
            controlador.Move(desplazamiento * Time.deltaTime);
        }

        // Convierte el input 2D a una direccion en el mundo usando los ejes planos de la camara:
        // W va hacia ObtenerAdelante() y D hacia ObtenerDerecha().
        // Asi el PJ se mueve siempre relativo a hacia donde estas mirando.
        private Vector3 ObtenerDireccionRelativaCamara(Vector2 input)
        {
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            if (camaraJugador == null) return Vector3.zero;

            return camaraJugador.ObtenerAdelante() * input.y + camaraJugador.ObtenerDerecha() * input.x;
        }

        // Acumula gravedad en velocidadVertical para que el PJ se pegue al suelo.
        // No hay salto: en cuanto toca el suelo la velocidad se resetea.
        private void AplicarGravedad()
        {
            if (controlador.isGrounded && velocidadVertical.y < 0f)
            {
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
            float anguloObjetivo = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg;

            float anguloSuavizado = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, anguloObjetivo, ref velocidadGiro, suavizadoRotacion);

            transform.rotation = Quaternion.Euler(0f, anguloSuavizado, 0f);
        }

        // Bloquea o desbloquea el movimiento del jugador.
        // Lo llama el sistema de dialogo para que no te puedas mover mientras hablas con un NPC.
        public void BloquearMovimiento(bool bloqueado)
        {
            movimientoBloqueado = bloqueado;
            velocidadGiro = 0f;
        }

        public void Capturar(DatosPartidaGuardada datos)
        {
            datos.posicionJugador = transform.position;
            datos.rotacionJugadorY = transform.eulerAngles.y;
        }

        public void Restaurar(DatosPartidaGuardada datos)
        {
            TeletransportarA(datos.posicionJugador, datos.rotacionJugadorY);
        }

        // Devuelve al jugador a donde empezo la escena. Lo usa el reinicio de nivel.
        public void VolverAlInicio()
        {
            TeletransportarA(posicionInicial, rotacionInicialY);
        }

        // Mueve al jugador de golpe a una posicion.
        //
        // El CharacterController mantiene su propia idea de donde esta y pisa las
        // asignaciones directas a transform.position, asi que hay que apagarlo
        // mientras se hace el cambio para que lea la posicion nueva al encenderse.
        private void TeletransportarA(Vector3 posicion, float rotacionY)
        {
            bool estabaActivo = controlador.enabled;
            controlador.enabled = false;

            transform.position = posicion;
            transform.rotation = Quaternion.Euler(0f, rotacionY, 0f);

            controlador.enabled = estabaActivo;

            // La velocidad acumulada es de la posicion vieja: si el jugador venia
            // cayendo, arrastrarla aqui lo hunde en el suelo al aparecer.
            velocidadVertical = Vector3.zero;
            velocidadGiro = 0f;
        }
    }
}
