using SoltaLaPala.Guardado;
using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.Player
{
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
        private float velocidadGiro;

        private Vector3 posicionInicial;
        private float rotacionInicialY;

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
            Vector2 input = movimientoBloqueado
                ? Vector2.zero
                : new Vector2(
                    ControlesJuego.Eje(AccionJuego.Izquierda, AccionJuego.Derecha),
                    ControlesJuego.Eje(AccionJuego.Atras, AccionJuego.Adelante));

            if (input.sqrMagnitude < 0.05f)
            {
                input = Vector2.zero;
            }

            Vector3 direccion = ObtenerDireccionRelativaCamara(input);

            if (input != Vector2.zero && direccion.sqrMagnitude > 0.01f)
            {
                GirarHacia(direccion);
            }
            else
            {
                velocidadGiro = 0f;
            }

            float velocidad = ControlesJuego.Mantenida(AccionJuego.Correr)
                ? velocidadCorrer
                : velocidadCaminar;

            AplicarGravedad();

            Vector3 desplazamientoHorizontal = (input != Vector2.zero) ? direccion * velocidad : Vector3.zero;
            Vector3 desplazamiento = desplazamientoHorizontal + velocidadVertical;
            controlador.Move(desplazamiento * Time.deltaTime);
        }

        private Vector3 ObtenerDireccionRelativaCamara(Vector2 input)
        {
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            if (camaraJugador == null) return Vector3.zero;

            return camaraJugador.ObtenerAdelante() * input.y + camaraJugador.ObtenerDerecha() * input.x;
        }

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

        private void GirarHacia(Vector3 direccion)
        {
            float anguloObjetivo = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg;

            float anguloSuavizado = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, anguloObjetivo, ref velocidadGiro, suavizadoRotacion);

            transform.rotation = Quaternion.Euler(0f, anguloSuavizado, 0f);
        }

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

        public void VolverAlInicio()
        {
            TeletransportarA(posicionInicial, rotacionInicialY);
        }

        private void TeletransportarA(Vector3 posicion, float rotacionY)
        {
            bool estabaActivo = controlador.enabled;
            controlador.enabled = false;

            transform.position = posicion;
            transform.rotation = Quaternion.Euler(0f, rotacionY, 0f);

            controlador.enabled = estabaActivo;

            velocidadVertical = Vector3.zero;
            velocidadGiro = 0f;
        }
    }
}
