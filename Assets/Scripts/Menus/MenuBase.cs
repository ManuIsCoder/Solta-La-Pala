using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    public abstract class MenuBase : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Raiz de la pantalla. Si se deja vacio se genera por codigo.")]
        public GameObject panel;

        protected Canvas canvas;

        private bool construida;

        protected virtual int OrdenPantalla => 100;

        protected virtual void Awake()
        {
            AsegurarConstruida();
            EstablecerVisible(false);
        }

        protected void AsegurarConstruida()
        {
            if (construida)
            {
                return;
            }

            construida = true;

            if (panel == null)
            {
                ConstructorUI.AsegurarEventSystem();
                canvas = ConstructorUI.CrearCanvas($"Canvas{GetType().Name}", transform, OrdenPantalla);
                panel = canvas.gameObject;
                Construir(canvas.transform);
            }
            else
            {
                Enlazar();
            }
        }

        protected abstract void Construir(Transform raiz);

        protected virtual void Enlazar() { }

        protected static void Enganchar(Button boton, UnityEngine.Events.UnityAction accion)
        {
            if (boton != null)
            {
                boton.onClick.AddListener(accion);
            }
        }

        public virtual void EstablecerVisible(bool visible)
        {
            AsegurarConstruida();

            if (panel != null)
            {
                panel.SetActive(visible);
            }

            if (visible)
            {
                AlMostrar();
            }
        }

        protected virtual void AlMostrar() { }

        protected GestorMenus Gestor => GestorMenus.Instancia;

        protected GameObject CrearFondoYPanel(Transform raiz, Vector2 tamanoPanel)
        {
            ConstructorUI.CrearFondoPantalla(raiz, ConstructorUI.ColorFondoPantalla);
            return ConstructorUI.CrearPanel("Panel", raiz, tamanoPanel);
        }

        protected Text CrearTitulo(Transform padre, string texto, float anchoPanel)
        {
            Text titulo = ConstructorUI.CrearTexto("Titulo", padre, texto, 46,
                TextAnchor.MiddleCenter, new Vector2(0f, -40f), new Vector2(anchoPanel - 80f, 60f));
            titulo.color = ConstructorUI.ColorAcento;
            return titulo;
        }
    }
}
