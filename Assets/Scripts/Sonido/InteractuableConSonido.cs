using SoltaLaPala.Interaction;
using UnityEngine;

namespace SoltaLaPala.Sonido
{
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
