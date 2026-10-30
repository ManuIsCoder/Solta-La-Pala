using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Marca un objeto que el jugador soltó con Q durante la partida.
    //
    // Hace falta para reiniciar el nivel: el objeto original que se recogio vuelve
    // a aparecer en su sitio (lo reactiva RegistroObjetosConsumidos), asi que el
    // que se solto tiene que desaparecer, o el objeto quedaria duplicado.
    public class ObjetoSoltado : MonoBehaviour
    {
        // Destruye todos los objetos soltados que haya en la escena.
        public static void DestruirTodos()
        {
            foreach (ObjetoSoltado objeto in
                     FindObjectsByType<ObjetoSoltado>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Destroy(objeto.gameObject);
            }
        }
    }
}
