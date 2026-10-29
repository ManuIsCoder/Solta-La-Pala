using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Interaction
{
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

        public void Mostrar(string mensaje)
        {
            if (texto != null) texto.text = mensaje;
            if (raiz != null) raiz.SetActive(true);
        }

        public void Ocultar()
        {
            if (raiz != null) raiz.SetActive(false);
        }
    }
}
