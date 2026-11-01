using SoltaLaPala.Inventory;
using SoltaLaPala.Guardado;
using SoltaLaPala.Menus;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SoltaLaPala.Minijuegos
{
    public class MinijuegoMicroondas : MinijuegoBase
    {
        [Header("IDs de ítems")]
        public string idComida = "Sandwich";
        public string idPlato = "PlatoComida";
        public string idTenedor = "Tenedor";
        public string idComidaCaliente = "SandwichCaliente";

        [Header("Fondo UI")]
        public Image imagenFondo;

        [Header("UI")]
        public Button botonEncender;
        public Button botonTenedor;
        public Button botonSalir;
        public Slider barraCalentado;
        public GameObject particulasHumo;

        [Header("Ajustes")]
        public float velocidadCalentado = 0.15f;
        [Tooltip("Activa esto para probar el minijuego sin requerir tener plato o tenedor en el inventario")]
        public bool modoPrueba = false;

        private bool calentando;
        private bool tenedorAdentro;

        protected override void AlIniciar()
        {
            bool tienePlato = modoPrueba || (Inventario != null && (Inventario.TieneItem(idComida) || Inventario.TieneItem(idPlato)));
            bool tienetenedor = modoPrueba || (Inventario != null && Inventario.TieneItem(idTenedor));
            tenedorAdentro = false;

            Debug.Log($"[MinijuegoMicroondas] Iniciado. TienePlato: {tienePlato}, TieneTenedor: {tienetenedor}, ModoPrueba: {modoPrueba}");

            if (barraCalentado == null) Debug.LogWarning("[MinijuegoMicroondas] ¡Falta arrastrar 'Barra Calentado' en el Inspector!");
            if (botonEncender == null) Debug.LogWarning("[MinijuegoMicroondas] ¡Falta arrastrar 'Boton Encender' en el Inspector!");

            if (barraCalentado != null) { barraCalentado.value = 0f; barraCalentado.maxValue = 1f; }

            if (botonEncender != null)
            {
                botonEncender.interactable = tienePlato;
                botonEncender.onClick.RemoveAllListeners();
                botonEncender.onClick.AddListener(EncenderMicroondas);
            }

            if (botonTenedor != null)
            {
                botonTenedor.gameObject.SetActive(tienetenedor);
                botonTenedor.interactable = true;
                botonTenedor.onClick.RemoveAllListeners();
                botonTenedor.onClick.AddListener(MeterTenedor);
            }

            if (botonSalir != null)
            {
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
            }

            if (particulasHumo != null) particulasHumo.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Salir();

            if (calentando && barraCalentado != null)
            {
                barraCalentado.value += velocidadCalentado * Time.deltaTime;
                if (barraCalentado.value >= 1f)
                {
                    barraCalentado.value = 1f;
                    calentando = false;
                    if (particulasHumo != null) particulasHumo.SetActive(false);
                    Completar();
                }
            }
        }

        public void EncenderMicroondas()
        {
            if (calentando) return;
            calentando = true;
            if (botonEncender != null) botonEncender.interactable = false;
            if (tenedorAdentro && particulasHumo != null)
                particulasHumo.SetActive(true);
        }

        public void MeterTenedor()
        {
            tenedorAdentro = true;
            if (botonTenedor != null) botonTenedor.interactable = false;
            if (botonEncender != null && !calentando) botonEncender.interactable = true;
            if (calentando && particulasHumo != null) particulasHumo.SetActive(true);
        }

        private void Completar()
        {
            CompletarTareaNivel();

            int impacto = tenedorAdentro ? 3 : 1;
            int sospecha = tenedorAdentro ? 8 : 0;

            if (EstadoPartida.Instancia != null)
                EstadoPartida.Instancia.RegistrarSabotaje(
                    impacto,
                    sospecha,
                    tenedorAdentro ? "Comida de la oficina arruinada" : null);

            if (Inventario != null)
            {
                if (!Inventario.QuitarItem(idComida))
                    Inventario.QuitarItem(idPlato);

                if (tenedorAdentro)
                {
                    Inventario.QuitarItem(idTenedor);
                }
                else
                {
                    DatosItem caliente = ItemsRegistrados.Instancia != null ? ItemsRegistrados.Instancia.Resolver(idComidaCaliente) : null;
                    if (caliente != null) Inventario.IntentarAgregarItem(caliente);
                }
            }

            Salir();
        }

        protected override void AlSalir()
        {
            calentando = false;
            tenedorAdentro = false;
            if (particulasHumo != null) particulasHumo.SetActive(false);
            StopAllCoroutines();
        }
    }
}
