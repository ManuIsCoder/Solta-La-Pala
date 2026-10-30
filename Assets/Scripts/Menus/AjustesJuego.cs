using System;
using UnityEngine;

namespace SoltaLaPala.Menus
{
    // Ajustes de audio, camara y pantalla. Igual que ControlesJuego es estatica y
    // persiste en PlayerPrefs, para que el menu de configuracion los pueda tocar
    // desde cualquier pantalla y sobrevivan al cierre del juego.
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

        // Avisa cuando cambia cualquier ajuste, para que la camara y la UI se actualicen.
        public static event Action AlCambiarAjustes;

        // Volumen maestro (0..1). Se aplica directamente al AudioListener.
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

        // Volumen de musica (0..1). Guardado para cuando se enganche el audio del juego.
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

        // Volumen de efectos (0..1).
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

        // Sensibilidad horizontal del raton.
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

        // Sensibilidad vertical del raton.
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

        // Invertir el eje vertical del raton.
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

        // Pantalla completa si/no. Se aplica al momento sobre la ventana.
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

        // Vuelve a los valores de fabrica.
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

        // Aplica los ajustes ya cargados al motor. Lo llama el arranque del juego
        // para que el volumen y la pantalla respeten lo guardado desde el primer frame.
        public static void AplicarTodo()
        {
            AsegurarCargado();
            AplicarVolumen();
            AplicarPantalla();
        }

        // Lee todo de PlayerPrefs la primera vez que alguien pregunta algo.
        private static void AsegurarCargado()
        {
            if (cargado)
            {
                return;
            }

            // Se marca cargado antes de leer: los setters llaman a AsegurarCargado()
            // y sin esto la primera escritura volveria a entrar aca en bucle.
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
            // Solo tocar la ventana si hace falta: cambiarla cada frame da tirones.
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
