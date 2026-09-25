using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Base comun de todas las pantallas de menu.
    //
    // PROTOTIPO: si no se asigna un panel a mano en el inspector, la pantalla se
    // construye sola al arrancar (mismo criterio que InterfazInventario). Cuando haya
    // arte se arma el panel en la escena, se arrastra al campo y la generacion se apaga sola.
    public abstract class MenuBase : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Raiz de la pantalla. Si se deja vacio se genera por codigo.")]
        public GameObject panel;

        // Canvas propio de esta pantalla, cuando se genera por codigo.
        protected Canvas canvas;

        // True cuando la UI ya fue construida o asignada.
        private bool construida;

        // Orden de dibujo del canvas: las pantallas con numero mas alto tapan a las bajas.
        protected virtual int OrdenPantalla => 100;

        protected virtual void Awake()
        {
            AsegurarConstruida();
            EstablecerVisible(false);
        }

        // Construye la pantalla si hace falta. Se llama tanto desde Awake como desde
        // EstablecerVisible, porque el gestor puede pedir mostrar una pantalla antes
        // de que su propio Awake haya corrido.
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
        }

        // Cada pantalla arma aca su contenido dentro del canvas que recibe.
        protected abstract void Construir(Transform raiz);

        // Enciende o apaga la pantalla. Al abrirse se refresca por si los datos
        // que muestra cambiaron desde la ultima vez.
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

        // Gancho para que cada pantalla actualice lo que muestra al abrirse.
        protected virtual void AlMostrar() { }

        // Acceso corto al gestor, que es quien navega entre pantallas.
        protected GestorMenus Gestor => GestorMenus.Instancia;

        // Helpers que usan casi todas las pantallas.

        // Fondo oscuro + panel centrado, que es la estructura repetida en cada menu.
        protected GameObject CrearFondoYPanel(Transform raiz, Vector2 tamanoPanel)
        {
            ConstructorUI.CrearFondoPantalla(raiz, ConstructorUI.ColorFondoPantalla);
            return ConstructorUI.CrearPanel("Panel", raiz, tamanoPanel);
        }

        // Titulo grande arriba del panel.
        protected Text CrearTitulo(Transform padre, string texto, float anchoPanel)
        {
            Text titulo = ConstructorUI.CrearTexto("Titulo", padre, texto, 46,
                TextAnchor.MiddleCenter, new Vector2(0f, -40f), new Vector2(anchoPanel - 80f, 60f));
            titulo.color = ConstructorUI.ColorAcento;
            return titulo;
        }
    }
}
