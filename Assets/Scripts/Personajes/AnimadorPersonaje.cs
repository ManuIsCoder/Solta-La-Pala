using UnityEngine;

namespace SoltaLaPala.Personajes
{
    [RequireComponent(typeof(Animator))]
    public class AnimadorPersonaje : MonoBehaviour
    {
        public const string ClipReposo = "Reposo";
        public const string ClipCaminata = "Caminata";
        public const string ClipSentado = "Sentado";
        public const string ClipAgarrar = "Agarrar";

        private static readonly int ParamVelocidad = Animator.StringToHash("Velocidad");
        private static readonly int ParamRitmo = Animator.StringToHash("RitmoCaminata");
        private static readonly int ParamSentado = Animator.StringToHash("Sentado");
        private static readonly int ParamAgarrar = Animator.StringToHash("Agarrar");

        [Tooltip("Usa la animacion de sentado en vez del reposo.")]
        public bool estaSentado;
        [Tooltip("Velocidad (m/s) a la que la caminata se reproduce a ritmo normal.")]
        public float velocidadCaminata = 1.8f;
        [Tooltip("Segundos que tarda el parametro Velocidad en alcanzar el valor real.")]
        public float suavizado = 0.1f;

        private Animator animator;
        private Transform referencia;
        private Vector3 posicionAnterior;

        public AnimationClip Clip(string nombre)
        {
            Animator a = animator != null ? animator : GetComponent<Animator>();
            AnimatorOverrideController controlador = a.runtimeAnimatorController as AnimatorOverrideController;
            return controlador != null ? controlador[nombre] : null;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            ActualizarReferencia();
        }

        public void Agarrar()
        {
            animator.SetTrigger(ParamAgarrar);
        }

        private void ActualizarReferencia()
        {
            referencia = transform.parent != null ? transform.parent : transform;
            posicionAnterior = referencia.position;
        }

        private void Update()
        {
            if (referencia == transform && transform.parent != null)
            {
                ActualizarReferencia();
            }

            float delta = Time.deltaTime;
            if (delta <= 0f)
            {
                return;
            }

            Vector3 desplazamiento = referencia.position - posicionAnterior;
            desplazamiento.y = 0f;
            posicionAnterior = referencia.position;
            float velocidad = desplazamiento.magnitude / delta;

            animator.SetFloat(ParamVelocidad, velocidad, suavizado, delta);
            bool caminando = velocidad > 0.1f && velocidadCaminata > 0f;
            animator.SetFloat(ParamRitmo, caminando ? Mathf.Clamp(velocidad / velocidadCaminata, 0.5f, 1.5f) : 1f);
            animator.SetBool(ParamSentado, estaSentado);
        }
    }
}
