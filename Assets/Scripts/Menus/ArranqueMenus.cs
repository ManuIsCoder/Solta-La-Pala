using SoltaLaPala.Guardado;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Menus
{
    // Crea el GestorMenus, el EstadoPartida y los objetos del guardado al arrancar
    // el juego, sin tener que acordarse de ponerlos a mano en cada escena.
    //
    // Si ya hay uno puesto en la escena (con ajustes propios en el inspector),
    // este arranque lo respeta y no crea nada.
    public static class ArranqueMenus
    {
        // AfterSceneLoad corre cuando los objetos de la escena ya existen (asi se
        // puede comprobar si el gestor ya estaba puesto a mano) pero antes del
        // primer Start, que es cuando el gestor abre el menu principal.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
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

            // Los dos van juntos y antes de cualquier Start: el menu principal
            // consulta el guardado en cuanto se abre.
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

            AsegurarInventario();
        }

        // El inventario vive en el jugador, no en un objeto suelto: necesita su
        // transform para colgar el ancla de la mano.
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

            // La UI si es independiente: se busca sola el inventario de la escena.
            if (Object.FindAnyObjectByType<InterfazInventario>() == null)
            {
                GameObject objeto = new GameObject("InterfazInventario");
                objeto.AddComponent<InterfazInventario>();
            }
        }
    }
}
