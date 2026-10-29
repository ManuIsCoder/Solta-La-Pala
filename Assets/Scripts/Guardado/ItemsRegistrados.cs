using System.Collections.Generic;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Guardado
{
    [CreateAssetMenu(fileName = "ItemsRegistrados", menuName = "Solta La Pala/Items Registrados")]
    public class ItemsRegistrados : ScriptableObject
    {
        private const string RutaResources = "ItemsRegistrados";

        [Tooltip("Todos los items del juego. Usa 'Recolectar items del proyecto' para llenarlo.")]
        public List<DatosItem> items = new List<DatosItem>();

        private static ItemsRegistrados instancia;

        private Dictionary<string, DatosItem> porId;

        public static ItemsRegistrados Instancia
        {
            get
            {
                if (instancia == null)
                {
                    instancia = Resources.Load<ItemsRegistrados>(RutaResources);

                    if (instancia == null)
                    {
                        Debug.LogWarning($"[ItemsRegistrados] No se encontro '{RutaResources}' en una carpeta " +
                                         "Resources. El inventario no se podra restaurar al cargar partida.");
                    }
                }

                return instancia;
            }
        }

        public DatosItem Resolver(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            ConstruirIndiceSiHaceFalta();

            return porId.TryGetValue(id, out DatosItem item) ? item : null;
        }

        private void ConstruirIndiceSiHaceFalta()
        {
            if (porId != null)
            {
                return;
            }

            porId = new Dictionary<string, DatosItem>();

            foreach (DatosItem item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                {
                    continue;
                }

                if (porId.ContainsKey(item.Id))
                {
                    Debug.LogWarning($"[ItemsRegistrados] Hay dos items con el id '{item.Id}'. " +
                                     "Renombra uno: el guardado solo podra recuperar el primero.", this);
                    continue;
                }

                porId[item.Id] = item;
            }
        }

        private void OnValidate()
        {
            porId = null;
        }

#if UNITY_EDITOR
        [ContextMenu("Recolectar items del proyecto")]
        private void RecolectarItems()
        {
            items.Clear();

            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:DatosItem"))
            {
                string ruta = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                DatosItem item = UnityEditor.AssetDatabase.LoadAssetAtPath<DatosItem>(ruta);

                if (item != null)
                {
                    items.Add(item);
                }
            }

            porId = null;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ItemsRegistrados] Recolectados {items.Count} items.");
        }
#endif
    }
}
