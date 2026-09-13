using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Interaction
{
    // Cartel abajo a la derecha, justo encima de los slots del inventario,
    // que dice de que forma se puede interactuar con el objeto que estas mirando.
    public class InterfazTextoInteraccion : MonoBehaviour
    {
        [Header("Referencias")]
        public GameObject raiz;
        public Text texto;

        private void Awake()
        {
            if (raiz == null) raiz = gameObject;
            if (texto == null) texto = GetComponentInChildren<Text>();
            Ocultar();
        }

        // Muestra el cartel con el texto indicado (ej: "[E] Recoger pala").
        public void Mostrar(string mensaje)
        {
            if (texto != null) texto.text = mensaje;
            if (raiz != null) raiz.SetActive(true);
        }

        // Oculta el cartel cuando no hay ningun interactuable delante.
        public void Ocultar()
        {
            if (raiz != null) raiz.SetActive(false);
        }
    }
}
