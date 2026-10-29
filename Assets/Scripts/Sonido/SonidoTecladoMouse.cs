using UnityEngine;

namespace SoltaLaPala.Sonido
{
    [DisallowMultipleComponent]
    public class SonidoTecladoMouse : MonoBehaviour
    {
        [Header("Teclado")]
        public AudioClip[] sonidosTeclado;
        public float tecladoCadaMinimo = 10f;
        public float tecladoCadaMaximo = 15f;

        [Header("Mouse")]
        public AudioClip[] sonidosMouse;
        public float mouseCadaMinimo = 20f;
        public float mouseCadaMaximo = 30f;

        [Header("Volumen y distancia")]
        [Range(0f, 1f)]
        public float volumen = 1f;
        public float distanciaMinima = 1.5f;
        public float distanciaMaxima = 10f;

        private AudioSource fuente;
        private float proximoTeclado;
        private float proximoMouse;

        private void Awake()
        {
            fuente = gameObject.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 1f;
            fuente.rolloffMode = AudioRolloffMode.Linear;
            fuente.minDistance = distanciaMinima;
            fuente.maxDistance = distanciaMaxima;
            fuente.volume = volumen;

            ProgramarTeclado();
            ProgramarMouse();
        }

        private void Update()
        {
            if (fuente.isPlaying)
            {
                return;
            }

            if (Time.time >= proximoTeclado)
            {
                Sonar(sonidosTeclado);
                ProgramarTeclado();
                return;
            }

            if (Time.time >= proximoMouse)
            {
                Sonar(sonidosMouse);
                ProgramarMouse();
            }
        }

        private void Sonar(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null)
            {
                fuente.PlayOneShot(clip, volumen);
            }
        }

        private void ProgramarTeclado()
        {
            proximoTeclado = Time.time + Random.Range(tecladoCadaMinimo, Mathf.Max(tecladoCadaMinimo, tecladoCadaMaximo));
        }

        private void ProgramarMouse()
        {
            proximoMouse = Time.time + Random.Range(mouseCadaMinimo, Mathf.Max(mouseCadaMinimo, mouseCadaMaximo));
        }
    }
}
