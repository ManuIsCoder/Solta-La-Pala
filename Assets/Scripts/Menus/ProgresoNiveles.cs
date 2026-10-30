using UnityEngine;

namespace SoltaLaPala.Menus
{
    // Guarda que niveles desbloqueo el jugador y con que puntuacion los termino.
    // Persiste en PlayerPrefs, igual que los controles y los ajustes.
    public static class ProgresoNiveles
    {
        // Cantidad de niveles del juego. Define cuantos botones dibuja el selector.
        public const int CantidadNiveles = 4;

        private const string PrefNivelMaximo = "progreso_nivelMaximo";
        private const string PrefijoImpacto = "progreso_impacto_";
        private const string PrefijoSospecha = "progreso_sospecha_";

        // Nivel que se esta jugando ahora. No se guarda: es de la sesion actual.
        public static int NivelActual { get; set; } = 1;

        // Nivel mas alto desbloqueado. El 1 siempre esta disponible.
        public static int NivelMaximoDesbloqueado
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PrefNivelMaximo, 1), 1, CantidadNiveles);
            set
            {
                int valor = Mathf.Clamp(value, 1, CantidadNiveles);

                // Solo subir: terminar otra vez un nivel viejo no debe bloquear los siguientes.
                if (valor > NivelMaximoDesbloqueado)
                {
                    PlayerPrefs.SetInt(PrefNivelMaximo, valor);
                    PlayerPrefs.Save();
                }
            }
        }

        // True si el nivel se puede jugar.
        public static bool EstaDesbloqueado(int nivel)
        {
            return nivel >= 1 && nivel <= NivelMaximoDesbloqueado;
        }

        // Desbloquea el nivel siguiente al que se acaba de completar.
        public static void CompletarNivel(int nivel)
        {
            NivelMaximoDesbloqueado = nivel + 1;
        }

        // Guarda el resultado de una partida para poder mostrarlo en Resultados.
        public static void GuardarResultado(int nivel, int impacto, int sospecha)
        {
            PlayerPrefs.SetInt(PrefijoImpacto + nivel, impacto);
            PlayerPrefs.SetInt(PrefijoSospecha + nivel, sospecha);
            PlayerPrefs.Save();
        }

        public static int ObtenerImpacto(int nivel)
        {
            return PlayerPrefs.GetInt(PrefijoImpacto + nivel, 0);
        }

        public static int ObtenerSospecha(int nivel)
        {
            return PlayerPrefs.GetInt(PrefijoSospecha + nivel, 0);
        }

        // Borra todo el progreso. Util para probar desde cero.
        public static void Reiniciar()
        {
            PlayerPrefs.DeleteKey(PrefNivelMaximo);

            for (int nivel = 1; nivel <= CantidadNiveles; nivel++)
            {
                PlayerPrefs.DeleteKey(PrefijoImpacto + nivel);
                PlayerPrefs.DeleteKey(PrefijoSospecha + nivel);
            }

            PlayerPrefs.Save();
            NivelActual = 1;
        }
    }
}
