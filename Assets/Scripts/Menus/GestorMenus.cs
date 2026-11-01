using System.Collections.Generic;
using SoltaLaPala.Dialogue;
using SoltaLaPala.Guardado;
using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using SoltaLaPala.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoltaLaPala.Menus
{
    [DefaultExecutionOrder(-50)]
    public class GestorMenus : MonoBehaviour
    {
        public static GestorMenus Instancia { get; private set; }

        [Header("Arranque")]
        [Tooltip("Si esta marcado, el juego arranca mostrando el menu principal.")]
        public bool abrirMenuPrincipalAlArrancar = true;

        [Header("Pantallas")]
        [Tooltip("Si se dejan vacias se generan solas al arrancar.")]
        public MenuPrincipal menuPrincipal;
        public MenuSeleccionNiveles menuSeleccionNiveles;
        public MenuPausa menuPausa;
        public MenuConfiguracion menuConfiguracion;
        public MenuTutorial menuTutorial;
        public MenuResultados menuResultados;
        public MenuVictoriaDerrota menuVictoriaDerrota;

        private readonly ControlJugador controlJugador = new ControlJugador();

        private readonly Stack<PantallaMenu> historial = new Stack<PantallaMenu>();

        public PantallaMenu PantallaActual { get; private set; } = PantallaMenu.Ninguna;

        public static event System.Action<PantallaMenu> AlCambiarPantalla;

        public bool HayMenuAbierto => PantallaActual != PantallaMenu.Ninguna;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            AjustesJuego.AplicarTodo();

            ConstructorUI.AsegurarEventSystem();
            AsegurarPantallas();
        }

        private void Start()
        {
            if (ProgresoNiveles.ConsumirInicioTrasCarga())
            {
                CerrarTodo();
                EstadoPartida.Instancia?.ReiniciarNivel();
                EstadoPartida.Instancia?.EmpezarNivel();
                return;
            }

            if (abrirMenuPrincipalAlArrancar)
            {
                Abrir(PantallaMenu.Principal);
            }
            else
            {
                CerrarTodo();
            }
        }

        private void Update()
        {
            if (!ControlesJuego.Pulsada(AccionJuego.Pausa))
            {
                return;
            }

            if (PantallaActual == PantallaMenu.Ninguna)
            {
                if (GestorDialogos.Instancia != null && GestorDialogos.Instancia.DialogoActivo)
                {
                    return;
                }

                SoltaLaPala.Minijuegos.GestorMinijuego gestorMinijuego =
                    SoltaLaPala.Minijuegos.GestorMinijuego.Instancia;
                if (gestorMinijuego != null && gestorMinijuego.MinijuegoActivo)
                {
                    gestorMinijuego.ForzarCierre();
                }

                Abrir(PantallaMenu.Pausa);
            }
            else if (PuedeCerrarseConEscape(PantallaActual))
            {
                Volver();
            }
        }

        public void Abrir(PantallaMenu pantalla)
        {
            if (pantalla == PantallaActual)
            {
                return;
            }

            if (PantallaActual != PantallaMenu.Ninguna)
            {
                historial.Push(PantallaActual);
            }

            Mostrar(pantalla);
        }

        public void Volver()
        {
            if (historial.Count > 0)
            {
                Mostrar(historial.Pop());
            }
            else
            {
                CerrarTodo();
            }
        }

        public void CerrarTodo()
        {
            historial.Clear();
            Mostrar(PantallaMenu.Ninguna);
        }

        public void EmpezarJuego()
        {
            CerrarTodo();

            EstadoPartida.Instancia?.EmpezarNivel();
        }

        public void ReiniciarYJugar()
        {
            string escena = ProgresoNiveles.ObtenerEscena(ProgresoNiveles.NivelActual);
            if (string.IsNullOrEmpty(escena))
            {
                Debug.LogError($"[Menus] No existe una escena para el nivel {ProgresoNiveles.NivelActual}.");
                return;
            }

            if (SceneManager.GetActiveScene().name != escena)
            {
                if (!Application.CanStreamedLevelBeLoaded(escena))
                {
                    Debug.LogError($"[Menus] La escena '{escena}' no está incluida en la configuración de compilación.");
                    return;
                }

                GuardadoPartida.Borrar();
                CerrarTodo();
                InventarioJugador.DelJugador()?.Vaciar();
                ProgresoNiveles.MarcarInicioTrasCarga();
                SceneManager.LoadScene(escena);
                return;
            }

            GuardadoPartida.Borrar();
            EstadoPartida.Instancia?.ReiniciarNivel();
            EmpezarJuego();
        }

        public bool CargarPartidaGuardada()
        {
            if (GestorGuardado.Instancia == null)
            {
                Debug.LogWarning("[Menus] No hay GestorGuardado en la escena, no se puede cargar la partida.");
                return false;
            }

            CerrarTodo();

            if (!GestorGuardado.Instancia.CargarAhora())
            {
                Abrir(PantallaMenu.Principal);
                return false;
            }

            EstadoPartida.Instancia?.EmpezarNivel();

            return true;
        }

        public bool GuardarPartida()
        {
            if (GestorGuardado.Instancia == null)
            {
                Debug.LogWarning("[Menus] No hay GestorGuardado en la escena, no se puede guardar.");
                return false;
            }

            return GestorGuardado.Instancia.GuardarAhora();
        }

        public void SalirDelJuego()
        {
#if UNITY_EDITOR
            Debug.Log("[Menus] Salir del juego (ignorado en el editor).");
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Mostrar(PantallaMenu pantalla)
        {
            PantallaActual = pantalla;

            EstablecerVisible(menuPrincipal, pantalla == PantallaMenu.Principal);
            EstablecerVisible(menuSeleccionNiveles, pantalla == PantallaMenu.SeleccionNiveles);
            EstablecerVisible(menuPausa, pantalla == PantallaMenu.Pausa);
            EstablecerVisible(menuConfiguracion, pantalla == PantallaMenu.Configuracion);
            EstablecerVisible(menuTutorial, pantalla == PantallaMenu.Tutorial);
            EstablecerVisible(menuResultados, pantalla == PantallaMenu.Resultados);

            bool finalVisible = pantalla == PantallaMenu.Victoria || pantalla == PantallaMenu.Derrota;
            EstablecerVisible(menuVictoriaDerrota, finalVisible);

            AplicarEstadoJuego(pantalla != PantallaMenu.Ninguna);

            AlCambiarPantalla?.Invoke(pantalla);
        }

        private static void EstablecerVisible(MenuBase menu, bool visible)
        {
            if (menu != null)
            {
                menu.EstablecerVisible(visible);
            }
        }

        public void RefrescarTiempo()
        {
            RefrescarTiempo(HayMenuAbierto);
        }

        private static void RefrescarTiempo(bool menuAbierto)
        {
            bool hayDialogo = GestorDialogos.Instancia != null && GestorDialogos.Instancia.DialogoActivo;

            Time.timeScale = menuAbierto || hayDialogo ? 0f : 1f;
        }

        private void AplicarEstadoJuego(bool menuAbierto)
        {
            RefrescarTiempo(menuAbierto);

            Cursor.lockState = menuAbierto ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = menuAbierto;

            controlJugador.Bloquear(menuAbierto, avisarUi: false);
        }

        private static bool PuedeCerrarseConEscape(PantallaMenu pantalla)
        {
            return pantalla != PantallaMenu.Victoria
                && pantalla != PantallaMenu.Derrota
                && pantalla != PantallaMenu.Principal;
        }

        private void AsegurarPantallas()
        {
            menuPrincipal = AsegurarPantalla(menuPrincipal, "MenuPrincipal");
            menuSeleccionNiveles = AsegurarPantalla(menuSeleccionNiveles, "MenuSeleccionNiveles");
            menuPausa = AsegurarPantalla(menuPausa, "MenuPausa");
            menuConfiguracion = AsegurarPantalla(menuConfiguracion, "MenuConfiguracion");
            menuTutorial = AsegurarPantalla(menuTutorial, "MenuTutorial");
            menuResultados = AsegurarPantalla(menuResultados, "MenuResultados");
            menuVictoriaDerrota = AsegurarPantalla(menuVictoriaDerrota, "MenuVictoriaDerrota");
        }

        private T AsegurarPantalla<T>(T actual, string nombre) where T : MenuBase
        {
            if (actual != null)
            {
                return actual;
            }

            T enEscena = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (enEscena != null)
            {
                return enEscena;
            }

            GameObject prefab = Resources.Load<GameObject>($"Menus/{nombre}");
            if (prefab != null && prefab.GetComponent<T>() != null)
            {
                GameObject instancia = Instantiate(prefab, transform);
                instancia.name = nombre;
                return instancia.GetComponent<T>();
            }

            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(transform, false);
            return objeto.AddComponent<T>();
        }

        public void MostrarVictoria(string detalle = null)
        {
            menuResultados?.ConfigurarFinal(true, detalle);
            if (menuVictoriaDerrota != null)
            {
                menuVictoriaDerrota.Configurar(true, detalle);
            }

            CerrarTodo();
            Abrir(PantallaMenu.Resultados);
        }

        public void MostrarDerrota(string detalle = null)
        {
            menuResultados?.ConfigurarFinal(false, detalle);
            if (menuVictoriaDerrota != null)
            {
                menuVictoriaDerrota.Configurar(false, detalle);
            }

            CerrarTodo();
            Abrir(PantallaMenu.Resultados);
        }

        public void ContinuarDesdeResultados()
        {
            PantallaMenu pantallaFinal = menuResultados != null && menuResultados.FueVictoria
                ? PantallaMenu.Victoria
                : PantallaMenu.Derrota;

            CerrarTodo();
            Abrir(pantallaFinal);
        }

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                Time.timeScale = 1f;
                Instancia = null;
            }
        }
    }
}
