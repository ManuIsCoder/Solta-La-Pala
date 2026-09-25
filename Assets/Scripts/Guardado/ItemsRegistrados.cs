using System.Collections.Generic;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Guardado
{
    // Catalogo de todos los DatosItem del juego. El guardado escribe ids de item,
    // y al cargar hace falta el camino de vuelta: id -> DatosItem.
    //
    // Es un asset unico que se busca por Resources, para que el inventario pueda
    // resolver items sin que nadie tenga que cablearle una referencia.
    [CreateAssetMenu(fileName = "ItemsRegistrados", menuName = "Solta La Pala/Items Registrados")]
    public class ItemsRegistrados : ScriptableObject
    {
        // Nombre del asset dentro de una carpeta Resources. Sin extension.
        private const string RutaResources = "ItemsRegistrados";

        [Tooltip("Todos los items del juego. Usa 'Recolectar items del proyecto' para llenarlo.")]
        public List<DatosItem> items = new List<DatosItem>();

        private static ItemsRegistrados instancia;

        // Cache id -> item, para no recorrer la lista en cada slot restaurado.
        private Dictionary<string, DatosItem> porId;

        // Carga el asset desde Resources la primera vez que alguien lo pide.
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

        // Devuelve el item con ese id, o null si no esta registrado.
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

                // Un id duplicado significa dos assets con el mismo nombre: se avisa
                // y se queda el primero, en vez de reventar con una excepcion.
                if (porId.ContainsKey(item.Id))
                {
                    Debug.LogWarning($"[ItemsRegistrados] Hay dos items con el id '{item.Id}'. " +
                                     "Renombra uno: el guardado solo podra recuperar el primero.", this);
                    continue;
                }

                porId[item.Id] = item;
            }
        }

        // Si se editan los items en el editor, el indice cacheado queda viejo.
        private void OnValidate()
        {
            porId = null;
        }

#if UNITY_EDITOR
        // Busca todos los DatosItem del proyecto y los mete en la lista, para no
        // tener que arrastrarlos uno a uno ni acordarse de añadir los nuevos.
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
