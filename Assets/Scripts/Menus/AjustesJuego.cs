using System;
using UnityEngine;

namespace SoltaLaPala.Menus
{
    public static class AjustesJuego
    {
        private const string PrefVolumenGeneral = "ajustes_volumenGeneral";
        private const string PrefVolumenMusica = "ajustes_volumenMusica";
        private const string PrefVolumenEfectos = "ajustes_volumenEfectos";
        private const string PrefSensibilidadX = "ajustes_sensibilidadX";
        private const string PrefSensibilidadY = "ajustes_sensibilidadY";
        private const string PrefInvertirY = "ajustes_invertirY";
        private const string PrefPantallaCompleta = "ajustes_pantallaCompleta";

        public const float SensibilidadMinima = 20f;
        public const float SensibilidadMaxima = 600f;

        private static bool cargado;

        private static float volumenGeneral = 1f;
        private static float volumenMusica = 1f;
        private static float volumenEfectos = 1f;
        private static float sensibilidadX = 200f;
        private static float sensibilidadY = 150f;
        private static bool invertirY;
        private static bool pantallaCompleta = true;

        public static event Action AlCambiarAjustes;

        public static float VolumenGeneral
        {
            get { AsegurarCargado(); return volumenGeneral; }
            set
            {
                AsegurarCargado();
                volumenGeneral = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(PrefVolumenGeneral, volumenGeneral);
                AplicarVolumen();
                Notificar();
            }
        }

        public static float VolumenMusica
        {
            get { AsegurarCargado(); return volumenMusica; }
            set
            {
                AsegurarCargado();
                volumenMusica = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(PrefVolumenMusica, volumenMusica);
                Notificar();
            }
        }

        public static float VolumenEfectos
        {
            get { AsegurarCargado(); return volumenEfectos; }
            set
            {
                AsegurarCargado();
                volumenEfectos = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(PrefVolumenEfectos, volumenEfectos);
                Notificar();
            }
        }

        public static float SensibilidadX
        {
            get { AsegurarCargado(); return sensibilidadX; }
            set
            {
                AsegurarCargado();
                sensibilidadX = Mathf.Clamp(value, SensibilidadMinima, SensibilidadMaxima);
                PlayerPrefs.SetFloat(PrefSensibilidadX, sensibilidadX);
                Notificar();
            }
        }

        public static float SensibilidadY
        {
            get { AsegurarCargado(); return sensibilidadY; }
            set
            {
                AsegurarCargado();
                sensibilidadY = Mathf.Clamp(value, SensibilidadMinima, SensibilidadMaxima);
                PlayerPrefs.SetFloat(PrefSensibilidadY, sensibilidadY);
                Notificar();
            }
        }

        public static bool InvertirY
        {
            get { AsegurarCargado(); return invertirY; }
            set
            {
                AsegurarCargado();
                invertirY = value;
                PlayerPrefs.SetInt(PrefInvertirY, invertirY ? 1 : 0);
                Notificar();
            }
        }

        public static bool PantallaCompleta
        {
            get { AsegurarCargado(); return pantallaCompleta; }
            set
            {
                AsegurarCargado();
                pantallaCompleta = value;
                PlayerPrefs.SetInt(PrefPantallaCompleta, pantallaCompleta ? 1 : 0);
                AplicarPantalla();
                Notificar();
            }
        }

        public static void RestaurarPorDefecto()
        {
            AsegurarCargado();

            volumenGeneral = 1f;
            volumenMusica = 1f;
            volumenEfectos = 1f;
            sensibilidadX = 200f;
            sensibilidadY = 150f;
            invertirY = false;
            pantallaCompleta = true;

            GuardarTodo();
            AplicarVolumen();
            AplicarPantalla();
            Notificar();
        }

        public static void AplicarTodo()
        {
            AsegurarCargado();
            AplicarVolumen();
            AplicarPantalla();
        }

        private static void AsegurarCargado()
        {
            if (cargado)
            {
                return;
            }

            cargado = true;

            volumenGeneral = PlayerPrefs.GetFloat(PrefVolumenGeneral, 1f);
            volumenMusica = PlayerPrefs.GetFloat(PrefVolumenMusica, 1f);
            volumenEfectos = PlayerPrefs.GetFloat(PrefVolumenEfectos, 1f);
            sensibilidadX = PlayerPrefs.GetFloat(PrefSensibilidadX, 200f);
            sensibilidadY = PlayerPrefs.GetFloat(PrefSensibilidadY, 150f);
            invertirY = PlayerPrefs.GetInt(PrefInvertirY, 0) == 1;
            pantallaCompleta = PlayerPrefs.GetInt(PrefPantallaCompleta, 1) == 1;
        }

        private static void GuardarTodo()
        {
            PlayerPrefs.SetFloat(PrefVolumenGeneral, volumenGeneral);
            PlayerPrefs.SetFloat(PrefVolumenMusica, volumenMusica);
            PlayerPrefs.SetFloat(PrefVolumenEfectos, volumenEfectos);
            PlayerPrefs.SetFloat(PrefSensibilidadX, sensibilidadX);
            PlayerPrefs.SetFloat(PrefSensibilidadY, sensibilidadY);
            PlayerPrefs.SetInt(PrefInvertirY, invertirY ? 1 : 0);
            PlayerPrefs.SetInt(PrefPantallaCompleta, pantallaCompleta ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void AplicarVolumen()
        {
            AudioListener.volume = volumenGeneral;
        }

        private static void AplicarPantalla()
        {
            if (Screen.fullScreen != pantallaCompleta)
            {
                Screen.fullScreen = pantallaCompleta;
            }
        }

        private static void Notificar()
        {
            PlayerPrefs.Save();
            AlCambiarAjustes?.Invoke();
        }
    }
}
