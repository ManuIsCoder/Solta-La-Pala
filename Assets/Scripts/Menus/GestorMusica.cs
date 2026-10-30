using System.Collections;
using UnityEngine;

namespace SoltaLaPala.Menus
{
    public class GestorMusica : MonoBehaviour
    {
        public static GestorMusica Instancia { get; private set; }

        private const string Carpeta = "Audio/Musica/";

        [Tooltip("Segundos que tarda en apagarse una pista y prenderse la siguiente.")]
        public float duracionFundido = 0.6f;

        private AudioSource fuente;
        private Coroutine cambio;

        private float factorFundido = 1f;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            fuente = gameObject.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;
            fuente.ignoreListenerPause = true;

            AplicarVolumen();

            GestorMenus.AlCambiarPantalla += AlCambiarPantalla;
            AjustesJuego.AlCambiarAjustes += AplicarVolumen;
        }

        private void Start()
        {
            if (EstadoPartida.Instancia != null)
            {
                EstadoPartida.Instancia.AlEmpezarNivel += AlEmpezarNivel;
            }

            if (GestorMenus.Instancia != null && GestorMenus.Instancia.PantallaActual == PantallaMenu.Principal)
            {
                Reproducir("menu", enLoop: true);
            }
        }

        private void OnDestroy()
        {
            if (Instancia != this)
            {
                return;
            }

            Instancia = null;
            GestorMenus.AlCambiarPantalla -= AlCambiarPantalla;
            AjustesJuego.AlCambiarAjustes -= AplicarVolumen;

            if (EstadoPartida.Instancia != null)
            {
                EstadoPartida.Instancia.AlEmpezarNivel -= AlEmpezarNivel;
            }
        }

        private void AlCambiarPantalla(PantallaMenu pantalla)
        {
            switch (pantalla)
            {
                case PantallaMenu.Principal:
                    Reproducir("menu", enLoop: true);
                    break;
                case PantallaMenu.Victoria:
                    Reproducir("victoria", enLoop: false);
                    break;
                case PantallaMenu.Derrota:
                    Reproducir("derrota", enLoop: false);
                    break;
            }
        }

        private void AlEmpezarNivel()
        {
            Reproducir(NombrePistaNivel(ProgresoNiveles.NivelActual), enLoop: true);
        }

        private static string NombrePistaNivel(int nivel)
        {
            for (int n = nivel; n >= 1; n--)
            {
                if (Resources.Load<AudioClip>(Carpeta + "nivel" + n) != null)
                {
                    return "nivel" + n;
                }
            }

            return "nivel" + nivel;
        }

        private void Reproducir(string nombre, bool enLoop)
        {
            AudioClip clip = Resources.Load<AudioClip>(Carpeta + nombre);

            if (clip != null && fuente.clip == clip && fuente.isPlaying && fuente.loop)
            {
                return;
            }

            if (clip == null)
            {
                Debug.LogWarning($"[GestorMusica] Falta la pista '{nombre}' en Resources/{Carpeta}.");
            }

            if (cambio != null)
            {
                StopCoroutine(cambio);
            }

            cambio = StartCoroutine(Cambiar(clip, enLoop));
        }

        private IEnumerator Cambiar(AudioClip clip, bool enLoop)
        {
            if (fuente.isPlaying)
            {
                for (float t = factorFundido * duracionFundido; t > 0f; t -= Time.unscaledDeltaTime)
                {
                    factorFundido = t / duracionFundido;
                    AplicarVolumen();
                    yield return null;
                }
            }

            fuente.Stop();
            fuente.clip = clip;
            fuente.loop = enLoop;
            factorFundido = 1f;
            AplicarVolumen();

            if (clip != null)
            {
                fuente.Play();
            }

            cambio = null;
        }

        private void AplicarVolumen()
        {
            if (fuente != null)
            {
                fuente.volume = AjustesJuego.VolumenMusica * factorFundido;
            }
        }
    }
}
