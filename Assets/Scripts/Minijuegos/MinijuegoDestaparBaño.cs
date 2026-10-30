using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SoltaLaPala.Menus;
using SoltaLaPala.Inventory;
using SoltaLaPala.Guardado;

namespace SoltaLaPala.Minijuegos
{
    public class MinijuegoDestaparBaño : MinijuegoBase
    {
        [Header("IDs de Ítems")]
        public string idDestapador = "Destapador";
        public string idSoreteResultante = "Sorete";

        [Header("Fondo UI")]
        public Image imagenFondo;

        [Header("UI - Elementos")]
        public Image imagenInodoro;
        public Image imagenSoreteFlotante;
        public Button botonBombear;
        public Button botonSalir;
        public Slider barraDestape;
        public GameObject efectoAgua;

        [Header("Ajustes")]
        public float progresoPorBombeo = 0.2f;
        public bool modoPrueba = false;

        private bool completado = false;

        protected override void AlIniciar()
        {
            bool tieneDestapador = modoPrueba || (Inventario != null && Inventario.TieneItem(idDestapador));
            completado = false;

            if (barraDestape != null) { barraDestape.value = 0f; barraDestape.maxValue = 1f; }
            if (imagenSoreteFlotante != null) imagenSoreteFlotante.gameObject.SetActive(false);
            if (efectoAgua != null) efectoAgua.SetActive(false);

            if (botonBombear != null)
            {
                botonBombear.interactable = tieneDestapador;
                botonBombear.onClick.RemoveAllListeners();
                botonBombear.onClick.AddListener(Bombear);
            }

            if (botonSalir != null)
            {
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
            }
        }

        public void Bombear()
        {
            if (completado) return;

            if (barraDestape != null)
            {
                barraDestape.value += progresoPorBombeo;
                StartCoroutine(EfectoSalpicar());

                if (barraDestape.value >= 1f)
                {
                    barraDestape.value = 1f;
                    completado = true;
                    StartCoroutine(FinalDestapar());
                }
            }
        }

        private IEnumerator EfectoSalpicar()
        {
            if (efectoAgua != null) efectoAgua.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            if (efectoAgua != null) efectoAgua.SetActive(false);
        }

        private IEnumerator FinalDestapar()
        {
            if (botonBombear != null) botonBombear.interactable = false;

            // Aparece el sorete flotando
            if (imagenSoreteFlotante != null)
                imagenSoreteFlotante.gameObject.SetActive(true);

            yield return new WaitForSeconds(1.5f);

            // Suma impacto de tarea normal (el baño no se arruina)
            if (EstadoPartida.Instancia != null)
                EstadoPartida.Instancia.RegistrarSabotaje(1, 0);

            // Entregar el objeto resultante (Sorete) al inventario
            DatosItem sorete = ItemsRegistrados.Instancia != null ? ItemsRegistrados.Instancia.Resolver(idSoreteResultante) : null;
            if (sorete != null && Inventario != null)
            {
                Inventario.IntentarAgregarItem(sorete);
            }

            Salir();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Salir();
        }
    }
}