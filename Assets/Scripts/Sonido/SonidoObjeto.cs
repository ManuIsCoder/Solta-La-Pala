using UnityEngine;

namespace SoltaLaPala.Sonido
{
    public enum ModoSonidoObjeto
    {
        Constante,
        AlInteractuar
    }

    [DisallowMultipleComponent]
    public class SonidoObjeto : MonoBehaviour
    {
        [Tooltip("Constante = en bucle siempre. AlInteractuar = solo cuando se usa el objeto.")]
        public ModoSonidoObjeto modo = ModoSonidoObjeto.Constante;

        [Tooltip("Audios del objeto. En modo Constante suena el primero; en AlInteractuar " +
                 "se elige uno al azar cada vez.")]
        public AudioClip[] sonidos;

        [Range(0f, 1f)]
        public float volumen = 1f;

        [Tooltip("Hasta esta distancia (metros) suena al maximo volumen.")]
        public float distanciaMinima = 2f;
        [Tooltip("Pasada esta distancia (metros) ya no se oye.")]
        public float distanciaMaxima = 15f;

        private AudioSource fuente;

        public bool Reproduciendo => fuente != null && fuente.isPlaying;

        private void Awake()
        {
            fuente = gameObject.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 1f;
            fuente.rolloffMode = AudioRolloffMode.Linear;
            fuente.minDistance = distanciaMinima;
            fuente.maxDistance = distanciaMaxima;
            fuente.volume = volumen;
        }

        private void Start()
        {
            if (modo != ModoSonidoObjeto.Constante || sonidos == null || sonidos.Length == 0 || sonidos[0] == null)
            {
                return;
            }

            fuente.clip = sonidos[0];
            fuente.loop = true;
            fuente.Play();

            fuente.time = Random.value * fuente.clip.length;
        }

        public void Reproducir()
        {
            AudioClip clip = ElegirSonido();
            if (clip != null)
            {
                fuente.PlayOneShot(clip, volumen);
            }
        }

        private AudioClip ElegirSonido()
        {
            if (sonidos == null || sonidos.Length == 0)
            {
                return null;
            }

            return sonidos[Random.Range(0, sonidos.Length)];
        }
    }
}
