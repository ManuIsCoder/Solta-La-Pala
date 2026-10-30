using UnityEngine;

namespace SoltaLaPala.Guardado
{
    [DisallowMultipleComponent]
    public class IdentificadorObjeto : MonoBehaviour
    {
        [Tooltip("Se rellena solo. Solo editalo a mano si sabes lo que estas haciendo: " +
                 "cambiarlo hace que los guardados viejos no reconozcan este objeto.")]
        [SerializeField] private string id;

        public string Id => id;

        public bool TieneId => !string.IsNullOrEmpty(id);

        public void AsignarId(string nuevoId)
        {
            id = nuevoId;
        }

        public void AsignarIdNuevo()
        {
            id = $"{gameObject.name}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(id))
            {
                return;
            }

            AsignarIdNuevo();
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
