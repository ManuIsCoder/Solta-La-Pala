using SoltaLaPala.Guardado;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using SoltaLaPala.Inventory;
using SoltaLaPala.Menus;

namespace SoltaLaPala.Minijuegos
{
    public class MinijuegoCafetera : MinijuegoBase
    {
        [Header("IDs de ítems necesarios")]
        public string idTaza = "TazaCafe";
        public string idSalero = "Salero";

        [Header("Fondo UI")]
        public Image imagenFondo;

        [Header("UI - Botones")]
        public Button botonCafe;
        public Button botonSal;
        public Button botonSalir;

        [Header("UI - Barra de llenado")]
        public Slider barraLlenado;

        [Header("UI - Partículas / sprites de caida")]
        public GameObject particulasCafe;
        public GameObject particulasSal;

        [Header("Ajustes")]
        public float velocidadDeLlenado = 0.2f;
        public float tiempoParticulas = 1.5f;
        [Tooltip("Activa esto para probar el minijuego sin requerir tener la taza o salero en el inventario")]
        public bool modoPrueba = false;

        private bool cayendoCafe;
        private bool tieneSal;
        private bool sabotajeSal;

        protected override void AlIniciar()
        {
            bool tieneTaza = modoPrueba || (Inventario != null && Inventario.TieneItem(idTaza));
            tieneSal = modoPrueba || (Inventario != null && Inventario.TieneItem(idSalero));
            sabotajeSal = false;

            Debug.Log($"[MinijuegoCafetera] Iniciado. TieneTaza: {tieneTaza}, TieneSal: {tieneSal}, ModoPrueba: {modoPrueba}");

            if (barraLlenado == null) Debug.LogWarning("[MinijuegoCafetera] ¡Falta arrastrar 'Barra Llenado' en el Inspector!");
            if (botonCafe == null) Debug.LogWarning("[MinijuegoCafetera] ¡Falta arrastrar 'Boton Cafe' en el Inspector!");
            if (particulasCafe == null) Debug.LogWarning("[MinijuegoCafetera] ¡Falta arrastrar 'Particulas Cafe' en el Inspector!");

            if (barraLlenado != null)
            {
                barraLlenado.value = 0f;
                barraLlenado.maxValue = 1f;
            }

            if (botonCafe != null)
            {
                botonCafe.interactable = tieneTaza;
                botonCafe.onClick.RemoveAllListeners();
                botonCafe.onClick.AddListener(PresionarCafetera);
            }

            if (botonSal != null)
            {
                botonSal.gameObject.SetActive(tieneSal);
                botonSal.interactable = true;
                botonSal.onClick.RemoveAllListeners();
                botonSal.onClick.AddListener(AgregarSal);
            }

            if (botonSalir != null)
            {
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
            }

            if (particulasCafe != null) particulasCafe.SetActive(false);
            if (particulasSal != null) particulasSal.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Salir();

            if (cayendoCafe && barraLlenado != null)
            {
                barraLlenado.value += velocidadDeLlenado * Time.deltaTime;
                if(barraLlenado.value >= 1f)
                {
                    barraLlenado.value = 1f;
                    cayendoCafe = false;
                    if(particulasCafe != null) particulasCafe.SetActive(false);
                    Completar();
                }
            }
        }

        public void PresionarCafetera()
        {
            Debug.Log("[MinijuegoCafetera] PresionarCafetera presionado. cayendoCafe actual: " + cayendoCafe);
            if (cayendoCafe) return;
            cayendoCafe = true;
            if (botonCafe != null) botonCafe.interactable = false;
            if (particulasCafe != null)
            {
                particulasCafe.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[MinijuegoCafetera] 'particulasCafe' no está asignado en el Inspector!");
            }
        }

        public void AgregarSal()
        {
            sabotajeSal = true;
            if (botonSal != null) botonSal.interactable = false;
            StartCoroutine(MostrarParticulasBreve(particulasSal));
        }

        private IEnumerator MostrarParticulasBreve(GameObject particulas)
        {
            if (particulas != null) particulas.SetActive(true);
            yield return new WaitForSeconds(tiempoParticulas);
            if (particulas != null) particulas.SetActive(false);
        }

        private void Completar()
        {
            CompletarTareaNivel();

            int impacto = sabotajeSal ? 2 : 1;
            int sospecha = sabotajeSal ? 5 : 0;

            if (EstadoPartida.Instancia != null)
                EstadoPartida.Instancia.RegistrarSabotaje(
                    impacto,
                    sospecha,
                    sabotajeSal ? "Cafe de la oficina arruinado" : null);

            Inventario?.QuitarItem(idTaza);
            if (sabotajeSal) Inventario?.QuitarItem(idSalero);

            Salir();
        }

        protected override void AlSalir()
        {
            cayendoCafe = false;
            sabotajeSal = false;
            if (particulasCafe != null) particulasCafe.SetActive(false);
            if (particulasSal != null) particulasSal.SetActive(false);
            StopAllCoroutines();
        }
    }
}
