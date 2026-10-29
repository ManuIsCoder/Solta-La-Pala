using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Minijuegos
{
    /// <summary>
    /// Interactuable para la Impresora (ubicada en la Sala de Impresora).
    /// Permite al jugador interactuar con [E] para abrir el minijuego de fotocopiar.
    /// 
    /// Verifica si el jugador tiene los Informes o la Foto de Chiqui Tapia en su inventario.
    /// Cuenta con un Switch de Activado / Desactivado editable desde el Inspector.
    /// </summary>
    [RequireComponent(typeof(ResaltadoInteractuable))]
    public class ImpresoraInteractuable : MonoBehaviour, IInteractuable
    {
        public enum EstadoSwitch
        {
            Activado,
            Desactivado
        }

        [Header("Interruptor del interactuable")]
        [Tooltip("Switch de Activado / Desactivado: habilita o deshabilita la interacción desde el Inspector")]
        public EstadoSwitch estado = EstadoSwitch.Activado;

        [Header("Referencia al Minijuego")]
        [Tooltip("Referencia al minijuego de fotocopiar. Si se deja vacía, se buscará automáticamente en la escena.")]
        public MinijuegoFotocopiar minijuego;

        [Header("IDs de Ítems requeridos")]
        public string idInforme = "Informe";
        public string idInformeAlternativo = "Documento";
        public string idFotoChiqui = "FotoChiqui";
        public string idFotoChiquiAlternativo = "fotoChiqui";

        public Transform Transform => transform;

        private void Awake()
        {
            BuscarMinijuegoSiEsNecesario();
        }

        private void BuscarMinijuegoSiEsNecesario()
        {
            if (minijuego == null)
            {
                minijuego = FindFirstObjectByType<MinijuegoFotocopiar>(FindObjectsInactive.Include);
            }
        }

        public bool JugadorTieneItemValido(GameObject quien)
        {
            BuscarMinijuegoSiEsNecesario();
            if (minijuego != null && minijuego.modoPrueba) return true;

            var jugador = quien != null ? quien : GameObject.FindGameObjectWithTag("Player");
            if (jugador == null) return false;

            var inv = jugador.GetComponent<InventarioJugador>();
            if (inv == null) return false;

            return inv.TieneItem(idInforme)
                || (!string.IsNullOrEmpty(idInformeAlternativo) && inv.TieneItem(idInformeAlternativo))
                || inv.TieneItem("Papeles")
                || inv.TieneItem(idFotoChiqui)
                || (!string.IsNullOrEmpty(idFotoChiquiAlternativo) && inv.TieneItem(idFotoChiquiAlternativo))
                || inv.TieneItem("Dibujo");
        }

        public string ObtenerTextoInteraccion()
        {
            if (estado == EstadoSwitch.Desactivado)
                return string.Empty;

            if (!JugadorTieneItemValido(null))
            {
                return "[E] Fotocopiar (Requiere Informes o Foto de Chiqui)";
            }

            return "[E] Usar impresora (Fotocopiar)";
        }

        public bool PuedeInteractuar()
        {
            if (estado != EstadoSwitch.Activado) return false;
            if (GestorMinijuego.Instancia == null || GestorMinijuego.Instancia.MinijuegoActivo) return false;

            return true;
        }

        public void Interactuar(GameObject quienInteractua)
        {
            if (estado == EstadoSwitch.Desactivado) return;

            if (!JugadorTieneItemValido(quienInteractua))
            {
                Debug.Log("[ImpresoraInteractuable] No tienes Informes ni la Foto de Chiqui en el inventario.");
                return;
            }

            BuscarMinijuegoSiEsNecesario();

            if (minijuego == null)
            {
                // Si aún no existe en la escena, creamos un contenedor automáticamente en tiempo de ejecución
                GameObject go = new GameObject("MinijuegoFotocopiar_Auto", typeof(RectTransform), typeof(MinijuegoFotocopiar));
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    go.transform.SetParent(canvas.transform, false);
                }
                minijuego = go.GetComponent<MinijuegoFotocopiar>();
                minijuego.panelRaiz = go;
            }

            var inv = quienInteractua != null ? quienInteractua.GetComponent<InventarioJugador>() : null;
            GestorMinijuego.Instancia?.AbrirMinijuego(minijuego, inv);
        }
    }
}
