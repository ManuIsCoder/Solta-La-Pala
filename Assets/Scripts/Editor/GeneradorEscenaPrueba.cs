using SoltaLaPala.Dialogue;
using SoltaLaPala.Guardado;
using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using SoltaLaPala.Menus;
using SoltaLaPala.NPC;
using SoltaLaPala.Player;
using SoltaLaPala.Sabotaje;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.EditorTools
{
    // Arma una escena de prueba con todos los sistemas enchufados entre si:
    // jugador, camara, inventario, interaccion, dialogos, sabotaje, NPCs con y
    // sin ruta, y el HUD (que se crea solo desde ArranqueMenus al dar Play).
    //
    // Se genera por codigo y no a mano para poder rehacerla cuando algo cambie,
    // y para que quede versionada como texto legible en vez de un .unity opaco.
    //
    // El escenario son cubos: lo justo para probar oclusion de vision, colision
    // de camara y recorridos. No depende de ningun asset externo.
    public static class GeneradorEscenaPrueba
    {
        private const string RutaEscena = "Assets/Scenes/EscenaPrueba.unity";
        private const string TagJugador = "Player";

        // Los items de prueba viven en Resources para que ItemsRegistrados los
        // encuentre al recolectar, igual que los definitivos.
        private const string CarpetaItems = "Assets/Resources/Items";
        private const string CarpetaIconos = "Assets/Sprites/Items";
        private const string CarpetaPrefabs = "Assets/Prefabs/Items";

        // Items que usa la escena. El nombre es tambien el id del guardado.
        private const string ItemPala = "Pala";
        private const string ItemPalita = "Palita";
        private const string ItemDetergente = "Detergente";

        // --- Calibracion de la escena de prueba ---
        //
        // Los valores estan pensados para ver las dos pantallas de final rapido,
        // no para que el nivel este equilibrado:
        //   - 60s de reloj, para que la derrota por tiempo llegue enseguida.
        //   - impacto objetivo bajo y varias tazas, para poder ganar en un par
        //     de sabotajes.
        //   - sospecha maxima baja, para que el tope se alcance sin farmear.
        private const float DuracionNivelPrueba = 60f;
        private const int ImpactoObjetivoPrueba = 6;
        private const int SospechaMaximaPrueba = 40;

        [MenuItem("Solta La Pala/Crear escena de prueba")]
        public static void Crear()
        {
            if (!ConfirmarSobrescritura())
            {
                return;
            }

            Construir();
        }

        private static bool ConfirmarSobrescritura()
        {
            if (!System.IO.File.Exists(RutaEscena))
            {
                return true;
            }

            return EditorUtility.DisplayDialog(
                "Escena de prueba",
                $"Ya existe {RutaEscena}.\n\nSe va a sobrescribir con una nueva. " +
                "Los cambios que le hayas hecho a mano se pierden.",
                "Sobrescribir", "Cancelar");
        }

        private static void Construir()
        {
            // Los items se crean antes que la escena: los objetos del mundo y las
            // misiones necesitan referenciarlos.
            AsegurarItems();

            var escena = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CrearIluminacion();
            CrearEventSystem();
            CrearEstadoPartida();
            CrearEscenario();

            GameObject jugador = CrearJugador();
            CrearCamara(jugador.transform);
            CrearUiInteraccion();
            CrearUiDialogo();
            CrearMenuSabotaje();

            CrearObjetosInteractuables();
            CrearNpcs();

            EditorSceneManager.MarkSceneDirty(escena);

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.Refresh();

            AnadirABuildSettings();

            Debug.Log($"[GeneradorEscenaPrueba] Escena creada en {RutaEscena}. " +
                      "Dale Play: el HUD, los menus y el inventario se crean solos.");
        }

        // --- Items -----------------------------------------------------------

        // Crea los DatosItem de prueba si no existen y los mete en el registro.
        // Los que ya existan se respetan: puede que les hayas puesto icono o
        // prefab a mano.
        private static void AsegurarItems()
        {
            System.IO.Directory.CreateDirectory(CarpetaItems);
            AssetDatabase.Refresh();

            CrearItem(ItemPala, "Pala", "Una pala de obra, pesada y seria.",
                ColorDe(ItemPala), new Vector3(0.18f, 0.7f, 0.18f));
            CrearItem(ItemPalita, "Palita",
                "Una palita de playa, de plastico. No es lo que nadie pidio.",
                ColorDe(ItemPalita), new Vector3(0.12f, 0.3f, 0.12f));
            CrearItem(ItemDetergente, "Detergente",
                "Lavavajillas concentrado. Sabe fatal y se nota.",
                ColorDe(ItemDetergente), new Vector3(0.2f, 0.5f, 0.2f));

            PoblarRegistroDeItems();
        }

        // Color de cada item. Lo comparten su icono, su prefab en la mano y el
        // objeto que se recoge del suelo, para que se reconozca de un vistazo.
        private static Color ColorDe(string nombreItem)
        {
            switch (nombreItem)
            {
                case ItemPala: return new Color(0.55f, 0.40f, 0.25f);
                case ItemPalita: return new Color(0.95f, 0.55f, 0.15f);
                case ItemDetergente: return new Color(0.25f, 0.75f, 0.85f);
                default: return Color.gray;
            }
        }

        private static DatosItem CrearItem(string nombreAsset, string nombreVisible,
            string descripcion, Color color, Vector3 tamano)
        {
            string ruta = $"{CarpetaItems}/{nombreAsset}.asset";

            // El icono y el prefab se rehacen aunque el item ya exista: si falta
            // alguno el inventario o la mano se quedan vacios, que es justo lo que
            // este generador tiene que dejar resuelto.
            Sprite icono = CrearIcono(nombreAsset, color);
            GameObject prefab = CrearPrefabDeMano(nombreAsset, color, tamano);

            DatosItem existente = AssetDatabase.LoadAssetAtPath<DatosItem>(ruta);

            if (existente != null)
            {
                if (existente.icono == null) existente.icono = icono;
                if (existente.prefabEnMano == null) existente.prefabEnMano = prefab;

                EditorUtility.SetDirty(existente);
                AssetDatabase.SaveAssets();
                return existente;
            }

            DatosItem item = ScriptableObject.CreateInstance<DatosItem>();
            item.nombre = nombreVisible;
            item.descripcion = descripcion;
            item.icono = icono;
            item.prefabEnMano = prefab;

            AssetDatabase.CreateAsset(item, ruta);

            // El id se rellena en OnValidate, que no corre al crear por codigo.
            // Sin id el guardado no puede restaurar el item al cargar partida.
            ForzarIdDelItem(item, nombreAsset);

            return item;
        }

        // Material guardado en disco, para los prefabs.
        private static Material CrearMaterialAsset(string nombreItem, Color color)
        {
            System.IO.Directory.CreateDirectory(CarpetaPrefabs);

            string ruta = $"{CarpetaPrefabs}/Mat{nombreItem}.mat";

            Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);

            if (existente != null)
            {
                return existente;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");

            Material material = new Material(shader) { color = color };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            AssetDatabase.CreateAsset(material, ruta);

            return material;
        }

        // Genera un PNG cuadrado de color plano y lo importa como Sprite.
        //
        // Es un marcador de posicion: cuando haya arte se sustituyen los PNG y los
        // DatosItem los siguen apuntando sin tocar nada.
        private static Sprite CrearIcono(string nombreItem, Color color)
        {
            System.IO.Directory.CreateDirectory(CarpetaIconos);

            string ruta = $"{CarpetaIconos}/Icono{nombreItem}.png";

            if (!System.IO.File.Exists(ruta))
            {
                const int lado = 64;
                Texture2D textura = new Texture2D(lado, lado);

                for (int y = 0; y < lado; y++)
                {
                    for (int x = 0; x < lado; x++)
                    {
                        // Un borde mas oscuro para que el icono se despegue del
                        // fondo del slot, que tambien es oscuro.
                        bool borde = x < 4 || y < 4 || x >= lado - 4 || y >= lado - 4;
                        textura.SetPixel(x, y, borde ? color * 0.55f : color);
                    }
                }

                textura.Apply();
                System.IO.File.WriteAllBytes(ruta, textura.EncodeToPNG());
                Object.DestroyImmediate(textura);

                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            }

            ConfigurarComoSprite(ruta);

            return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }

        // Un PNG recien escrito entra como Texture2D: sin esto no se puede
        // asignar al campo icono, que espera un Sprite.
        private static void ConfigurarComoSprite(string ruta)
        {
            TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;

            if (importador == null || importador.textureType == TextureImporterType.Sprite)
            {
                return;
            }

            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.alphaIsTransparency = true;
            importador.SaveAndReimport();
        }

        // Prefab que se instancia en la mano del jugador: un cubo del color y el
        // tamano del item, sin collider (lo sujeta, no choca con el).
        private static GameObject CrearPrefabDeMano(string nombreItem, Color color, Vector3 tamano)
        {
            System.IO.Directory.CreateDirectory(CarpetaPrefabs);

            string ruta = $"{CarpetaPrefabs}/{nombreItem}EnMano.prefab";

            GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);

            if (existente != null)
            {
                return existente;
            }

            GameObject temporal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temporal.name = $"{nombreItem}EnMano";
            temporal.transform.localScale = tamano;

            // El material tiene que ser un asset: uno creado en memoria se pierde
            // al guardar el prefab y el objeto sale rosa al instanciarlo.
            temporal.GetComponent<Renderer>().sharedMaterial =
                CrearMaterialAsset(nombreItem, color);

            // El item sujeto no tiene que colisionar con nada. InventarioJugador
            // tambien desactiva la fisica al instanciarlo, pero mejor que el prefab
            // ya venga limpio.
            Object.DestroyImmediate(temporal.GetComponent<BoxCollider>());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporal, ruta);
            Object.DestroyImmediate(temporal);

            return prefab;
        }

        // El campo id es privado con [SerializeField]: se escribe por
        // SerializedObject, que es la via del editor para tocar privados.
        private static void ForzarIdDelItem(DatosItem item, string id)
        {
            SerializedObject so = new SerializedObject(item);
            SerializedProperty propiedad = so.FindProperty("id");

            if (propiedad != null && string.IsNullOrEmpty(propiedad.stringValue))
            {
                propiedad.stringValue = id;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
        }

        // Mete todos los DatosItem del proyecto en ItemsRegistrados, que es lo que
        // el inventario usa para resolver ids al cargar una partida.
        private static void PoblarRegistroDeItems()
        {
            ItemsRegistrados registro = BuscarOCrearRegistro();

            if (registro == null)
            {
                return;
            }

            registro.items.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:DatosItem"))
            {
                DatosItem item = AssetDatabase.LoadAssetAtPath<DatosItem>(
                    AssetDatabase.GUIDToAssetPath(guid));

                if (item != null)
                {
                    registro.items.Add(item);
                }
            }

            EditorUtility.SetDirty(registro);
            AssetDatabase.SaveAssets();
        }

        // El registro tiene que estar en Resources y llamarse ItemsRegistrados:
        // asi lo busca ItemsRegistrados.Instancia en tiempo de ejecucion.
        private static ItemsRegistrados BuscarOCrearRegistro()
        {
            const string ruta = "Assets/Resources/ItemsRegistrados.asset";

            ItemsRegistrados registro = AssetDatabase.LoadAssetAtPath<ItemsRegistrados>(ruta);

            if (registro != null)
            {
                return registro;
            }

            System.IO.Directory.CreateDirectory("Assets/Resources");

            registro = ScriptableObject.CreateInstance<ItemsRegistrados>();
            AssetDatabase.CreateAsset(registro, ruta);

            return registro;
        }

        private static DatosItem CargarItem(string nombreAsset)
        {
            return AssetDatabase.LoadAssetAtPath<DatosItem>(
                $"{CarpetaItems}/{nombreAsset}.asset");
        }

        // Registra la escena en Build Settings si no estaba, para poder jugarla
        // desde un build y no solo desde el editor.
        private static void AnadirABuildSettings()
        {
            var escenas = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            if (escenas.Exists(e => e.path == RutaEscena))
            {
                return;
            }

            escenas.Add(new EditorBuildSettingsScene(RutaEscena, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }

        // --- Escenario -------------------------------------------------------

        private static void CrearIluminacion()
        {
            GameObject sol = new GameObject("Luz direccional");
            Light luz = sol.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            luz.shadows = LightShadows.Soft;
            sol.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // EstadoPartida se pone en la escena, no se deja crear a ArranqueMenus:
        // asi lleva los valores de prueba en vez de los de produccion, y se
        // pueden tocar desde el inspector durante el Play.
        private static void CrearEstadoPartida()
        {
            GameObject objeto = new GameObject("EstadoPartida");

            EstadoPartida estado = objeto.AddComponent<EstadoPartida>();
            estado.duracionNivel = DuracionNivelPrueba;
            estado.impactoObjetivo = ImpactoObjetivoPrueba;
            estado.sospechaMaxima = SospechaMaximaPrueba;
        }

        // Sin EventSystem los botones del panel de sabotaje no responden al raton.
        // GestorMenus tambien lo crea al arrancar, pero tenerlo en la escena hace
        // que la UI funcione aunque se abra sin pasar por los menus.
        private static void CrearEventSystem()
        {
            GameObject objeto = new GameObject("EventSystem");
            objeto.AddComponent<UnityEngine.EventSystems.EventSystem>();
            objeto.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Suelo, paredes exteriores y un par de tabiques interiores. Los tabiques
        // son lo que hace util la prueba: cortan la linea de vision de los NPC y
        // obligan a la camara a acercarse al jugador.
        private static void CrearEscenario()
        {
            GameObject raiz = new GameObject("Escenario");

            CrearCaja("Suelo", raiz.transform,
                new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 30f),
                new Color(0.55f, 0.55f, 0.58f));

            // Paredes exteriores, para que no te salgas del recinto.
            CrearCaja("ParedNorte", raiz.transform,
                new Vector3(0f, 1.5f, 15f), new Vector3(30f, 4f, 0.5f), ColorPared);
            CrearCaja("ParedSur", raiz.transform,
                new Vector3(0f, 1.5f, -15f), new Vector3(30f, 4f, 0.5f), ColorPared);
            CrearCaja("ParedEste", raiz.transform,
                new Vector3(15f, 1.5f, 0f), new Vector3(0.5f, 4f, 30f), ColorPared);
            CrearCaja("ParedOeste", raiz.transform,
                new Vector3(-15f, 1.5f, 0f), new Vector3(0.5f, 4f, 30f), ColorPared);

            // Tabiques interiores: los obstaculos que tapan la vision.
            CrearCaja("Tabique1", raiz.transform,
                new Vector3(-4f, 1.25f, 3f), new Vector3(0.4f, 3.5f, 9f), ColorTabique);
            CrearCaja("Tabique2", raiz.transform,
                new Vector3(5f, 1.25f, -4f), new Vector3(9f, 3.5f, 0.4f), ColorTabique);

            // Mesas donde apoyar la taza y el objeto recogible.
            CrearCaja("Mesa1", raiz.transform,
                new Vector3(3f, 0.4f, 6f), new Vector3(2.5f, 0.8f, 1.2f), ColorMueble);
            CrearCaja("Mesa2", raiz.transform,
                new Vector3(-8f, 0.4f, -6f), new Vector3(2.5f, 0.8f, 1.2f), ColorMueble);
        }

        private static readonly Color ColorPared = new Color(0.72f, 0.70f, 0.66f);
        private static readonly Color ColorTabique = new Color(0.60f, 0.62f, 0.68f);
        private static readonly Color ColorMueble = new Color(0.45f, 0.33f, 0.24f);

        private static GameObject CrearCaja(string nombre, Transform padre,
            Vector3 posicion, Vector3 escala, Color color)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.name = nombre;
            caja.transform.SetParent(padre, false);
            caja.transform.position = posicion;
            caja.transform.localScale = escala;

            Pintar(caja, color);

            return caja;
        }

        // Crea un material por objeto. Es un derroche en produccion, pero en una
        // escena de prueba evita que cambiar un color afecte a todo lo demas.
        private static void Pintar(GameObject objeto, Color color)
        {
            Renderer render = objeto.GetComponent<Renderer>();

            if (render == null)
            {
                return;
            }

            // Se busca el shader del pipeline activo: URP y el integrado usan
            // nombres distintos y el equivocado sale rosa.
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");

            Material material = new Material(shader) { color = color };

            // El color va por las dos propiedades porque URP lee _BaseColor y el
            // integrado _Color.
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            render.sharedMaterial = material;
        }

        // --- Jugador y camara -------------------------------------------------

        private static GameObject CrearJugador()
        {
            GameObject jugador = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            jugador.name = "Jugador";
            jugador.transform.position = new Vector3(0f, 1f, -8f);
            Pintar(jugador, new Color(0.25f, 0.5f, 0.85f));

            // El CharacterController hace de colisionador: el de la capsula
            // primitiva estorbaria y dispararia detecciones contra si mismo.
            Object.DestroyImmediate(jugador.GetComponent<CapsuleCollider>());

            AsegurarTagJugador(jugador);

            CharacterController controlador = jugador.AddComponent<CharacterController>();
            controlador.height = 2f;
            controlador.radius = 0.4f;
            controlador.center = Vector3.zero;

            jugador.AddComponent<MovimientoJugador>();
            jugador.AddComponent<InventarioJugador>();

            InteractorJugador interactor = jugador.AddComponent<InteractorJugador>();

            // 4 metros desde el jugador. El rayo sale de el y no de la camara, asi
            // que el alcance es el mismo en primera y en tercera persona.
            interactor.alcance = 4f;

            // Marca de "frente" para saber hacia donde mira la capsula, que por si
            // sola no tiene orientacion visible.
            GameObject nariz = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nariz.name = "Frente";
            nariz.transform.SetParent(jugador.transform, false);
            nariz.transform.localPosition = new Vector3(0f, 0.35f, 0.45f);
            nariz.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            Object.DestroyImmediate(nariz.GetComponent<BoxCollider>());
            Pintar(nariz, new Color(0.95f, 0.95f, 0.3f));

            return jugador;
        }

        // El tag Player tiene que existir en el proyecto o AddComponent falla al
        // asignarlo. Varios scripts buscan al jugador por el.
        private static void AsegurarTagJugador(GameObject jugador)
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty tags = tagManager.FindProperty("tags");

            bool existe = false;
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == TagJugador)
                {
                    existe = true;
                    break;
                }
            }

            if (!existe)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = TagJugador;
                tagManager.ApplyModifiedProperties();
            }

            jugador.tag = TagJugador;
        }

        private static void CrearCamara(Transform jugador)
        {
            GameObject objeto = new GameObject("Camara");
            objeto.tag = "MainCamera";
            objeto.transform.position = new Vector3(0f, 3f, -12f);

            objeto.AddComponent<Camera>();
            objeto.AddComponent<AudioListener>();

            CamaraTerceraPersona camara = objeto.AddComponent<CamaraTerceraPersona>();
            camara.objetivo = jugador;

            // Sin esto la camara atraviesa las paredes del escenario.
            camara.capasColision = ~0;
        }

        // --- Interfaces -------------------------------------------------------

        // Canvas a pantalla completa, como los que arma ConstructorUI.
        private static Canvas CrearCanvas(string nombre, int orden)
        {
            GameObject objeto = new GameObject(nombre);

            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = orden;

            CanvasScaler escalador = objeto.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;

            objeto.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        private static Text CrearTexto(string nombre, Transform padre, string contenido,
            int tamano, TextAnchor alineacion)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);

            Text texto = objeto.AddComponent<Text>();
            texto.text = contenido;
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = tamano;
            texto.alignment = alineacion;
            texto.color = Color.white;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;

            return texto;
        }

        private static GameObject CrearPanel(string nombre, Transform padre, Color color)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.AddComponent<Image>().color = color;
            return objeto;
        }

        // Prompt de "[E] Interactuar", centrado un poco por debajo del medio.
        private static void CrearUiInteraccion()
        {
            Canvas canvas = CrearCanvas("CanvasInteraccion", 10);

            Text texto = CrearTexto("TextoInteraccion", canvas.transform, "",
                26, TextAnchor.MiddleCenter);

            RectTransform rect = texto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -120f);
            rect.sizeDelta = new Vector2(700f, 44f);

            InterfazTextoInteraccion interfaz =
                canvas.gameObject.AddComponent<InterfazTextoInteraccion>();
            interfaz.raiz = texto.gameObject;
            interfaz.texto = texto;
        }

        // Cuadro de dialogo abajo, con nombre del NPC y la frase.
        private static void CrearUiDialogo()
        {
            Canvas canvas = CrearCanvas("CanvasDialogo", 20);

            GameObject panel = CrearPanel("PanelDialogo", canvas.transform,
                new Color(0.05f, 0.05f, 0.09f, 0.92f));

            RectTransform rectPanel = panel.GetComponent<RectTransform>();
            rectPanel.anchorMin = new Vector2(0.5f, 0f);
            rectPanel.anchorMax = new Vector2(0.5f, 0f);
            rectPanel.pivot = new Vector2(0.5f, 0f);
            rectPanel.anchoredPosition = new Vector2(0f, 40f);
            rectPanel.sizeDelta = new Vector2(1100f, 200f);

            Text nombre = CrearTexto("Nombre", panel.transform, "", 28, TextAnchor.UpperLeft);
            RectTransform rectNombre = nombre.GetComponent<RectTransform>();
            rectNombre.anchorMin = new Vector2(0f, 1f);
            rectNombre.anchorMax = new Vector2(0f, 1f);
            rectNombre.pivot = new Vector2(0f, 1f);
            rectNombre.anchoredPosition = new Vector2(30f, -18f);
            rectNombre.sizeDelta = new Vector2(600f, 36f);
            nombre.color = new Color(0.95f, 0.75f, 0.25f);

            Text frase = CrearTexto("Frase", panel.transform, "", 24, TextAnchor.UpperLeft);
            RectTransform rectFrase = frase.GetComponent<RectTransform>();
            rectFrase.anchorMin = new Vector2(0f, 0f);
            rectFrase.anchorMax = new Vector2(1f, 1f);
            rectFrase.offsetMin = new Vector2(30f, 50f);
            rectFrase.offsetMax = new Vector2(-30f, -60f);

            Text continuar = CrearTexto("Continuar", panel.transform,
                "[ESPACIO]", 18, TextAnchor.LowerRight);
            RectTransform rectContinuar = continuar.GetComponent<RectTransform>();
            rectContinuar.anchorMin = new Vector2(1f, 0f);
            rectContinuar.anchorMax = new Vector2(1f, 0f);
            rectContinuar.pivot = new Vector2(1f, 0f);
            rectContinuar.anchoredPosition = new Vector2(-30f, 16f);
            rectContinuar.sizeDelta = new Vector2(220f, 26f);
            continuar.color = new Color(0.55f, 0.55f, 0.60f);

            InterfazDialogo interfaz = canvas.gameObject.AddComponent<InterfazDialogo>();
            interfaz.panel = panel;
            interfaz.textoNombre = nombre;
            interfaz.textoDialogo = frase;
            interfaz.indicadorContinuar = continuar.gameObject;

            panel.SetActive(false);

            // El gestor busca solo la interfaz y al jugador en su Awake.
            new GameObject("GestorDialogos").AddComponent<GestorDialogos>();
        }

        // Panel de sabotaje con sus 4 opciones.
        private static void CrearMenuSabotaje()
        {
            Canvas canvas = CrearCanvas("CanvasSabotaje", 30);

            GameObject panel = CrearPanel("PanelSabotaje", canvas.transform,
                new Color(0.10f, 0.10f, 0.15f, 0.97f));

            RectTransform rectPanel = panel.GetComponent<RectTransform>();
            rectPanel.anchorMin = new Vector2(0.5f, 0.5f);
            rectPanel.anchorMax = new Vector2(0.5f, 0.5f);
            rectPanel.anchoredPosition = Vector2.zero;
            rectPanel.sizeDelta = new Vector2(560f, 520f);

            Text titulo = CrearTexto("Titulo", panel.transform,
                "SABOTEAR CAFE", 34, TextAnchor.MiddleCenter);
            RectTransform rectTitulo = titulo.GetComponent<RectTransform>();
            rectTitulo.anchorMin = new Vector2(0.5f, 1f);
            rectTitulo.anchorMax = new Vector2(0.5f, 1f);
            rectTitulo.pivot = new Vector2(0.5f, 1f);
            rectTitulo.anchoredPosition = new Vector2(0f, -30f);
            rectTitulo.sizeDelta = new Vector2(500f, 46f);
            titulo.color = new Color(0.95f, 0.75f, 0.25f);

            MenuSabotajeTaza menu = canvas.gameObject.AddComponent<MenuSabotajeTaza>();
            menu.panel = panel;
            menu.textoTitulo = titulo;

            float y = -110f;
            menu.botonNormal = CrearBotonSabotaje(panel.transform, "Cafe normal", ref y);
            menu.botonSal = CrearBotonSabotaje(panel.transform, "Con sal", ref y);
            menu.botonFrio = CrearBotonSabotaje(panel.transform, "Frio y rancio", ref y);
            menu.botonDetergente = CrearBotonSabotaje(panel.transform, "Con detergente", ref y);

            Text resultado = CrearTexto("Resultado", panel.transform, "",
                20, TextAnchor.MiddleCenter);
            RectTransform rectResultado = resultado.GetComponent<RectTransform>();
            rectResultado.anchorMin = new Vector2(0.5f, 0f);
            rectResultado.anchorMax = new Vector2(0.5f, 0f);
            rectResultado.pivot = new Vector2(0.5f, 0f);
            rectResultado.anchoredPosition = new Vector2(0f, 60f);
            rectResultado.sizeDelta = new Vector2(500f, 60f);
            menu.textoResultado = resultado;

            Text ayuda = CrearTexto("Ayuda", panel.transform,
                "[X] Salir", 18, TextAnchor.MiddleCenter);
            RectTransform rectAyuda = ayuda.GetComponent<RectTransform>();
            rectAyuda.anchorMin = new Vector2(0.5f, 0f);
            rectAyuda.anchorMax = new Vector2(0.5f, 0f);
            rectAyuda.pivot = new Vector2(0.5f, 0f);
            rectAyuda.anchoredPosition = new Vector2(0f, 24f);
            rectAyuda.sizeDelta = new Vector2(300f, 26f);
            ayuda.color = new Color(0.55f, 0.55f, 0.60f);

            panel.SetActive(false);
        }

        private static Button CrearBotonSabotaje(Transform padre, string etiqueta, ref float y)
        {
            GameObject objeto = CrearPanel($"Boton{etiqueta}", padre,
                new Color(0.22f, 0.22f, 0.30f));

            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(420f, 60f);

            Button boton = objeto.AddComponent<Button>();
            boton.targetGraphic = objeto.GetComponent<Image>();

            Text texto = CrearTexto("Etiqueta", objeto.transform, etiqueta,
                24, TextAnchor.MiddleCenter);
            RectTransform rectTexto = texto.GetComponent<RectTransform>();
            rectTexto.anchorMin = Vector2.zero;
            rectTexto.anchorMax = Vector2.one;
            rectTexto.offsetMin = Vector2.zero;
            rectTexto.offsetMax = Vector2.zero;

            y -= 72f;

            return boton;
        }

        // --- Interactuables ---------------------------------------------------

        private static void CrearObjetosInteractuables()
        {
            GameObject raiz = new GameObject("Interactuables");

            // Una taza pide detergente: sirve para probar el requisito de item.
            CrearTaza("TazaConRequisito", raiz.transform,
                new Vector3(3f, 0.95f, 6f), exigeDetergente: true);

            // Y varias sueltas, repartidas, para poder encadenar sabotajes: con
            // una sola no se llega ni al impacto objetivo ni al tope de sospecha.
            CrearTaza("Taza2", raiz.transform, new Vector3(-8f, 0.95f, -5f), false);
            CrearTaza("Taza3", raiz.transform, new Vector3(12f, 0.3f, -12f), false);
            CrearTaza("Taza4", raiz.transform, new Vector3(-12f, 0.3f, -12f), false);
            CrearTaza("Taza5", raiz.transform, new Vector3(12f, 0.3f, 4f), false);
            CrearTaza("Taza6", raiz.transform, new Vector3(0f, 0.3f, 12f), false);

            // Los tres objetos que se pueden recoger, repartidos por el mapa.
            CrearRecogible(ItemPala, raiz.transform,
                new Vector3(-8f, 0.95f, -6f), new Vector3(0.18f, 0.7f, 0.18f));

            CrearRecogible(ItemPalita, raiz.transform,
                new Vector3(11f, 0.3f, 11f), new Vector3(0.12f, 0.3f, 0.12f));

            CrearRecogible(ItemDetergente, raiz.transform,
                new Vector3(-11f, 0.35f, 11f), new Vector3(0.2f, 0.5f, 0.2f));
        }

        // Taza saboteable. Con exigeDetergente pide llevar el item en la mano,
        // que es el caso para probar RequisitoItem; las demas son libres para
        // poder encadenar sabotajes sin depender del inventario.
        private static void CrearTaza(string nombre, Transform padre,
            Vector3 posicion, bool exigeDetergente)
        {
            GameObject taza = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            taza.name = nombre;
            taza.transform.SetParent(padre, false);
            taza.transform.position = posicion;
            taza.transform.localScale = new Vector3(0.3f, 0.15f, 0.3f);

            // La que pide detergente se pinta distinta para localizarla de un vistazo.
            Pintar(taza, exigeDetergente
                ? new Color(0.35f, 0.80f, 0.85f)
                : new Color(0.90f, 0.90f, 0.92f));

            taza.AddComponent<SabotajeTazaInteractuable>();

            if (!exigeDetergente)
            {
                return;
            }

            RequisitoItem requisito = taza.AddComponent<RequisitoItem>();
            requisito.itemRequerido = CargarItem(ItemDetergente);
            requisito.debeEstarEnLaMano = true;
            requisito.consumirAlUsar = true;
        }

        // Un objeto del mundo que se puede recoger y que lleva su DatosItem.
        // El color sale de ColorDe, el mismo que usan su icono y su prefab en la
        // mano, para que el objeto se reconozca en los tres sitios.
        private static void CrearRecogible(string nombreItem, Transform padre,
            Vector3 posicion, Vector3 escala)
        {
            GameObject objeto = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objeto.name = nombreItem;
            objeto.transform.SetParent(padre, false);
            objeto.transform.position = posicion;
            objeto.transform.localScale = escala;
            Pintar(objeto, ColorDe(nombreItem));

            ObjetoRecogible recogible = objeto.AddComponent<ObjetoRecogible>();
            recogible.item = CargarItem(nombreItem);
        }

        // --- NPCs -------------------------------------------------------------

        private static void CrearNpcs()
        {
            GameObject raiz = new GameObject("NPCs");

            // NPC con ruta en bucle: recorre cuatro esquinas.
            RutaNPC rutaBucle = CrearRuta("RutaPatrulla", raiz.transform,
                RutaNPC.ModoRecorrido.Bucle, new[]
                {
                    new Vector3(-10f, 0f, 10f),
                    new Vector3(10f, 0f, 10f),
                    new Vector3(10f, 0f, -10f),
                    new Vector3(-10f, 0f, -10f)
                });

            GameObject patrulla = CrearNpc("Supervisor", "Supervisor",
                new Vector3(-10f, 1f, 10f), raiz.transform,
                new Color(0.85f, 0.30f, 0.30f));
            patrulla.AddComponent<PatrullaNPC>().ruta = rutaBucle;

            // NPC con ruta de ida y vuelta: pasillo corto.
            RutaNPC rutaVaiven = CrearRuta("RutaPasillo", raiz.transform,
                RutaNPC.ModoRecorrido.IdaYVuelta, new[]
                {
                    new Vector3(-12f, 0f, 0f),
                    new Vector3(-12f, 0f, 8f)
                });

            GameObject vaiven = CrearNpc("Companero", "Compañero",
                new Vector3(-12f, 1f, 0f), raiz.transform,
                new Color(0.30f, 0.70f, 0.40f));
            vaiven.AddComponent<PatrullaNPC>().ruta = rutaVaiven;

            // NPC estatico: sin PatrullaNPC, se queda donde esta.
            // Lleva el encargo: te pide la Pala y cuela la Palita como cambiazo.
            GameObject recepcionista = CrearNpc("Recepcionista", "Ejemplo",
                new Vector3(7f, 1f, 9f), raiz.transform,
                new Color(0.40f, 0.45f, 0.85f));

            MisionEntrega mision = recepcionista.AddComponent<MisionEntrega>();
            mision.itemCorrecto = CargarItem(ItemPala);
            mision.itemsSaboteados = new[] { CargarItem(ItemPalita) };

            // Basta con llevar el objeto encima. Exigir que este en la mano hace
            // que la entrega falle si recogiste otra cosa antes, porque solo el
            // primer objeto recogido cae en el slot seleccionado.
            mision.debeEstarEnLaMano = false;
        }

        // Un NPC: capsula con dialogo, vision y el ojo indicador.
        private static GameObject CrearNpc(string nombre, string idDialogo,
            Vector3 posicion, Transform padre, Color color)
        {
            GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = nombre;
            npc.transform.SetParent(padre, false);
            npc.transform.position = posicion;
            Pintar(npc, color);

            DialogoNPC dialogo = npc.AddComponent<DialogoNPC>();
            dialogo.nombre = nombre;
            // El id apunta a Resources/Nivel/Dialogos/{id}/: si no coincide con una
            // carpeta existente, el NPC no tiene nada que decir.
            dialogo.id = idDialogo;

            npc.AddComponent<DetectorVisionNPC>();

            IndicadorVisionNPC indicador = npc.AddComponent<IndicadorVisionNPC>();
            AsignarSpritesDelOjo(indicador);

            // Marca de "frente", igual que la del jugador.
            GameObject nariz = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nariz.name = "Frente";
            nariz.transform.SetParent(npc.transform, false);
            nariz.transform.localPosition = new Vector3(0f, 0.35f, 0.45f);
            nariz.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            Object.DestroyImmediate(nariz.GetComponent<BoxCollider>());
            Pintar(nariz, Color.white);

            return npc;
        }

        // Engancha los PNG del ojo por nombre. Si no estan, el indicador queda
        // sin sprites y simplemente no dibuja nada.
        private static void AsignarSpritesDelOjo(IndicadorVisionNPC indicador)
        {
            indicador.spriteSinVer = BuscarSprite("Ojo cerrado");
            indicador.spriteVeLejos = BuscarSprite("Ojo Viendo");
            indicador.spriteVeCerca = BuscarSprite("Ojo alerta");
        }

        private static Sprite BuscarSprite(string nombre)
        {
            foreach (string guid in AssetDatabase.FindAssets($"\"{nombre}\" t:Sprite"))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);

                if (sprite != null)
                {
                    return sprite;
                }
            }

            Debug.LogWarning($"[GeneradorEscenaPrueba] No se encontro el sprite '{nombre}'.");
            return null;
        }

        // Ruta con sus paradas como hijos, que es lo que RutaNPC espera.
        private static RutaNPC CrearRuta(string nombre, Transform padre,
            RutaNPC.ModoRecorrido modo, Vector3[] posiciones)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);

            RutaNPC ruta = objeto.AddComponent<RutaNPC>();
            ruta.modo = modo;

            for (int i = 0; i < posiciones.Length; i++)
            {
                GameObject punto = new GameObject($"Punto{i + 1}");
                punto.transform.SetParent(objeto.transform, false);
                punto.transform.position = posiciones[i];
            }

            return ruta;
        }
    }
}
