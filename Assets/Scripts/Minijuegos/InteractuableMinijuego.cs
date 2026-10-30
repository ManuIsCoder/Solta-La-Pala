using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Minijuegos
{
    public enum EstadoInteractuable
    {
        Activado,
        Desactivado
    }

    public enum ModoMinijuegoInteractuable
    {
        Discurso,
        Microondas,
        Cafetera,
        RegarPlanta,
        DestaparBaño,
        BorrarDatosComputadora,
        RedactarDocumentos,
        FotocopiarImagen,
        EntregarDocumentos
    }

    [RequireComponent(typeof(ResaltadoInteractuable))]
    public class InteractuableMinijuego : MonoBehaviour, IInteractuable
    {
        [Header("Switch de Estado (Activado / Desactivado)")]
        [Tooltip("Si está Desactivado, no se puede interactuar ni jugar a este minijuego.")]
        public EstadoInteractuable estado = EstadoInteractuable.Activado;

        [Header("Switch de Tipo de Minijuego")]
        [Tooltip("Selecciona qué minijuego abre este objeto (ideal para la Laptop que se reutiliza).")]
        public ModoMinijuegoInteractuable tipoMinijuego;

        [Header("Texto flotante de interacción")]
        public string textoInteraccion = "[E] Usar";

        [Header("Referencia directa opcional (deja vacío para buscar por tipo)")]
        public MinijuegoBase minijuegoDirecto;

        public Transform Transform => transform;

        public string ObtenerTextoInteraccion()
        {
            if (estado == EstadoInteractuable.Desactivado) return string.Empty;
            return !string.IsNullOrEmpty(textoInteraccion) ? textoInteraccion : $"[E] Usar {tipoMinijuego}";
        }

        public bool PuedeInteractuar()
        {
            // Si el switch está desactivado, no se puede jugar
            if (estado == EstadoInteractuable.Desactivado) return false;

            // No interactuar si ya hay un minijuego activo
            if (GestorMinijuego.Instancia != null && GestorMinijuego.Instancia.MinijuegoActivo) return false;

            return true;
        }

        public void Interactuar(GameObject quienInteractua)
        {
            if (!PuedeInteractuar()) return;

            var inv = quienInteractua.GetComponent<InventarioJugador>();
            MinijuegoBase minijuego = ObtenerMinijuego();

            if (minijuego != null)
            {
                GestorMinijuego.Instancia?.AbrirMinijuego(minijuego, inv);
            }
            else
            {
                Debug.LogWarning($"[InteractuableMinijuego] No se encontró el componente de minijuego para '{tipoMinijuego}' en la escena.", this);
            }
        }

        private MinijuegoBase ObtenerMinijuego()
        {
            if (minijuegoDirecto != null) return minijuegoDirecto;

            // Busca el minijuego en la escena según el tipo seleccionado en el switch
            return tipoMinijuego switch
            {
                ModoMinijuegoInteractuable.Cafetera => FindAnyObjectByType<MinijuegoCafetera>(FindObjectsInactive.Include),
                ModoMinijuegoInteractuable.Microondas => FindAnyObjectByType<MinijuegoMicroondas>(FindObjectsInactive.Include),
                ModoMinijuegoInteractuable.Discurso => FindAnyObjectByType<MinijuegoDiscurso>(FindObjectsInactive.Include),
                ModoMinijuegoInteractuable.RegarPlanta => FindAnyObjectByType<MinijuegoRegarPlanta>(FindObjectsInactive.Include),
                ModoMinijuegoInteractuable.DestaparBaño => FindAnyObjectByType<MinijuegoDestaparBaño>(FindObjectsInactive.Include),
                _ => null
            };
        }
    }
}