using UnityEngine;

namespace SoltaLaPala.Guardado
{
    // Identidad estable de un objeto de la escena, para que el guardado pueda
    // reconocerlo al cargar.
    //
    // Hace falta porque al recoger un objeto o sabotear una taza el objeto se
    // desactiva: sin un id no hay forma de saber CUAL de las tazas de la escena
    // ya se uso. El nombre del GameObject no sirve (se repite y se renombra),
    // ni el InstanceID (cambia en cada arranque).
    //
    // El id se genera solo la primera vez que el componente existe en el editor
    // y queda serializado en la escena, asi que sobrevive a recargas y builds.
    [DisallowMultipleComponent]
    public class IdentificadorObjeto : MonoBehaviour
    {
        [Tooltip("Se rellena solo. Solo editalo a mano si sabes lo que estas haciendo: " +
                 "cambiarlo hace que los guardados viejos no reconozcan este objeto.")]
        [SerializeField] private string id;

        public string Id => id;

        // True si el id esta listo para usarse. Un prefab recien arrastrado a la
        // escena en tiempo de ejecucion no pasa por OnValidate y se queda sin id.
        public bool TieneId => !string.IsNullOrEmpty(id);

#if UNITY_EDITOR
        // Corre al añadir el componente, al duplicar el objeto y al tocar el
        // inspector. Solo asigna si esta vacio, para no romper guardados existentes.
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(id))
            {
                return;
            }

            AsignarIdNuevo();
        }

        // Fuerza un id nuevo. Lo usa OnValidate y esta disponible para el editor
        // por si hay que arreglar un duplicado a mano.
        public void AsignarIdNuevo()
        {
            // El nombre delante hace los ids legibles al mirar el JSON o la escena;
            // el GUID corto garantiza que dos objetos con el mismo nombre no choquen.
            id = $"{gameObject.name}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";

            // Sin esto Unity no marca la escena como sucia y el id no se guarda.
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private void Awake()
        {
            if (!TieneId)
            {
                Debug.LogWarning($"[IdentificadorObjeto] '{gameObject.name}' no tiene id: " +
                                 "el guardado no podra recordar su estado.", this);
            }
        }
    }
}
