using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoltaLaPala.Menus
{
    public static class ControlesJuego
    {
        private const string PrefijoPref = "controles_";

        private static readonly Dictionary<AccionJuego, KeyCode> teclasPorDefecto =
            new Dictionary<AccionJuego, KeyCode>
            {
                { AccionJuego.Adelante, KeyCode.W },
                { AccionJuego.Atras, KeyCode.S },
                { AccionJuego.Izquierda, KeyCode.A },
                { AccionJuego.Derecha, KeyCode.D },
                { AccionJuego.Correr, KeyCode.LeftShift },
                { AccionJuego.Interactuar, KeyCode.E },
                { AccionJuego.SoltarObjeto, KeyCode.Q },
                { AccionJuego.AvanzarDialogo, KeyCode.Space },
                { AccionJuego.Slot1, KeyCode.Alpha1 },
                { AccionJuego.Slot2, KeyCode.Alpha2 },
                { AccionJuego.Slot3, KeyCode.Alpha3 },
                { AccionJuego.CambiarVista, KeyCode.V },
                { AccionJuego.Pausa, KeyCode.Escape }
            };

        private static readonly Dictionary<AccionJuego, string> nombresVisibles =
            new Dictionary<AccionJuego, string>
            {
                { AccionJuego.Adelante, "Avanzar" },
                { AccionJuego.Atras, "Retroceder" },
                { AccionJuego.Izquierda, "Izquierda" },
                { AccionJuego.Derecha, "Derecha" },
                { AccionJuego.Correr, "Correr" },
                { AccionJuego.Interactuar, "Interactuar" },
                { AccionJuego.SoltarObjeto, "Soltar objeto" },
                { AccionJuego.AvanzarDialogo, "Avanzar dialogo" },
                { AccionJuego.Slot1, "Objeto 1" },
                { AccionJuego.Slot2, "Objeto 2" },
                { AccionJuego.Slot3, "Objeto 3" },
                { AccionJuego.CambiarVista, "Cambiar vista" },
                { AccionJuego.Pausa, "Pausa" }
            };

        private static Dictionary<AccionJuego, KeyCode> teclasActuales;

        public static event Action AlCambiarControles;

        public static AccionJuego[] TodasLasAcciones =>
            (AccionJuego[])Enum.GetValues(typeof(AccionJuego));

        public static KeyCode ObtenerTecla(AccionJuego accion)
        {
            AsegurarCargado();

            return teclasActuales.TryGetValue(accion, out KeyCode tecla)
                ? tecla
                : KeyCode.None;
        }

        public static string ObtenerNombre(AccionJuego accion)
        {
            return nombresVisibles.TryGetValue(accion, out string nombre)
                ? nombre
                : accion.ToString();
        }

        public static void AsignarTecla(AccionJuego accion, KeyCode tecla)
        {
            AsegurarCargado();

            foreach (AccionJuego otra in TodasLasAcciones)
            {
                if (otra != accion && teclasActuales[otra] == tecla)
                {
                    teclasActuales[otra] = KeyCode.None;
                    Guardar(otra, KeyCode.None);
                }
            }

            teclasActuales[accion] = tecla;
            Guardar(accion, tecla);

            AlCambiarControles?.Invoke();
        }

        public static void RestaurarPorDefecto()
        {
            teclasActuales = new Dictionary<AccionJuego, KeyCode>(teclasPorDefecto);

            foreach (AccionJuego accion in TodasLasAcciones)
            {
                PlayerPrefs.DeleteKey(PrefijoPref + accion);
            }

            PlayerPrefs.Save();
            AlCambiarControles?.Invoke();
        }

        public static bool Mantenida(AccionJuego accion)
        {
            KeyCode tecla = ObtenerTecla(accion);
            return tecla != KeyCode.None && Input.GetKey(tecla);
        }

        public static bool Pulsada(AccionJuego accion)
        {
            KeyCode tecla = ObtenerTecla(accion);
            return tecla != KeyCode.None && Input.GetKeyDown(tecla);
        }

        public static float Eje(AccionJuego negativa, AccionJuego positiva)
        {
            float valor = 0f;

            if (Mantenida(positiva)) valor += 1f;
            if (Mantenida(negativa)) valor -= 1f;

            return valor;
        }

        private static void AsegurarCargado()
        {
            if (teclasActuales != null)
            {
                return;
            }

            teclasActuales = new Dictionary<AccionJuego, KeyCode>();

            foreach (AccionJuego accion in TodasLasAcciones)
            {
                KeyCode porDefecto = teclasPorDefecto.TryGetValue(accion, out KeyCode valor)
                    ? valor
                    : KeyCode.None;

                teclasActuales[accion] = LeerGuardada(accion, porDefecto);
            }
        }

        private static KeyCode LeerGuardada(AccionJuego accion, KeyCode porDefecto)
        {
            string guardada = PlayerPrefs.GetString(PrefijoPref + accion, "");

            if (string.IsNullOrEmpty(guardada))
            {
                return porDefecto;
            }

            return Enum.TryParse(guardada, out KeyCode tecla) ? tecla : porDefecto;
        }

        private static void Guardar(AccionJuego accion, KeyCode tecla)
        {
            PlayerPrefs.SetString(PrefijoPref + accion, tecla.ToString());
            PlayerPrefs.Save();
        }
    }
}
