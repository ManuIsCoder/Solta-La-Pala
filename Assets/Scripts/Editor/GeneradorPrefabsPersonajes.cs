using SoltaLaPala.Dialogue;
using SoltaLaPala.Interaction;
using SoltaLaPala.NPC;
using SoltaLaPala.Personajes;
using UnityEditor;
using UnityEngine;

namespace SoltaLaPala.EditorTools
{
    /// <summary>
    /// Genera los prefabs "listos para escena" de cada NPC dentro de su carpeta
    /// Assets/Personajes/NPCXxx/personajeListo/NPCXxx.prefab.
    ///
    /// Uso: menú de Unity → Solta La Pala → Generar Prefabs de Personajes
    /// </summary>
    public static class GeneradorPrefabsPersonajes
    {
        // Carpeta raíz de los modelos de NPC
        private const string CarpetaPersonajes = "Assets/Personajes";

        // Carpeta en Resources donde están los prefabs de modelo (solo mesh + animador)
        private const string CarpetaRecursos = "Assets/Resources/Personajes";

        // Nombre de la subcarpeta que se crea dentro de cada NPC
        private const string NombreCarpetaListo = "personajeListo";

        // Nombres de todos los NPCs que existen en el proyecto
        private static readonly string[] NombresNPC =
        {
            "NPCHombre1",
            "NPCHombre2",
            "NPCHombre3",
            "NPCHombre4",
            "NPCMujer1",
            "NPCMujer2",
            "NPCMujer3",
            "NPCMujer4",
        };

        [MenuItem("Solta La Pala/Generar Prefabs de Personajes")]
        public static void GenerarTodos()
        {
            int creados = 0;
            int omitidos = 0;

            foreach (string nombreNPC in NombresNPC)
            {
                if (GenerarPrefabNPC(nombreNPC))
                    creados++;
                else
                    omitidos++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Generar Prefabs de Personajes",
                $"Listo.\n\nCreados / actualizados: {creados}\nOmitidos (prefab de modelo no encontrado): {omitidos}",
                "OK");

            Debug.Log($"[GeneradorPrefabsPersonajes] Creados: {creados}, omitidos: {omitidos}.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Internals
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Genera (o actualiza) el prefab listo para escena de un NPC.
        /// Devuelve true si tuvo éxito.
        /// </summary>
        private static bool GenerarPrefabNPC(string nombreNPC)
        {
            // 1. Cargar el prefab de modelo desde Resources/Personajes
            GameObject prefabModelo = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{CarpetaRecursos}/{nombreNPC}.prefab");

            if (prefabModelo == null)
            {
                Debug.LogWarning($"[GeneradorPrefabsPersonajes] No se encontró el prefab de modelo " +
                                 $"'{CarpetaRecursos}/{nombreNPC}.prefab'. Se omite '{nombreNPC}'.");
                return false;
            }

            // 2. Asegurar que existe la carpeta personajeListo
            string carpetaDestino = $"{CarpetaPersonajes}/{nombreNPC}/{NombreCarpetaListo}";
            if (!AssetDatabase.IsValidFolder(carpetaDestino))
            {
                string carpetaPadre = $"{CarpetaPersonajes}/{nombreNPC}";
                AssetDatabase.CreateFolder(carpetaPadre, NombreCarpetaListo);
            }

            // 3. Crear el GameObject raíz en memoria
            GameObject raiz = new GameObject(nombreNPC);

            // ── CapsuleCollider (cuerpo físico del NPC) ──────────────────────────
            CapsuleCollider capsule = raiz.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.3f;
            capsule.center = new Vector3(0f, 1f, 0f);

            // ── ModeloPersonaje (adjunta el prefab de mesh/animaciones) ───────────
            ModeloPersonaje modeloPersonaje = raiz.AddComponent<ModeloPersonaje>();
            modeloPersonaje.prefabModelo = prefabModelo;
            modeloPersonaje.ocultarCapsula = false; // No tiene MeshRenderer propio que ocultar

            // ── ResaltadoInteractuable (requerido por DialogoNPC) ────────────────
            raiz.AddComponent<ResaltadoInteractuable>();

            // ── DialogoNPC ────────────────────────────────────────────────────────
            DialogoNPC dialogo = raiz.AddComponent<DialogoNPC>();
            dialogo.nombre = NombreAmigable(nombreNPC);
            dialogo.id    = nombreNPC;

            // ── DetectorVisionNPC (requiere DialogoNPC → ya está) ─────────────────
            raiz.AddComponent<DetectorVisionNPC>();

            // ── PatrullaNPC (requiere DetectorVisionNPC → ya está) ────────────────
            raiz.AddComponent<PatrullaNPC>();

            // 4. Guardar como prefab
            string rutaPrefab = $"{carpetaDestino}/{nombreNPC}.prefab";
            bool esNuevo = !System.IO.File.Exists(
                System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), rutaPrefab));

            GameObject prefabGuardado = PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);

            // 5. Limpiar el GameObject temporal de la escena
            Object.DestroyImmediate(raiz);

            if (prefabGuardado == null)
            {
                Debug.LogError($"[GeneradorPrefabsPersonajes] No se pudo guardar el prefab en '{rutaPrefab}'.");
                return false;
            }

            Debug.Log($"[GeneradorPrefabsPersonajes] {(esNuevo ? "Creado" : "Actualizado")}: {rutaPrefab}");
            return true;
        }

        /// <summary>
        /// Convierte "NPCHombre1" en "NPC Hombre 1" para usarlo como nombre visible en el diálogo.
        /// </summary>
        private static string NombreAmigable(string nombre)
        {
            // Insertar espacio antes de cada mayúscula que no sea la primera
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < nombre.Length; i++)
            {
                if (i > 0 && char.IsUpper(nombre[i]))
                    sb.Append(' ');
                sb.Append(nombre[i]);
            }
            return sb.ToString();
        }
    }
}
