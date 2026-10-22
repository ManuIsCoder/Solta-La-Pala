using SoltaLaPala.Interaction;
using UnityEngine;

namespace SoltaLaPala.Sonido
{
    // Objeto con el que se interactua con [E] y que hace sonar su SonidoObjeto.
    // Es el que usan el telefono, la impresora y el microondas de los prefabs
    // "con sonido".
    //
    // Necesita un Collider para que el jugador lo apunte.
    [RequireComponent(typeof(ResaltadoInteractuable))]
    [RequireComponent(typeof(SonidoObjeto))]
    public class InteractuableConSonido : MonoBehaviour, IInteractuable
    {
        [Tooltip("Texto que sale abajo a la derecha al apuntar al objeto. Ej: [E] Usar microondas")]
        public string textoInteraccion = "[E] Usar";

        private SonidoObjeto sonido;

        public Transform Transform => transform;

        private void Awake()
        {
            sonido = GetComponent<SonidoObjeto>();
        }

        public string ObtenerTextoInteraccion() => textoInteraccion;

        // Mientras suena no se puede volver a usar, para que no se pisen los audios.
        public bool PuedeInteractuar() => sonido == null || !sonido.Reproduciendo;

        public void Interactuar(GameObject quienInteractua)
        {
            if (sonido != null)
            {
                sonido.Reproducir();
            }
        }
    }
}
