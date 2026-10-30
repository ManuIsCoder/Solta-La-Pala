using UnityEngine;

namespace SoltaLaPala.Sonido
{
    // Como suena un objeto del mundo.
    public enum ModoSonidoObjeto
    {
        // Suena todo el tiempo en bucle (heladera, luz fluorescente).
        Constante,
        // Solo suena cuando alguien lo manda con Reproducir() (microondas, impresora, telefono).
        AlInteractuar
    }

    // Sonido 3D de un objeto: se oye mas fuerte cuanto mas cerca esta el jugador.
    //
    // Los prefabs de Assets/Prefabs/con sonido lo llevan ya configurado. Para
    // cambiar el audio o el volumen de uno, se edita aca en el Inspector.
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

        // True mientras esta sonando algo.
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

            // Que varios objetos iguales no suenen sincronizados.
            fuente.time = Random.value * fuente.clip.length;
        }

        // Hace sonar uno de los audios, sin cortar el que ya este sonando.
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
