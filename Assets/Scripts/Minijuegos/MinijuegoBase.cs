using UnityEngine;
using SoltaLaPala.Inventory;

namespace SoltaLaPala.Minijuegos
{
    public abstract class MinijuegoBase : MonoBehaviour
    {
        protected virtual void Start()
        {
            // Si el panel arranca activo en la escena para pruebas directas
            if (panelRaiz != null && panelRaiz.activeInHierarchy && Inventario == null)
            {
                Iniciar(InventarioJugador.DelJugador(), GestorMinijuego.Instancia);
            }
        }

        protected GestorMinijuego Gestor { get; private set; }

        [Header("Canvas del minijuego")]
        public GameObject panelRaiz;
        protected InventarioJugador Inventario { get; private set; }

        public void Iniciar(InventarioJugador inventario, GestorMinijuego gestor)
        {
            Inventario = inventario;
            Gestor = gestor;

            if (panelRaiz != null)
                panelRaiz.SetActive(true);
            
            AlIniciar();
        }

        public void Salir()
        {
            AlSalir();

            if (panelRaiz != null)
                panelRaiz.SetActive(false);

            Gestor?.CerrarMinijuego();
        }

        protected abstract void AlIniciar();

        protected virtual void AlSalir() { }
    }
}
