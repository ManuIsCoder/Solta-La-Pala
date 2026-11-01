using UnityEngine;

namespace SoltaLaPala.Menus
{
    public static class ProgresoNiveles
    {
        public const int CantidadNiveles = 4;

        private const string PrefNivelMaximo = "progreso_nivelMaximo";
        private const string PrefijoImpacto = "progreso_impacto_";
        private const string PrefijoSospecha = "progreso_sospecha_";

        public static int NivelActual { get; set; } = 1;

        private static bool iniciarNivelAlCargarEscena;

        public static int NivelMaximoDesbloqueado
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PrefNivelMaximo, 1), 1, CantidadNiveles);
            set
            {
                int valor = Mathf.Clamp(value, 1, CantidadNiveles);

                if (valor > NivelMaximoDesbloqueado)
                {
                    PlayerPrefs.SetInt(PrefNivelMaximo, valor);
                    PlayerPrefs.Save();
                }
            }
        }

        public static bool EstaDesbloqueado(int nivel)
        {
            return nivel >= 1 && nivel <= NivelMaximoDesbloqueado;
        }

        public static void CompletarNivel(int nivel)
        {
            NivelMaximoDesbloqueado = nivel + 1;
        }

        public static string ObtenerEscena(int nivel)
        {
            return nivel >= 1 && nivel <= CantidadNiveles
                ? $"nivel {nivel}"
                : null;
        }

        public static void MarcarInicioTrasCarga()
        {
            iniciarNivelAlCargarEscena = true;
        }

        public static bool ConsumirInicioTrasCarga()
        {
            bool iniciar = iniciarNivelAlCargarEscena;
            iniciarNivelAlCargarEscena = false;
            return iniciar;
        }

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
