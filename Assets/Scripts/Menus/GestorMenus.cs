using System.Collections.Generic;
using SoltaLaPala.Dialogue;
using SoltaLaPala.Guardado;
using SoltaLaPala.Interaction;
using SoltaLaPala.Player;
using UnityEngine;

namespace SoltaLaPala.Menus
{
    // Coordina todas las pantallas de menu: cual esta abierta, la pila de "volver",
    // la pausa del tiempo y el bloqueo del jugador.
    //
    // Es el unico que toca Time.timeScale y el unico que decide quien tiene el control,
    // para que dos menus no se peleen por el cursor.
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

        // Congela al jugador mientras hay un menu abierto.
        private readonly ControlJugador controlJugador = new ControlJugador();

        // Pila de pantallas por las que se paso, para que "Volver" sepa a donde ir.
        private readonly Stack<PantallaMenu> historial = new Stack<PantallaMenu>();

        // Pantalla que se esta mostrando ahora mismo.
        public PantallaMenu PantallaActual { get; private set; } = PantallaMenu.Ninguna;

        // True si hay cualquier menu abierto. Lo consultan los scripts de gameplay
        // para no leer input mientras el jugador esta navegando menus.
        public bool HayMenuAbierto => PantallaActual != PantallaMenu.Ninguna;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            // Los ajustes guardados (volumen, pantalla completa) se aplican antes
            // de que se vea el primer frame.
            AjustesJuego.AplicarTodo();

            ConstructorUI.AsegurarEventSystem();
            AsegurarPantallas();
        }

        private void Start()
        {
            if (abrirMenuPrincipalAlArrancar)
            {
                Abrir(PantallaMenu.Principal);
            }
            else
            {
                CerrarTodo();
            }
        }

        // Lee la tecla de pausa. Solo pausa si no hay ya un menu abierto
        // y si no hay un dialogo en marcha (el dialogo ya bloquea al jugador).
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

                Abrir(PantallaMenu.Pausa);
            }
            else if (PuedeCerrarseConEscape(PantallaActual))
            {
                Volver();
            }
        }

        // Abre una pantalla, guardando la actual en el historial para poder volver.
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

        // Vuelve a la pantalla anterior. Si no hay ninguna, devuelve el control al juego.
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

        // Cierra todos los menus, reanuda el tiempo y devuelve el control al jugador.
        public void CerrarTodo()
        {
            historial.Clear();
            Mostrar(PantallaMenu.Ninguna);
        }

        // Empieza la partida: cierra los menus y deja jugar.
        public void EmpezarJuego()
        {
            CerrarTodo();

            // Arranca el reloj del nivel. Va despues de CerrarTodo para que el
            // tiempo no empiece a correr con un menu todavia en pantalla.
            EstadoPartida.Instancia?.EmpezarNivel();
        }

        // Empieza el nivel actual desde cero y devuelve el control al juego.
        //
        // Lo usan el selector de niveles, la pausa y las dos opciones de la
        // pantalla de final. Antes cada uno encadenaba estos tres pasos por su
        // cuenta y alguno se dejaba el borrado del guardado, que dejaba
        // "Continuar" apuntando a una partida ya descartada.
        public void ReiniciarYJugar()
        {
            EstadoPartida.Instancia?.ReiniciarNivel();
            GuardadoPartida.Borrar();

            EmpezarJuego();
        }

        // Retoma la partida guardada. Devuelve false si no habia nada que cargar,
        // para que quien lo llame pueda quedarse en el menu.
        public bool CargarPartidaGuardada()
        {
            if (GestorGuardado.Instancia == null)
            {
                Debug.LogWarning("[Menus] No hay GestorGuardado en la escena, no se puede cargar la partida.");
                return false;
            }

            // Cerrar primero y restaurar despues, no al reves: con un menu abierto
            // AplicarEstadoJuego deja Time.timeScale en 0, y teletransportar al
            // jugador con el tiempo congelado deja al CharacterController y a la
            // camara en un estado a medias.
            CerrarTodo();

            if (!GestorGuardado.Instancia.CargarAhora())
            {
                // No habia guardado utilizable (borrado o corrupto): se vuelve al
                // menu principal en vez de dejar al jugador en una escena sin estado.
                Abrir(PantallaMenu.Principal);
                return false;
            }

            // El reloj arranca con el tiempo que Restaurar acaba de dejar puesto.
            EstadoPartida.Instancia?.EmpezarNivel();

            return true;
        }

        // Guarda la partida en curso. Lo usa el menu de pausa.
        public bool GuardarPartida()
        {
            if (GestorGuardado.Instancia == null)
            {
                Debug.LogWarning("[Menus] No hay GestorGuardado en la escena, no se puede guardar.");
                return false;
            }

            return GestorGuardado.Instancia.GuardarAhora();
        }

        // Cierra el juego. En el editor solo lo marca en consola, porque
        // Application.Quit no hace nada mientras estas en Play Mode.
        public void SalirDelJuego()
        {
#if UNITY_EDITOR
            Debug.Log("[Menus] Salir del juego (ignorado en el editor).");
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // Muestra la pantalla pedida y esconde las demas.
        // Es el unico sitio donde se enciende/apaga cada menu.
        private void Mostrar(PantallaMenu pantalla)
        {
            PantallaActual = pantalla;

            EstablecerVisible(menuPrincipal, pantalla == PantallaMenu.Principal);
            EstablecerVisible(menuSeleccionNiveles, pantalla == PantallaMenu.SeleccionNiveles);
            EstablecerVisible(menuPausa, pantalla == PantallaMenu.Pausa);
            EstablecerVisible(menuConfiguracion, pantalla == PantallaMenu.Configuracion);
            EstablecerVisible(menuTutorial, pantalla == PantallaMenu.Tutorial);
            EstablecerVisible(menuResultados, pantalla == PantallaMenu.Resultados);

            // Victoria y derrota comparten pantalla: la diferencia es el texto,
            // que ya configuro quien la abrio via MostrarVictoria/MostrarDerrota.
            bool finalVisible = pantalla == PantallaMenu.Victoria || pantalla == PantallaMenu.Derrota;
            EstablecerVisible(menuVictoriaDerrota, finalVisible);

            AplicarEstadoJuego(pantalla != PantallaMenu.Ninguna);
        }

        private static void EstablecerVisible(MenuBase menu, bool visible)
        {
            if (menu != null)
            {
                menu.EstablecerVisible(visible);
            }
        }

        // Congela el juego y libera el cursor mientras hay un menu abierto.
        private void AplicarEstadoJuego(bool menuAbierto)
        {
            // timeScale 0 congela fisica y animaciones. Los menus usan
            // WaitForSecondsRealtime / unscaledDeltaTime si necesitan tiempo.
            Time.timeScale = menuAbierto ? 0f : 1f;

            Cursor.lockState = menuAbierto ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = menuAbierto;

            // avisarUi en false: la camara ya consulta a este gestor directamente
            // para saber si hay menu abierto, y contarlo tambien en su contador
            // dejaria la vista trabada al cerrar.
            controlJugador.Bloquear(menuAbierto, avisarUi: false);
        }

        // Escape cierra los menus de navegacion, pero no las pantallas de final
        // de partida: de victoria/derrota se sale eligiendo una opcion.
        private static bool PuedeCerrarseConEscape(PantallaMenu pantalla)
        {
            return pantalla != PantallaMenu.Victoria
                && pantalla != PantallaMenu.Derrota
                && pantalla != PantallaMenu.Principal;
        }

        // Crea las pantallas que no se hayan asignado a mano en el inspector.
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

        // Si la pantalla no esta asignada, busca una en la escena y si tampoco hay,
        // la crea como hijo de este objeto.
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

            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(transform, false);
            return objeto.AddComponent<T>();
        }

        // Atajos para que el gameplay abra las pantallas de final de partida.
        public void MostrarVictoria(string detalle = null)
        {
            if (menuVictoriaDerrota != null)
            {
                menuVictoriaDerrota.Configurar(true, detalle);
            }

            Abrir(PantallaMenu.Victoria);
        }

        public void MostrarDerrota(string detalle = null)
        {
            if (menuVictoriaDerrota != null)
            {
                menuVictoriaDerrota.Configurar(false, detalle);
            }

            Abrir(PantallaMenu.Derrota);
        }

        // Al destruirse hay que descongelar el tiempo: timeScale es global y
        // sobrevive al cambio de escena, dejando el juego congelado sin nadie que lo arregle.
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
