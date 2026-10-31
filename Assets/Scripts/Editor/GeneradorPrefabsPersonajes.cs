using SoltaLaPala.Dialogue;
using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using SoltaLaPala.NPC;
using SoltaLaPala.Personajes;
using SoltaLaPala.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.EditorTools
{
    /// <summary>
    /// Genera los prefabs "listos para escena" de cada NPC y del Jugador.
    ///
    /// NPCs  → Assets/Personajes/NPCXxx/personajeListo/NPCXxx.prefab
    /// Jugador → Assets/Personajes/Jugador/PJ Listo/Jugador.prefab
    ///           Assets/Personajes/Jugador/PJ Listo/Camara.prefab
    ///           Assets/Personajes/Jugador/PJ Listo/CanvasInteraccion.prefab
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

        // Nombre de la subcarpeta del Jugador
        private const string NombreCarpetaPJ = "PJ Listo";

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

            // ── Jugador ──────────────────────────────────────────────────────────
            if (GenerarPrefabsJugador())
                creados += 3; // Jugador + Camara + CanvasInteraccion
            else
                omitidos++;

            // ── NPCs ──────────────────────────────────────────────────────────────
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
        // Jugador
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Genera los tres prefabs del jugador dentro de Assets/Personajes/Jugador/PJ Listo/:
        ///   • Jugador.prefab        — cuerpo con CharacterController y todos los scripts
        ///   • Camara.prefab         — cámara en tercera persona con AudioListener
        ///   • CanvasInteraccion.prefab — canvas con el texto "[E] Interactuar"
        /// Devuelve true si todos se crearon/actualizaron con éxito.
        /// </summary>
        private static bool GenerarPrefabsJugador()
        {
            // Cargar el prefab de modelo del jugador desde Resources/Personajes
            GameObject prefabModelo = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{CarpetaRecursos}/Jugador.prefab");

            if (prefabModelo == null)
            {
                Debug.LogWarning("[GeneradorPrefabsPersonajes] No se encontró " +
                                 $"'{CarpetaRecursos}/Jugador.prefab'. Se omite el Jugador.");
                return false;
            }

            // Asegurar que existe la carpeta PJ Listo
            string carpetaJugador = $"{CarpetaPersonajes}/Jugador";
            string carpetaDest    = $"{carpetaJugador}/{NombreCarpetaPJ}";
            if (!AssetDatabase.IsValidFolder(carpetaDest))
                AssetDatabase.CreateFolder(carpetaJugador, NombreCarpetaPJ);

            // ── 1. CanvasInteraccion ──────────────────────────────────────────────
            GameObject canvasPrefab = CrearCanvasInteraccion(carpetaDest);
            if (canvasPrefab == null) return false;

            // ── 2. Camara ─────────────────────────────────────────────────────────
            GameObject camaraPrefab = CrearCamara(carpetaDest);
            if (camaraPrefab == null) return false;

            // ── 3. Jugador ────────────────────────────────────────────────────────
            GameObject jugadorPrefab = CrearJugador(carpetaDest, prefabModelo, camaraPrefab, canvasPrefab);
            if (jugadorPrefab == null) return false;

            Debug.Log($"[GeneradorPrefabsPersonajes] Prefabs del Jugador creados en '{carpetaDest}'.");
            return true;
        }

        private static GameObject CrearCanvasInteraccion(string carpeta)
        {
            // Raíz Canvas
            GameObject canvasGO = new GameObject("CanvasInteraccion");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Texto de interacción (hijo)
            GameObject textoGO = new GameObject("TextoInteraccion");
            textoGO.SetActive(false);
            textoGO.transform.SetParent(canvasGO.transform, false);

            RectTransform rt = textoGO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -120f);
            rt.sizeDelta = new Vector2(700f, 44f);

            Text texto = textoGO.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = 26;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.color = Color.white;
            texto.supportRichText = true;

            // InterfazTextoInteraccion en el Canvas
            InterfazTextoInteraccion interfaz = canvasGO.AddComponent<InterfazTextoInteraccion>();
            interfaz.raiz = textoGO;
            interfaz.texto = texto;

            string ruta = $"{carpeta}/CanvasInteraccion.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(canvasGO, ruta);
            Object.DestroyImmediate(canvasGO);
            return saved;
        }

        private static GameObject CrearCamara(string carpeta)
        {
            GameObject camaraGO = new GameObject("Camara");
            camaraGO.tag = "MainCamera";

            Camera cam = camaraGO.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;

            camaraGO.AddComponent<AudioListener>();

            CamaraTerceraPersona ctrl = camaraGO.AddComponent<CamaraTerceraPersona>();
            ctrl.offsetObjetivo        = new Vector3(0f, 1.5f, 0f);
            ctrl.sensibilidadX         = 200f;
            ctrl.sensibilidadY         = 150f;
            ctrl.distancia             = 4f;
            ctrl.suavizadoSeguimiento  = 0.05f;
            ctrl.pitchMinimo           = -35f;
            ctrl.pitchMaximo           = 70f;
            ctrl.offsetPrimeraPersona  = new Vector3(0f, 0.6f, 0.45f);
            ctrl.pitchMinimoPrimeraPersona = -75f;
            ctrl.pitchMaximoPrimeraPersona =  75f;

            string ruta = $"{carpeta}/Camara.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(camaraGO, ruta);
            Object.DestroyImmediate(camaraGO);
            return saved;
        }

        private static GameObject CrearJugador(string carpeta,
                                                GameObject prefabModelo,
                                                GameObject camaraPrefab,
                                                GameObject canvasPrefab)
        {
            GameObject jugadorGO = new GameObject("Jugador");
            jugadorGO.tag = "Player";

            // CharacterController
            CharacterController cc = jugadorGO.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.center = Vector3.zero;
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;

            // ModeloPersonaje — instancia el mesh 3D en runtime
            ModeloPersonaje modelo = jugadorGO.AddComponent<ModeloPersonaje>();
            modelo.prefabModelo            = prefabModelo;
            modelo.ocultarCapsula          = true;
            modelo.ocultarEnPrimeraPersona = true;

            // MovimientoJugador
            MovimientoJugador mov = jugadorGO.AddComponent<MovimientoJugador>();
            mov.velocidadCaminar   = 4f;
            mov.velocidadCorrer    = 7f;
            mov.suavizadoRotacion  = 0.1f;
            mov.gravedad           = -20f;

            // InventarioJugador
            InventarioJugador inv = jugadorGO.AddComponent<InventarioJugador>();
            inv.offsetMano        = new Vector3(0.35f, 1.2f, 0.6f);
            inv.amplitudFlotado   = 0.04f;
            inv.velocidadFlotado  = 1.5f;
            inv.offsetEnHueso     = new Vector3(0.08f, 0.02f, 0f);

            // InteractorJugador
            InteractorJugador inter = jugadorGO.AddComponent<InteractorJugador>();
            inter.alcance       = 4f;
            inter.radio         = 0.6f;
            inter.alturaOrigen  = 1.2f;

            string ruta = $"{carpeta}/Jugador.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(jugadorGO, ruta);
            Object.DestroyImmediate(jugadorGO);

            if (saved == null)
            {
                Debug.LogError($"[GeneradorPrefabsPersonajes] No se pudo guardar Jugador.prefab en '{ruta}'.");
                return null;
            }

            // Nota: Las referencias cruzadas (camaraJugador, anclaMano, transformCamara, interfazTexto)
            // se configuran desde el Inspector en la escena una vez que arrastrás los tres prefabs.
            Debug.Log("[GeneradorPrefabsPersonajes] NOTA: Después de poner los prefabs en la escena, " +
                      "asigná la Camara al campo 'camaraJugador' del MovimientoJugador, " +
                      "y el CanvasInteraccion al campo 'interfazTexto' del InteractorJugador.");
            return saved;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // NPCs
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
