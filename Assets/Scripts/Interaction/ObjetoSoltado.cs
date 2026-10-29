using UnityEngine;

namespace SoltaLaPala.Interaction
{
    public class ObjetoSoltado : MonoBehaviour
    {
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
