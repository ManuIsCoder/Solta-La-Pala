using SoltaLaPala.Guardado;
using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.Player
{
    // Camara orbital de tercera persona: gira alrededor del jugador con el raton.
    // La direccion en la que mira esta camara es la que MovimientoJugador usa como "adelante" (W).
    public class CamaraTerceraPersona : MonoBehaviour, IGuardable
    {
        [Header("Objetivo")]
        [Tooltip("El jugador al que sigue la camara.")]
        public Transform objetivo;
        [Tooltip("Desplazamiento sobre el pivote, para que la camara mire a la cabeza y no a los pies.")]
        public Vector3 offsetObjetivo = new Vector3(0f, 1.5f, 0f);

        [Header("Raton")]
        public float sensibilidadX = 200f;
        public float sensibilidadY = 150f;
        [Tooltip("Invierte el eje vertical del raton.")]
        public bool invertirY = false;

        [Header("Limites verticales")]
        [Tooltip("Cuanto puede mirar hacia arriba (grados negativos) y hacia abajo (positivos).")]
        public float pitchMinimo = -35f;
        public float pitchMaximo = 70f;

        [Header("Distancia")]
        public float distancia = 4f;
        [Tooltip("Suavizado del seguimiento de la camara al moverse el jugador.")]
        public float suavizadoSeguimiento = 0.05f;

        [Header("Colision")]
        [Tooltip("Capas contra las que la camara se acerca para no atravesar paredes.")]
        public LayerMask capasColision;
        [Tooltip("Margen para que la camara no quede pegada a la pared.")]
        public float margenColision = 0.2f;

        // Angulo horizontal acumulado (izquierda/derecha).
        private float yaw;
        // Angulo vertical acumulado (arriba/abajo).
        private float pitch;
        private bool camaraBloqueada;
        // Velocidad que va acumulando SmoothDamp al seguir al jugador.
        private Vector3 velocidadSeguimiento;

        // Inicializa yaw/pitch con la rotacion actual y esconde/bloquea el cursor.
        private void Start()
        {
            // Si no se asigno objetivo en el inspector, buscar al jugador por tag.
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

            // Arrancar desde la rotacion que tenga la camara en la escena, para no dar un tiron
            // en el primer frame. Los angulos de eulerAngles vienen en rango 0..360, asi que
            // el pitch hay que pasarlo a -180..180 antes de recortarlo.
            Vector3 rotacionInicial = transform.eulerAngles;
            yaw = rotacionInicial.y;
            pitch = Mathf.Clamp(NormalizarAngulo(rotacionInicial.x), pitchMinimo, pitchMaximo);

            AplicarAjustes();

            BloquearCursor(true);
        }

        // La sensibilidad y el invertir-Y los manda el menu de configuracion, asi que
        // se copian al arrancar y cada vez que el jugador los cambia.
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

        // Pasa un angulo de 0..360 a -180..180, que es el rango con el que trabaja el clamp del pitch.
        private static float NormalizarAngulo(float angulo)
        {
            angulo %= 360f;
            return angulo > 180f ? angulo - 360f : angulo;
        }

        // Lee el raton cada frame y acumula yaw/pitch.
        // No hace nada si la camara esta bloqueada (ej: durante un dialogo).
        private void Update()
        {
            if (camaraBloqueada)
            {
                return;
            }

            AplicarRotacion(LeerRaton());
        }

        // Coloca la camara despues de que el jugador se haya movido.
        // Va en LateUpdate para que no tiemble la imagen.
        private void LateUpdate()
        {
            if (objetivo == null)
            {
                return;
            }

            Vector3 posicionDeseada = ResolverColision(CalcularPosicionDeseada());

            // La posicion se suaviza para que la camara no de tirones al moverse el jugador,
            // pero la rotacion se aplica de golpe: suavizarla haria que la camara vaya
            // por detras del raton y se siente pastoso.
            transform.position = suavizadoSeguimiento > 0f
                ? Vector3.SmoothDamp(transform.position, posicionDeseada, ref velocidadSeguimiento, suavizadoSeguimiento)
                : posicionDeseada;

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // Devuelve el movimiento del raton de este frame, ya escalado por sensibilidad.
        // Ojo: los ejes "Mouse X"/"Mouse Y" ya vienen escalados por el tiempo de frame,
        // por eso NO se multiplican por Time.deltaTime (haria la camara lenta y pastosa).
        private Vector2 LeerRaton()
        {
            float deltaX = Input.GetAxis("Mouse X") * sensibilidadX * 0.01f;
            float deltaY = Input.GetAxis("Mouse Y") * sensibilidadY * 0.01f;
            return new Vector2(deltaX, deltaY);
        }

        // Suma el delta del raton a yaw/pitch y recorta el pitch entre pitchMinimo y pitchMaximo
        // para que no se pueda dar la vuelta completa en vertical.
        private void AplicarRotacion(Vector2 deltaRaton)
        {
            yaw += deltaRaton.x;

            // En pantalla, mover el raton hacia arriba (delta positivo) tiene que bajar el pitch,
            // porque un pitch positivo en Unity mira hacia abajo. De ahi el signo invertido.
            pitch += invertirY ? deltaRaton.y : -deltaRaton.y;
            pitch = Mathf.Clamp(pitch, pitchMinimo, pitchMaximo);

            // El yaw no se recorta (giro libre de 360), pero se mantiene en rango
            // para que no crezca sin limite en partidas largas.
            yaw = Mathf.Repeat(yaw, 360f);
        }

        // Calcula donde deberia estar la camara: parte del jugador + offset,
        // y retrocede 'distancia' en la direccion contraria a la que mira.
        private Vector3 CalcularPosicionDeseada()
        {
            return ObtenerPivote() - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * distancia;
        }

        // Punto alrededor del cual orbita la camara: el jugador desplazado por offsetObjetivo
        // (a la altura de la cabeza, no de los pies).
        private Vector3 ObtenerPivote()
        {
            return objetivo.position + offsetObjetivo;
        }

        // Lanza un rayo del jugador a la posicion deseada y, si choca con algo,
        // acerca la camara para no meterse dentro de paredes.
        private Vector3 ResolverColision(Vector3 posicionDeseada)
        {
            if (capasColision == 0)
            {
                return posicionDeseada;
            }

            Vector3 pivote = ObtenerPivote();
            Vector3 haciaCamara = posicionDeseada - pivote;

            // SphereCast en vez de Raycast: un rayo fino se cuela por esquinas y bordes,
            // la esfera respeta el volumen que ocupa la camara.
            if (Physics.SphereCast(pivote, margenColision, haciaCamara.normalized, out RaycastHit impacto,
                    haciaCamara.magnitude, capasColision, QueryTriggerInteraction.Ignore))
            {
                return pivote + haciaCamara.normalized * impacto.distance;
            }

            return posicionDeseada;
        }

        // Bloquea o desbloquea el giro de la camara y libera el cursor.
        // Lo llama el sistema de dialogo junto con BloquearMovimiento.
        public void BloquearCamara(bool bloqueado)
        {
            camaraBloqueada = bloqueado;

            // Al bloquear se libera el cursor para poder clicar en la UI del dialogo,
            // y al desbloquear se vuelve a capturar.
            BloquearCursor(!bloqueado);
        }

        // Captura el cursor en el centro de la pantalla y lo esconde, o lo libera.
        private void BloquearCursor(bool bloqueado)
        {
            Cursor.lockState = bloqueado ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !bloqueado;
        }

        // Direccion horizontal hacia donde mira la camara (sin componente vertical).
        // Es el "adelante" que usa MovimientoJugador para la tecla W.
        // Se calcula desde el yaw y no desde transform.forward para que mirar al suelo
        // no empuje al PJ hacia abajo: solo cuenta el giro horizontal.
        public Vector3 ObtenerAdelante()
        {
            return Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        }

        // Direccion horizontal a la derecha de la camara. El "lateral" para las teclas A/D.
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
            pitch = Mathf.Clamp(NormalizarAngulo(datos.rotacionCamaraX), pitchMinimo, pitchMaximo);

            ColocarSinSuavizado();
        }

        // Pone la camara en su sitio de un golpe, saltando el SmoothDamp.
        //
        // Hace falta al cargar partida: el jugador acaba de teletransportarse y el
        // suavizado haria que la camara viaje volando desde donde estaba, atravesando
        // el escenario durante medio segundo.
        private void ColocarSinSuavizado()
        {
            if (objetivo == null)
            {
                return;
            }

            transform.position = ResolverColision(CalcularPosicionDeseada());
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            // La velocidad acumulada del suavizado es de la posicion vieja y arrastraria
            // la camara mas alla del objetivo en los frames siguientes.
            velocidadSeguimiento = Vector3.zero;
        }
    }
}
