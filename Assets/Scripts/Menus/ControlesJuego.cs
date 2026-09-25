using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoltaLaPala.Menus
{
    // Mapa central de teclas del juego. Es la unica fuente de verdad sobre que tecla
    // hace que: el resto de scripts pregunta aca en vez de tener el KeyCode hardcodeado,
    // asi el menu de configuracion puede reasignar cualquier accion.
    //
    // Es una clase estatica (no un MonoBehaviour) para que funcione desde el primer frame
    // sin depender de que haya un objeto puesto en la escena.
    public static class ControlesJuego
    {
        // Prefijo de las claves de PlayerPrefs, para no pisar otros ajustes guardados.
        private const string PrefijoPref = "controles_";

        // Teclas de fabrica. Son las que tenian hardcodeadas los scripts originales.
        private static readonly Dictionary<AccionJuego, KeyCode> teclasPorDefecto =
            new Dictionary<AccionJuego, KeyCode>
            {
                { AccionJuego.Adelante, KeyCode.W },
                { AccionJuego.Atras, KeyCode.S },
                { AccionJuego.Izquierda, KeyCode.A },
                { AccionJuego.Derecha, KeyCode.D },
                { AccionJuego.Correr, KeyCode.LeftShift },
                { AccionJuego.Interactuar, KeyCode.E },
                { AccionJuego.AvanzarDialogo, KeyCode.Space },
                { AccionJuego.Slot1, KeyCode.Alpha1 },
                { AccionJuego.Slot2, KeyCode.Alpha2 },
                { AccionJuego.Slot3, KeyCode.Alpha3 },
                { AccionJuego.CambiarVista, KeyCode.V },
                { AccionJuego.Pausa, KeyCode.Escape }
            };

        // Nombres que se muestran en el menu de configuracion.
        private static readonly Dictionary<AccionJuego, string> nombresVisibles =
            new Dictionary<AccionJuego, string>
            {
                { AccionJuego.Adelante, "Avanzar" },
                { AccionJuego.Atras, "Retroceder" },
                { AccionJuego.Izquierda, "Izquierda" },
                { AccionJuego.Derecha, "Derecha" },
                { AccionJuego.Correr, "Correr" },
                { AccionJuego.Interactuar, "Interactuar" },
                { AccionJuego.AvanzarDialogo, "Avanzar dialogo" },
                { AccionJuego.Slot1, "Objeto 1" },
                { AccionJuego.Slot2, "Objeto 2" },
                { AccionJuego.Slot3, "Objeto 3" },
                { AccionJuego.CambiarVista, "Cambiar vista" },
                { AccionJuego.Pausa, "Pausa" }
            };

        // Teclas actualmente en uso. Se llena la primera vez que alguien pregunta algo.
        private static Dictionary<AccionJuego, KeyCode> teclasActuales;

        // Avisa cuando cambia cualquier asignacion, para que la UI se redibuje.
        public static event Action AlCambiarControles;

        // Todas las acciones en el orden del enum. Lo usa el menu para listarlas.
        public static AccionJuego[] TodasLasAcciones =>
            (AccionJuego[])Enum.GetValues(typeof(AccionJuego));

        // Tecla asignada a una accion. Si nunca se cargo nada, carga desde PlayerPrefs.
        public static KeyCode ObtenerTecla(AccionJuego accion)
        {
            AsegurarCargado();

            return teclasActuales.TryGetValue(accion, out KeyCode tecla)
                ? tecla
                : KeyCode.None;
        }

        // Nombre legible de la accion para mostrar en pantalla.
        public static string ObtenerNombre(AccionJuego accion)
        {
            return nombresVisibles.TryGetValue(accion, out string nombre)
                ? nombre
                : accion.ToString();
        }

        // Asigna una tecla nueva a una accion y lo guarda.
        // Si la tecla ya estaba en uso por otra accion, la otra se queda sin tecla
        // (KeyCode.None) para que nunca haya dos acciones con la misma tecla.
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

        // Vuelve a las teclas de fabrica y borra lo guardado.
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

        // True mientras la tecla de la accion se mantiene pulsada (movimiento, correr).
        public static bool Mantenida(AccionJuego accion)
        {
            KeyCode tecla = ObtenerTecla(accion);
            return tecla != KeyCode.None && Input.GetKey(tecla);
        }

        // True solo en el frame en que se pulsa la tecla (interactuar, pausa, slots).
        public static bool Pulsada(AccionJuego accion)
        {
            KeyCode tecla = ObtenerTecla(accion);
            return tecla != KeyCode.None && Input.GetKeyDown(tecla);
        }

        // Eje -1/0/1 a partir de dos acciones opuestas. Reemplaza a Input.GetAxisRaw
        // para que las teclas de movimiento tambien se puedan reasignar.
        public static float Eje(AccionJuego negativa, AccionJuego positiva)
        {
            float valor = 0f;

            if (Mantenida(positiva)) valor += 1f;
            if (Mantenida(negativa)) valor -= 1f;

            return valor;
        }

        // Carga las teclas guardadas la primera vez que hace falta.
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

        // Lee una tecla de PlayerPrefs. Si no hay nada guardado o el valor esta corrupto,
        // devuelve la tecla de fabrica.
        private static KeyCode LeerGuardada(AccionJuego accion, KeyCode porDefecto)
        {
            string guardada = PlayerPrefs.GetString(PrefijoPref + accion, "");

            if (string.IsNullOrEmpty(guardada))
            {
                return porDefecto;
            }

            return Enum.TryParse(guardada, out KeyCode tecla) ? tecla : porDefecto;
        }

        // Guarda una tecla concreta en PlayerPrefs.
        private static void Guardar(AccionJuego accion, KeyCode tecla)
        {
            PlayerPrefs.SetString(PrefijoPref + accion, tecla.ToString());
            PlayerPrefs.Save();
        }
    }
}
