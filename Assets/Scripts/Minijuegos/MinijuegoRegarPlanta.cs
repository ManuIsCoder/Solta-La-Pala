using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SoltaLaPala.Menus;
using SoltaLaPala.Inventory;

namespace SoltaLaPala.Minijuegos
{
    public class MinijuegoRegarPlanta : MinijuegoBase
    {
        [Header("IDs de Ítems")]
        public string idAgua = "BotellaAgua";
        public string idEnergetica = "BebidaEnergetica";

        [Header("Fondo UI")]
        public Image imagenFondo;

        [Header("Sprites de la Planta")]
        public Image imagenPlanta;
        public Sprite spritePlantaNormal;
        public Sprite spritePlantaCrecida;
        public Sprite spritePlantaMarchita;

        [Header("UI - Botones / Arrastrables")]
        public Button botonRegarAgua;
        public Button botonRegarEnergetica;
        public Button botonSalir;
        public Slider barraHidratacion;
        public GameObject particulasAgua;

        [Header("Ajustes")]
        public float velocidadRegado = 0.35f;
        public bool modoPrueba = false;

        private bool regando = false;
        private bool esSabotaje = false;

        protected override void AlIniciar()
        {
            bool tieneAgua = modoPrueba || (Inventario != null && Inventario.TieneItem(idAgua));
            bool tieneEnergetica = modoPrueba || (Inventario != null && Inventario.TieneItem(idEnergetica));
            regando = false;
            esSabotaje = false;

            if (barraHidratacion != null) { barraHidratacion.value = 0f; barraHidratacion.maxValue = 1f; }
            if (particulasAgua != null) particulasAgua.SetActive(false);
            if (imagenPlanta != null && spritePlantaNormal != null) imagenPlanta.sprite = spritePlantaNormal;

            if (botonRegarAgua != null)
            {
                botonRegarAgua.gameObject.SetActive(tieneAgua);
                botonRegarAgua.onClick.RemoveAllListeners();
                botonRegarAgua.onClick.AddListener(() => IniciarRegado(false));
            }

            if (botonRegarEnergetica != null)
            {
                botonRegarEnergetica.gameObject.SetActive(tieneEnergetica);
                botonRegarEnergetica.onClick.RemoveAllListeners();
                botonRegarEnergetica.onClick.AddListener(() => IniciarRegado(true));
            }

            if (botonSalir != null)
            {
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
            }
        }

        public void IniciarRegado(bool conEnergetica)
        {
            if (regando) return;
            regando = true;
            esSabotaje = conEnergetica;
            if (particulasAgua != null) particulasAgua.SetActive(true);
            if (botonRegarAgua != null) botonRegarAgua.interactable = false;
            if (botonRegarEnergetica != null) botonRegarEnergetica.interactable = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Salir();

            if (regando && barraHidratacion != null)
            {
                barraHidratacion.value += velocidadRegado * Time.deltaTime;
                if (barraHidratacion.value >= 1f)
                {
                    barraHidratacion.value = 1f;
                    regando = false;
                    if (particulasAgua != null) particulasAgua.SetActive(false);
                    StartCoroutine(AnimacionFinal());
                }
            }
        }

        private IEnumerator AnimacionFinal()
        {
            // Cambiar el sprite de la planta
            if (imagenPlanta != null)
            {
                imagenPlanta.sprite = esSabotaje ? spritePlantaMarchita : spritePlantaCrecida;
            }

            yield return new WaitForSeconds(1.2f);

            int impacto = esSabotaje ? 3 : 1;
            int sospecha = esSabotaje ? 7 : 0;

            if (EstadoPartida.Instancia != null)
                EstadoPartida.Instancia.RegistrarSabotaje(impacto, sospecha);

            if (esSabotaje)
                Inventario?.QuitarItem(idEnergetica);
            else
                Inventario?.QuitarItem(idAgua);

            Salir();
        }

        protected override void AlSalir()
        {
            regando = false;
            StopAllCoroutines();
        }
    }
}