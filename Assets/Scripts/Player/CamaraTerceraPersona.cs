using SoltaLaPala.Guardado;
using SoltaLaPala.Menus;
using SoltaLaPala.Personajes;
using UnityEngine;

namespace SoltaLaPala.Player
{
    public class CamaraTerceraPersona : MonoBehaviour, IGuardable
    {
        [Header("Objetivo")]
        [Tooltip("El jugador al que sigue la camara.")]
        public Transform objetivo;
        [Tooltip("Desplazamiento sobre el pivote, para que la camara mire a la cabeza y no a los pies.")]
        public Vector3 offsetObjetivo = new Vector3(0f, 1.5f, 0f);

        [Header("Mouse")]
        public float sensibilidadX = 200f;
        public float sensibilidadY = 150f;
        [Tooltip("Invierte el eje vertical del mouse.")]
        public bool invertirY = false;

        [Header("Limites verticales")]
        [Tooltip("Cuanto puede mirar hacia arriba (grados negativos) y hacia abajo (positivos).")]
        public float pitchMinimo = -35f;
        public float pitchMaximo = 70f;

        [Header("Distancia")]
        public float distancia = 4f;
        [Tooltip("Suavizado del seguimiento de la camara al moverse el jugador.")]
        public float suavizadoSeguimiento = 0.05f;

        [Header("Primera persona")]
        [Tooltip("Altura de los ojos del PJ si el modelo no tiene hueso de cabeza.")]
        public Vector3 offsetPrimeraPersona = new Vector3(0f, 0.55f, 0.15f);
        [Tooltip("Posicion de los ojos respecto de la altura de la cabeza, en los ejes del jugador.")]
        public Vector3 offsetOjos = new Vector3(0f, 0.1f, 0.12f);
        [Tooltip("Limites de la mirada vertical en primera persona.")]
        public float pitchMinimoPrimeraPersona = -75f;
        public float pitchMaximoPrimeraPersona = 75f;

        [Header("Colision")]
        [Tooltip("Capas contra las que la camara se acerca para no atravesar paredes.")]
        public LayerMask capasColision;
        [Tooltip("Margen para que la camara no quede pegada a la pared.")]
        public float margenColision = 0.2f;

        private float yaw;
        private float pitch;
        private float yawInicial;
        private float pitchInicial;
        private bool camaraBloqueada;
        private Vector3 velocidadSeguimiento;
        private Vector3 offsetCabeza;
        private bool cabezaMedida;
        private readonly RaycastHit[] impactos = new RaycastHit[16];

        public bool EnPrimeraPersona { get; private set; }

        private void Start()
        {
            if (objetivo == null)
            {
                GameObject jugador = GameObject.FindGameObjectWithTag("Player");
                if (jugador != null)
                {
                    objetivo = jugador.transform;
                }
                else
                {
                    Debug.LogError("CamaraTerceraPersona: no hay objetivo asignado y no se encontro ningun objeto con tag 'Player'.", this);
                    enabled = false;
                    return;
                }
            }

            Vector3 rotacionInicial = transform.eulerAngles;
            yaw = rotacionInicial.y;
            pitch = Mathf.Clamp(NormalizarAngulo(rotacionInicial.x), pitchMinimo, pitchMaximo);
            yawInicial = yaw;
            pitchInicial = pitch;

            AplicarAjustes();

            BloquearCursor(true);
        }

        private void OnEnable()
        {
            AjustesJuego.AlCambiarAjustes += AplicarAjustes;
        }

        private void OnDisable()
        {
            AjustesJuego.AlCambiarAjustes -= AplicarAjustes;
        }

        private void AplicarAjustes()
        {
            sensibilidadX = AjustesJuego.SensibilidadX;
            sensibilidadY = AjustesJuego.SensibilidadY;
            invertirY = AjustesJuego.InvertirY;
        }

        private static float NormalizarAngulo(float angulo)
        {
            angulo %= 360f;
            return angulo > 180f ? angulo - 360f : angulo;
        }

        private void Update()
        {
            if (camaraBloqueada || HayUiQueCapturaElRaton())
            {
                return;
            }

            if (ControlesJuego.Pulsada(AccionJuego.CambiarVista))
            {
                CambiarVista();
            }

            AplicarRotacion(LeerRaton());
        }

        private static bool HayUiQueCapturaElRaton()
        {
            if (uiExternasAbiertas > 0)
            {
                return true;
            }

            return GestorMenus.Instancia != null && GestorMenus.Instancia.HayMenuAbierto;
        }

        private static int uiExternasAbiertas;

        public static void RegistrarUiAbierta(bool abierta)
        {
            uiExternasAbiertas = Mathf.Max(0, uiExternasAbiertas + (abierta ? 1 : -1));
        }

        public static void RestablecerUisAbiertas()
        {
            uiExternasAbiertas = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstadoEstatico()
        {
            uiExternasAbiertas = 0;
        }

        public void CambiarVista()
        {
            EnPrimeraPersona = !EnPrimeraPersona;

            pitch = Mathf.Clamp(pitch, PitchMinimoActual, PitchMaximoActual);

            velocidadSeguimiento = Vector3.zero;
        }

        private float PitchMinimoActual => EnPrimeraPersona ? pitchMinimoPrimeraPersona : pitchMinimo;
        private float PitchMaximoActual => EnPrimeraPersona ? pitchMaximoPrimeraPersona : pitchMaximo;

        private void LateUpdate()
        {
            if (objetivo == null)
            {
                return;
            }

            Vector3 posicionDeseada = CalcularPosicionDeseada();

            if (EnPrimeraPersona)
            {
                transform.position = posicionDeseada;
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                return;
            }

            if (ResolverColision(ref posicionDeseada) || suavizadoSeguimiento <= 0f)
            {
                transform.position = posicionDeseada;
                velocidadSeguimiento = Vector3.zero;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, posicionDeseada,
                    ref velocidadSeguimiento, suavizadoSeguimiento);
            }

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private Vector2 LeerRaton()
        {
            float deltaX = Input.GetAxis("Mouse X") * sensibilidadX * 0.01f;
            float deltaY = Input.GetAxis("Mouse Y") * sensibilidadY * 0.01f;
            return new Vector2(deltaX, deltaY);
        }

        private void AplicarRotacion(Vector2 deltaRaton)
        {
            yaw += deltaRaton.x;

            pitch += invertirY ? deltaRaton.y : -deltaRaton.y;
            pitch = Mathf.Clamp(pitch, PitchMinimoActual, PitchMaximoActual);

            yaw = Mathf.Repeat(yaw, 360f);
        }

        private Vector3 CalcularPosicionDeseada()
        {
            if (EnPrimeraPersona)
            {
                return ObtenerPivote();
            }

            return ObtenerPivote() - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * distancia;
        }

        private Vector3 ObtenerPivote()
        {
            if (EnPrimeraPersona)
            {
                Vector3 offset = MedirAlturaCabeza() ? offsetCabeza : offsetPrimeraPersona;
                return objetivo.position + objetivo.TransformDirection(offset);
            }

            return objetivo.position + offsetObjetivo;
        }

        private bool MedirAlturaCabeza()
        {
            if (cabezaMedida)
            {
                return true;
            }

            ModeloPersonaje modelo = objetivo.GetComponent<ModeloPersonaje>();
            Transform cabeza = modelo != null ? modelo.BuscarHueso(ModeloPersonaje.HuesoCabeza) : null;
            if (cabeza == null)
            {
                return false;
            }

            float altura = objetivo.InverseTransformPoint(cabeza.position).y;
            offsetCabeza = new Vector3(offsetOjos.x, altura + offsetOjos.y, offsetOjos.z);
            cabezaMedida = true;
            return true;
        }

        private bool ResolverColision(ref Vector3 posicionDeseada)
        {
            if (capasColision == 0)
            {
                return false;
            }

            Vector3 pivote = ObtenerPivote();
            Vector3 haciaCamara = posicionDeseada - pivote;
            float largo = haciaCamara.magnitude;
            if (largo < 0.001f)
            {
                return false;
            }

            Vector3 direccion = haciaCamara / largo;
            int cantidad = Physics.SphereCastNonAlloc(pivote, margenColision, direccion, impactos,
                largo, capasColision, QueryTriggerInteraction.Ignore);

            float masCercano = largo;
            for (int i = 0; i < cantidad; i++)
            {
                if (impactos[i].transform.IsChildOf(objetivo))
                {
                    continue;
                }

                masCercano = Mathf.Min(masCercano, impactos[i].distance);
            }

            if (masCercano >= largo)
            {
                return false;
            }

            posicionDeseada = pivote + direccion * masCercano;
            return true;
        }

        public void BloquearCamara(bool bloqueado)
        {
            camaraBloqueada = bloqueado;

            BloquearCursor(!bloqueado);
        }

        private void BloquearCursor(bool bloqueado)
        {
            Cursor.lockState = bloqueado ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !bloqueado;
        }

        public Vector3 ObtenerAdelante()
        {
            return Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        }

        public Vector3 ObtenerDerecha()
        {
            return Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        }

        public void Capturar(DatosPartidaGuardada datos)
        {
            datos.rotacionCamaraX = pitch;
            datos.rotacionCamaraY = yaw;
        }

        public void Restaurar(DatosPartidaGuardada datos)
        {
            yaw = Mathf.Repeat(datos.rotacionCamaraY, 360f);
            pitch = Mathf.Clamp(NormalizarAngulo(datos.rotacionCamaraX), PitchMinimoActual, PitchMaximoActual);

            ColocarSinSuavizado();
        }

        public void VolverAlInicio()
        {
            yaw = yawInicial;
            pitch = Mathf.Clamp(pitchInicial, PitchMinimoActual, PitchMaximoActual);

            ColocarSinSuavizado();
        }

        private void ColocarSinSuavizado()
        {
            if (objetivo == null)
            {
                return;
            }

            Vector3 destino = CalcularPosicionDeseada();

            if (!EnPrimeraPersona)
            {
                ResolverColision(ref destino);
            }

            transform.position = destino;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            velocidadSeguimiento = Vector3.zero;
        }
    }
}
