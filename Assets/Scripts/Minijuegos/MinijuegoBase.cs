using UnityEngine;
using SoltaLaPala.Inventory;

namespace SoltaLaPala.Minijuegos
{
    public abstract class MinijuegoBase : MonoBehaviour
    {
        protected virtual void Start()
        {
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

        // Lo llama EstadoPartida al reiniciar el nivel para que el minijuego vuelva a su estado inicial.
        public virtual void Reiniciar() { }
    }
}
