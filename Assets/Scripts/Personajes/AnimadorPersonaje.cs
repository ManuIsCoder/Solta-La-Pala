using SoltaLaPala.Menus;
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
        private static readonly int EstadoAgarrar = Animator.StringToHash("Agarrar");
        private static readonly int EstadoNada = Animator.StringToHash("Nada");
        private const string CapaAgarrar = "Agarrar";

        [Tooltip("Usa la animacion de sentado en vez del reposo.")]
        public bool estaSentado;
        [Tooltip("Velocidad (m/s) a la que la caminata se reproduce a ritmo normal.")]
        public float velocidadCaminata = 1.8f;
        [Tooltip("Cuanto cambia por segundo el parametro Velocidad. Mas alto = pasa antes de caminar a reposo.")]
        public float aceleracion = 8f;

        private Animator animator;
        private Transform referencia;
        private Vector3 posicionAnterior;
        private float velocidadAnimada;
        private int indiceCapaAgarrar = -1;
        private bool esperandoFinDeAgarrar;
        private bool animacionAgarrarIniciada;

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
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            indiceCapaAgarrar = animator.GetLayerIndex(CapaAgarrar);
            if (indiceCapaAgarrar >= 0)
            {
                animator.SetLayerWeight(indiceCapaAgarrar, 0f);
            }
            ActualizarReferencia();
        }

        public void Agarrar()
        {
            if (indiceCapaAgarrar >= 0)
            {
                animator.SetLayerWeight(indiceCapaAgarrar, 1f);
                esperandoFinDeAgarrar = true;
                animacionAgarrarIniciada = false;
            }

            animator.SetTrigger(ParamAgarrar);
        }

        private void ActualizarReferencia()
        {
            referencia = transform.parent != null ? transform.parent : transform;
            posicionAnterior = referencia.position;
        }

        private void Update()
        {
            ActualizarCapaAgarrar();

            if (referencia == transform && transform.parent != null)
            {
                ActualizarReferencia();
            }

            bool enPausa = GestorMenus.Instancia != null && GestorMenus.Instancia.HayMenuAbierto;
            animator.speed = enPausa ? 0f : 1f;

            float delta = Time.unscaledDeltaTime;
            if (enPausa || delta <= 0f)
            {
                posicionAnterior = referencia.position;
                return;
            }

            Vector3 desplazamiento = referencia.position - posicionAnterior;
            desplazamiento.y = 0f;
            posicionAnterior = referencia.position;
            float velocidad = desplazamiento.magnitude / delta;

            velocidadAnimada = Mathf.MoveTowards(velocidadAnimada, velocidad, aceleracion * delta);
            animator.SetFloat(ParamVelocidad, velocidadAnimada);

            bool caminando = velocidadAnimada > 0.1f && velocidadCaminata > 0f;
            animator.SetFloat(ParamRitmo, caminando ? Mathf.Clamp(velocidadAnimada / velocidadCaminata, 0.5f, 1.5f) : 1f);
            animator.SetBool(ParamSentado, estaSentado);
        }

        private void ActualizarCapaAgarrar()
        {
            if (!esperandoFinDeAgarrar || indiceCapaAgarrar < 0)
            {
                return;
            }

            AnimatorStateInfo estado = animator.GetCurrentAnimatorStateInfo(indiceCapaAgarrar);
            bool enTransicion = animator.IsInTransition(indiceCapaAgarrar);
            if (estado.shortNameHash == EstadoAgarrar
                || (enTransicion
                    && animator.GetNextAnimatorStateInfo(indiceCapaAgarrar).shortNameHash == EstadoAgarrar))
            {
                animacionAgarrarIniciada = true;
                return;
            }

            if (animacionAgarrarIniciada && !enTransicion && estado.shortNameHash == EstadoNada)
            {
                animator.SetLayerWeight(indiceCapaAgarrar, 0f);
                esperandoFinDeAgarrar = false;
                animacionAgarrarIniciada = false;
            }
        }
    }
}
