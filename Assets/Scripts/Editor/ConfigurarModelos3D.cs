using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SoltaLaPala.Inventory;
using SoltaLaPala.Interaction;

namespace SoltaLaPala.Editor
{
    /// <summary>
    /// Asigna los modelos 3D reales (salero.fbx, crayolas.fbx, Pen A.prefab) a los ítems
    /// del inventario y actualiza los objetos recogibles en la escena para que usen esos
    /// modelos en vez de las primitivas de prototipo.
    ///
    /// Menú: Solta La Pala → Configurar Modelos 3D de Ítems
    /// </summary>
    public static class ConfigurarModelos3D
    {
        private const string RutaSaleroFbx    = "Assets/Modelos/salero.fbx";
        private const string RutaCrayolasFbx  = "Assets/Modelos/crayolas.fbx";
        private const string RutaPenAPrefab   = "Assets/librerias office/Office Pack/Prefabs/Small Objects/Pen A.prefab";

        private const string RutaSaleroAsset  = "Assets/Resources/Items/Salero.asset";
        private const string RutaBiromeAsset  = "Assets/Resources/Items/Birome.asset";
        private const string RutaCrayolasAsset = "Assets/Resources/Items/Crayolas.asset";

        [MenuItem("Solta La Pala/Configurar Modelos 3D de Ítems")]
        public static void Configurar()
        {
            bool ok = true;

            ok &= AsignarPrefabEnMano(RutaSaleroAsset,  RutaSaleroFbx,  "Salero");
            ok &= AsignarPrefabEnMano(RutaCrayolasAsset, RutaCrayolasFbx, "Crayolas");
            ok &= AsignarPrefabEnMano(RutaBiromeAsset,  RutaPenAPrefab,  "Birome (Pen A)");

            ActualizarSaleroEnEscena();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (ok)
                Debug.Log("[ConfigurarModelos3D] ¡Modelos 3D asignados correctamente!");
            else
                Debug.LogWarning("[ConfigurarModelos3D] Algunos modelos no se encontraron. Revisá la consola.");
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static bool AsignarPrefabEnMano(string rutaAsset, string rutaModelo, string nombreLog)
        {
            DatosItem item = AssetDatabase.LoadAssetAtPath<DatosItem>(rutaAsset);
            if (item == null)
            {
                Debug.LogWarning($"[ConfigurarModelos3D] No se encontró el asset: {rutaAsset}");
                return false;
            }

            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
            if (modelo == null)
            {
                Debug.LogWarning($"[ConfigurarModelos3D] No se encontró el modelo: {rutaModelo}");
                return false;
            }

            item.prefabEnMano = modelo;
            EditorUtility.SetDirty(item);
            Debug.Log($"[ConfigurarModelos3D] {nombreLog}: prefabEnMano → {rutaModelo}");
            return true;
        }

        /// <summary>
        /// Busca el objeto "Salero" (ObjetoRecogible) en la escena activa y lo ajusta:
        ///   - Cambia la malla a Cube primitivo para que ObjetoRecogible la reemplace en runtime.
        ///   - Normaliza la escala a (1, 1, 1).
        ///   - Ajusta el colisionador a BoxCollider proporcional al salero.fbx con globalScale=0.02.
        /// </summary>
        private static void ActualizarSaleroEnEscena()
        {
            ObjetoRecogible[] recogibles = Object.FindObjectsByType<ObjetoRecogible>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (ObjetoRecogible rec in recogibles)
            {
                DatosItem datosItem = rec.item;
                if (datosItem == null || datosItem.id != "Salero") continue;

                GameObject go = rec.gameObject;

                // Escala 1,1,1 — el modelo importado ya tiene el tamaño correcto.
                go.transform.localScale = Vector3.one;

                // Cambiar malla a Cube primitivo: ObjetoRecogible la reemplazará en Awake.
                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null)
                {
                    // La malla "Cube" es la primitiva interna de Unity (guid 0000…e000…).
                    // La obtenemos instanciando un cubo temporal y copiando la referencia.
                    GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    mf.sharedMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
                    Object.DestroyImmediate(tmp);
                }

                // Colisionador: reemplazar CapsuleCollider por BoxCollider si hace falta.
                CapsuleCollider caps = go.GetComponent<CapsuleCollider>();
                if (caps != null)
                {
                    Object.DestroyImmediate(caps);
                    BoxCollider box = go.AddComponent<BoxCollider>();
                    // salero.fbx con globalScale=0.02: ~4.4cm × 4.4cm × 9.2cm
                    box.size   = new Vector3(0.05f, 0.10f, 0.05f);
                    box.center = new Vector3(0f, 0.05f, 0f);
                }

                EditorUtility.SetDirty(go);
                Debug.Log($"[ConfigurarModelos3D] Salero en escena actualizado: {go.scene.name}");
            }

            // Marcar escenas modificadas para que Unity pida guardar.
            for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; i++)
            {
                var s = EditorSceneManager.GetSceneAt(i);
                if (s.isDirty) EditorSceneManager.MarkSceneDirty(s);
            }
        }
    }
}
