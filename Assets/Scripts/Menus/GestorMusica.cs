using System.Collections;
using UnityEngine;

namespace SoltaLaPala.Menus
{
    // Pone la musica que corresponde en cada momento:
    //   menu principal      -> Resources/Audio/Musica/menu
    //   nivel 1, 2, 3       -> nivel1, nivel2, nivel3 (los niveles de mas usan la ultima)
    //   pantalla de victoria -> victoria (una vez, sin loop)
    //   pantalla de derrota  -> derrota  (una vez, sin loop)
    //
    // Todas las pistas siguen la barra de "Musica" de la configuracion. El volumen
    // general ya lo aplica AjustesJuego sobre el AudioListener.
    //
    // Si falta una pista, ese momento queda en silencio en vez de seguir sonando
    // la anterior.
    public class GestorMusica : MonoBehaviour
    {
        public static GestorMusica Instancia { get; private set; }

        private const string Carpeta = "Audio/Musica/";

        [Tooltip("Segundos que tarda en apagarse una pista y prenderse la siguiente.")]
        public float duracionFundido = 0.6f;

        private AudioSource fuente;
        private Coroutine cambio;

        // 0..1: lo usa el fundido para bajar y subir sin pisar el volumen de los ajustes.
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
            // La musica no se posiciona en el mundo.
            fuente.spatialBlend = 0f;
            // Sigue sonando con el juego pausado (timeScale 0 no frena el audio,
            // pero AudioListener.pause si; la musica de menu la ignora).
            fuente.ignoreListenerPause = true;

            AplicarVolumen();

            // En Awake: el menu principal se abre en el Start de GestorMenus y hay
            // que estar escuchando antes.
            GestorMenus.AlCambiarPantalla += AlCambiarPantalla;
            AjustesJuego.AlCambiarAjustes += AplicarVolumen;
        }

        private void Start()
        {
            if (EstadoPartida.Instancia != null)
            {
                EstadoPartida.Instancia.AlEmpezarNivel += AlEmpezarNivel;
            }

            // Por si el menu principal se abrio antes de que este objeto existiera.
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

        // Pausa y configuracion no cambian la musica: se sigue oyendo la del nivel
        // o la del menu, segun de donde se vino.
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

        // nivel1, nivel2, ... Si el nivel no tiene pista propia se usa la del
        // ultimo nivel que si tenga.
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

        // Cambia de pista con un fundido corto. Si ya esta sonando esa misma en
        // loop no hace nada: volver al menu principal desde configuracion no
        // tiene que reiniciar la cancion.
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

        // Tiempo real: los menus abren con Time.timeScale en 0.
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
