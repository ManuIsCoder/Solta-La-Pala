using SoltaLaPala.Dialogue;
using SoltaLaPala.Guardado;
using SoltaLaPala.Inventory;
using SoltaLaPala.Personajes;
using SoltaLaPala.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoltaLaPala.Menus
{
    public static class ArranqueMenus
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
            AsegurarServiciosDeEscena();
        }

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            CamaraTerceraPersona.RestablecerUisAbiertas();
            AsegurarServiciosDeEscena();
        }

        private static void AsegurarServiciosDeEscena()
        {
            if (Object.FindAnyObjectByType<GestorMusica>() == null)
            {
                GameObject objeto = new GameObject("GestorMusica");
                objeto.AddComponent<GestorMusica>();
            }

            if (Object.FindAnyObjectByType<GestorMenus>() == null)
            {
                GameObject objeto = new GameObject("GestorMenus");
                objeto.AddComponent<GestorMenus>();
            }

            if (Object.FindAnyObjectByType<EstadoPartida>() == null)
            {
                GameObject objeto = new GameObject("EstadoPartida");
                objeto.AddComponent<EstadoPartida>();
            }

            if (Object.FindAnyObjectByType<GestorGuardado>() == null)
            {
                GameObject objeto = new GameObject("GestorGuardado");
                objeto.AddComponent<GestorGuardado>();
            }

            if (Object.FindAnyObjectByType<RegistroObjetosConsumidos>() == null)
            {
                GameObject objeto = new GameObject("RegistroObjetosConsumidos");
                objeto.AddComponent<RegistroObjetosConsumidos>();
            }

            if (Object.FindAnyObjectByType<HudTiempo>() == null)
            {
                GameObject objeto = new GameObject("HudTiempo");
                objeto.AddComponent<HudTiempo>();
            }

            if (Object.FindAnyObjectByType<BarraSospecha>() == null)
            {
                GameObject objeto = new GameObject("BarraSospecha");
                objeto.AddComponent<BarraSospecha>();
            }

            if (Object.FindAnyObjectByType<IndicadorVisionHUD>() == null)
            {
                GameObject objeto = new GameObject("IndicadorVisionHUD");
                objeto.AddComponent<IndicadorVisionHUD>();
            }

            if (Object.FindAnyObjectByType<IndicadorRango>() == null)
            {
                GameObject objeto = new GameObject("IndicadorRango");
                objeto.AddComponent<IndicadorRango>();
            }

            if (Object.FindAnyObjectByType<MenuEntrega>() == null)
            {
                GameObject objeto = new GameObject("MenuEntrega");
                objeto.AddComponent<MenuEntrega>();
            }

            if (Object.FindAnyObjectByType<SoltaLaPala.Tutorial.PanelTutorialHUD>() == null)
            {
                GameObject objeto = new GameObject("PanelTutorialHUD");
                objeto.AddComponent<SoltaLaPala.Tutorial.PanelTutorialHUD>();
            }

            if (Object.FindAnyObjectByType<SoltaLaPala.Tutorial.PanelTareasHUD>() == null)
            {
                GameObject objeto = new GameObject("PanelTareasHUD");
                objeto.AddComponent<SoltaLaPala.Tutorial.PanelTareasHUD>();
            }

            if (Object.FindAnyObjectByType<InterfazDialogo>() == null)
            {
                GameObject objeto = new GameObject("CanvasDialogo");
                objeto.AddComponent<InterfazDialogo>();
            }

            if (Object.FindAnyObjectByType<GestorDialogos>() == null)
            {
                GameObject objeto = new GameObject("GestorDialogos");
                objeto.AddComponent<GestorDialogos>();
            }

            if (Object.FindAnyObjectByType<SoltaLaPala.Sabotaje.MenuSabotajeTaza>() == null)
            {
                GameObject objeto = new GameObject("MenuSabotajeTaza");
                objeto.AddComponent<SoltaLaPala.Sabotaje.MenuSabotajeTaza>();
            }

            AsegurarModeloJugador();
            AsegurarInventario();
            ConfiguradorNivel1.Configurar();
        }

        private static void AsegurarModeloJugador()
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador == null || jugador.GetComponent<ModeloPersonaje>() != null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>("Personajes/Jugador");
            if (prefab != null)
            {
                jugador.AddComponent<ModeloPersonaje>().Asignar(prefab);
            }
        }

        private static void AsegurarInventario()
        {
            if (Object.FindAnyObjectByType<InventarioJugador>() == null)
            {
                GameObject jugador = GameObject.FindGameObjectWithTag("Player");

                if (jugador == null)
                {
                    Debug.LogWarning("[Menus] No hay ningun objeto con tag 'Player': " +
                                     "el inventario no se puede crear.");
                }
                else
                {
                    jugador.AddComponent<InventarioJugador>();
                }
            }

            InterfazInventario interfaz = Object.FindAnyObjectByType<InterfazInventario>();
            if (interfaz == null)
            {
                GameObject objeto = new GameObject("InterfazInventario");
                interfaz = objeto.AddComponent<InterfazInventario>();
            }

            interfaz.AsignarInventario(InventarioJugador.DelJugador());
        }
    }
}
