using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Minijuegos
{
    /// <summary>
    /// Interactuable para la computadora (Laptop_Off).
    /// Cuenta con:
    /// 1. Switch de Estado ("Activado" / "Desactivado"): permite o bloquea la interacción desde el inspector.
    /// 2. Switch de Minijuego ("modoJuego"): al ser un objeto que se repite en varias tareas,
    ///    permite elegir desde el inspector qué juego se ejecuta al interactuar:
    ///    - Borrar datos de la computadora (Nivel 2)
    ///    - Entregar documentos (Nivel 4)
    ///    - Discurso (Nivel 1)
    /// </summary>
    [RequireComponent(typeof(ResaltadoInteractuable))]
    public class ComputadoraInteractuable : MonoBehaviour, IInteractuable
    {
        public enum EstadoSwitch
        {
            Activado,
            Desactivado
        }

        public enum ModoJuegoComputadora
        {
            BorrarDatos,
            EntregarDocumentos,
            Discurso
        }

        [Header("Interruptores de la computadora")]
        [Tooltip("Switch de Activado / Desactivado: habilita o deshabilita la interacción con este objeto")]
        public EstadoSwitch estado = EstadoSwitch.Activado;

        [Tooltip("Switch para cambiar qué minijuego ejecuta este interactuable")]
        public ModoJuegoComputadora modoJuego = ModoJuegoComputadora.BorrarDatos;

        [Header("Referencias a los minijuegos")]
        [Tooltip("Minijuego de borrar datos de la computadora")]
        public MinijuegoBorrarDatos minijuegoBorrarDatos;

        [Tooltip("Minijuego de entregar documentos (opcional para cuando se implemente)")]
        public MinijuegoBase minijuegoEntregarDocumentos;

        [Tooltip("Minijuego de discurso (opcional para cuando se implemente)")]
        public MinijuegoBase minijuegoDiscurso;

        public Transform Transform => transform;

        public string ObtenerTextoInteraccion()
        {
            if (estado == EstadoSwitch.Desactivado)
                return string.Empty;

            switch (modoJuego)
            {
                case ModoJuegoComputadora.BorrarDatos:
                    return "[E] Usar computadora (Borrar datos)";
                case ModoJuegoComputadora.EntregarDocumentos:
                    return "[E] Usar computadora (Entregar documentos)";
                case ModoJuegoComputadora.Discurso:
                    return "[E] Usar computadora (Discurso)";
                default:
                    return "[E] Usar computadora";
            }
        }

        public bool PuedeInteractuar()
        {
            return estado == EstadoSwitch.Activado
                && GestorMinijuego.Instancia != null
                && !GestorMinijuego.Instancia.MinijuegoActivo;
        }

        public void Interactuar(GameObject quienInteractua)
        {
            if (estado == EstadoSwitch.Desactivado) return;

            var inv = quienInteractua.GetComponent<InventarioJugador>();
            MinijuegoBase minijuegoAIniciar = ObtenerMinijuegoActual();

            if (minijuegoAIniciar != null)
            {
                GestorMinijuego.Instancia?.AbrirMinijuego(minijuegoAIniciar, inv);
            }
            else
            {
                Debug.LogWarning($"[ComputadoraInteractuable] El minijuego para el modo '{modoJuego}' no está asignado en el Inspector.");
            }
        }

        private MinijuegoBase ObtenerMinijuegoActual()
        {
            switch (modoJuego)
            {
                case ModoJuegoComputadora.BorrarDatos:
                    if (minijuegoBorrarDatos == null)
                    {
                        minijuegoBorrarDatos = FindObjectOfType<MinijuegoBorrarDatos>(true);
                    }
                    return minijuegoBorrarDatos;
                case ModoJuegoComputadora.EntregarDocumentos:
                    return minijuegoEntregarDocumentos;
                case ModoJuegoComputadora.Discurso:
                    return minijuegoDiscurso;
                default:
                    return null;
            }
        }
    }
}
