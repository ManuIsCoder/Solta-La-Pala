using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SoltaLaPala.Minijuegos;
using SoltaLaPala.Interaction;
using SoltaLaPala.Menus;

namespace SoltaLaPala.Editor
{
    public static class GeneradorMinijuegoComputadora
    {
        private const string CarpetaPrefabsInteractuables = "Assets/Prefabs/Interactuable minijuego";
        private const string CarpetaSpritesMinijuegos = "Assets/Sprites/Minijuegos";
        private const string RutaLaptopBase = "Assets/librerias office/LowPolyOfficeProps_FULL/Prefabs/Laptop_Off.prefab";

        private const string RutaFondoComputadora = CarpetaSpritesMinijuegos + "/fondo computadora.png";
        private const string RutaPapelera = CarpetaSpritesMinijuegos + "/logo_papelera.png";
        private const string RutaCartaAmor = CarpetaSpritesMinijuegos + "/icono_carta_a_mama_txt.png";
        private const string RutaFotoViaje = CarpetaSpritesMinijuegos + "/icono_gato_foto_png.png";
        private const string RutaSql = CarpetaSpritesMinijuegos + "/logo_sql.png";
        private const string RutaXlsx = CarpetaSpritesMinijuegos + "/icono_reporteEmpresa2026_xlsx.png";
        private const string RutaMp4 = CarpetaSpritesMinijuegos + "/logo_mp4.png";

        [MenuItem("Solta La Pala/Generar Minijuego Borrar Datos (Prefabs y UI)")]
        public static void GenerarTodo()
        {
            AsegurarCarpetas();
            ConfigurarSprites();

            GameObject prefabPanelUI = GenerarPrefabPanelUI();
            GenerarPrefabInteractuable(prefabPanelUI);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GeneradorMinijuegoComputadora] Prefabs generados en " + CarpetaPrefabsInteractuables);
        }

        [MenuItem("Solta La Pala/Colocar Minijuego Borrar Datos en Escena Actual")]
        public static void ColocarEnEscenaActual()
        {
            GenerarTodo();

            string rutaPrefabPanel = CarpetaPrefabsInteractuables + "/PanelMinijuegoBorrarDatos.prefab";
            string rutaPrefabInteractuable = CarpetaPrefabsInteractuables + "/ComputadoraInteractuable.prefab";

            GameObject prefabPanel = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefabPanel);
            GameObject prefabInteractuable = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefabInteractuable);

            if (prefabPanel == null || prefabInteractuable == null)
            {
                EditorUtility.DisplayDialog("Error", "No se encontraron los prefabs generados.", "Aceptar");
                return;
            }

            // Buscar o crear Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = ConstructorUI.CrearCanvas("Canvasminijuegos", null, 10);
            }

            // Buscar si ya existe el panel
            MinijuegoBorrarDatos panelExistente = Object.FindObjectOfType<MinijuegoBorrarDatos>(true);
            MinijuegoBorrarDatos scriptPanel;
            if (panelExistente == null)
            {
                GameObject instPanel = (GameObject)PrefabUtility.InstantiatePrefab(prefabPanel, canvas.transform);
                instPanel.SetActive(false);
                scriptPanel = instPanel.GetComponent<MinijuegoBorrarDatos>();
                Undo.RegisterCreatedObjectUndo(instPanel, "Instanciar Panel Minijuego Borrar Datos");
            }
            else
            {
                scriptPanel = panelExistente;
            }

            // Instanciar computadora interactuable en escena
            ComputadoraInteractuable compExistente = Object.FindObjectOfType<ComputadoraInteractuable>();
            if (compExistente == null)
            {
                GameObject instComp = (GameObject)PrefabUtility.InstantiatePrefab(prefabInteractuable);
                instComp.transform.position = new Vector3(0f, 1f, 0f);
                ComputadoraInteractuable comp = instComp.GetComponent<ComputadoraInteractuable>();
                if (comp != null)
                {
                    comp.minijuegoBorrarDatos = scriptPanel;
                }
                Undo.RegisterCreatedObjectUndo(instComp, "Instanciar Computadora Interactuable");
                Selection.activeGameObject = instComp;
            }
            else
            {
                compExistente.minijuegoBorrarDatos = scriptPanel;
                Selection.activeGameObject = compExistente.gameObject;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Éxito", "Minijuego de Borrar Datos colocado en la escena actual correctamente.", "Aceptar");
        }

        private static void AsegurarCarpetas()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder(CarpetaPrefabsInteractuables))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Interactuable minijuego");
            }
        }

        private static void ConfigurarSprites()
        {
            string[] rutas = new[]
            {
                RutaFondoComputadora,
                RutaPapelera,
                RutaCartaAmor,
                RutaFotoViaje,
                RutaSql,
                RutaXlsx,
                RutaMp4
            };

            foreach (var ruta in rutas)
            {
                if (!(AssetImporter.GetAtPath(ruta) is TextureImporter importador))
                {
                    continue;
                }

                bool necesitaReimport = false;

                if (importador.textureType != TextureImporterType.Sprite)
                {
                    importador.textureType = TextureImporterType.Sprite;
                    importador.spriteImportMode = SpriteImportMode.Single;
                    necesitaReimport = true;
                }

                if (importador.alphaIsTransparency != true)
                {
                    importador.alphaIsTransparency = true;
                    necesitaReimport = true;
                }

                if (importador.mipmapEnabled != false)
                {
                    importador.mipmapEnabled = false;
                    necesitaReimport = true;
                }

                if (necesitaReimport)
                {
                    importador.SaveAndReimport();
                }
            }
        }

        private static GameObject GenerarPrefabPanelUI()
        {
            // Canvas / Raiz del panel
            GameObject panelRaiz = new GameObject("PanelMinijuegoBorrarDatos", typeof(RectTransform));
            panelRaiz.layer = LayerMask.NameToLayer("UI");

            RectTransform rtRaiz = panelRaiz.GetComponent<RectTransform>();
            rtRaiz.sizeDelta = new Vector2(1920, 1080);
            rtRaiz.anchorMin = new Vector2(0.5f, 0.5f);
            rtRaiz.anchorMax = new Vector2(0.5f, 0.5f);
            rtRaiz.pivot = new Vector2(0.5f, 0.5f);

            MinijuegoBorrarDatos scriptMinijuego = panelRaiz.AddComponent<MinijuegoBorrarDatos>();
            scriptMinijuego.panelRaiz = panelRaiz;

            // Imagen de Fondo
            GameObject objFondo = new GameObject("FondoComputadora", typeof(RectTransform), typeof(Image));
            objFondo.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtFondo = objFondo.GetComponent<RectTransform>();
            ConstructorUI.Estirar(rtFondo);
            Image imgFondo = objFondo.GetComponent<Image>();
            Sprite spriteFondo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaFondoComputadora);
            imgFondo.sprite = spriteFondo;
            imgFondo.color = Color.white;
            scriptMinijuego.imagenFondo = imgFondo;
            scriptMinijuego.spriteFondoComputadora = spriteFondo;

            // Área de Escritorio / Contenedor de archivos
            GameObject objEscritorio = new GameObject("AreaEscritorio", typeof(RectTransform));
            objEscritorio.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtEscritorio = objEscritorio.GetComponent<RectTransform>();
            rtEscritorio.anchorMin = new Vector2(0.04f, 0.08f);
            rtEscritorio.anchorMax = new Vector2(0.96f, 0.92f);
            rtEscritorio.offsetMin = Vector2.zero;
            rtEscritorio.offsetMax = Vector2.zero;
            scriptMinijuego.contenedorArchivos = rtEscritorio;

            // Papelera (claramente dentro de la pantalla a la derecha y con tamaño grande)
            GameObject objPapelera = new GameObject("ZonaPapelera", typeof(RectTransform), typeof(Image));
            objPapelera.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtPapelera = objPapelera.GetComponent<RectTransform>();
            rtPapelera.anchorMin = new Vector2(1f, 0.5f);
            rtPapelera.anchorMax = new Vector2(1f, 0.5f);
            rtPapelera.pivot = new Vector2(0.5f, 0.5f);
            rtPapelera.anchoredPosition = new Vector2(-150f, 0f);
            rtPapelera.sizeDelta = new Vector2(140f, 140f);

            Image imgPapelera = objPapelera.GetComponent<Image>();
            Sprite spritePap = AssetDatabase.LoadAssetAtPath<Sprite>(RutaPapelera);
            imgPapelera.sprite = spritePap;
            imgPapelera.preserveAspect = true;

            scriptMinijuego.zonaPapelera = rtPapelera;
            scriptMinijuego.imagenPapelera = imgPapelera;
            scriptMinijuego.spritePapelera = spritePap;

            // Texto de instrucciones
            GameObject objTextoInstruc = new GameObject("TextoInstrucciones", typeof(RectTransform), typeof(Text));
            objTextoInstruc.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtTxtInst = objTextoInstruc.GetComponent<RectTransform>();
            rtTxtInst.anchorMin = new Vector2(0.5f, 0.94f);
            rtTxtInst.anchorMax = new Vector2(0.5f, 0.94f);
            rtTxtInst.pivot = new Vector2(0.5f, 0.5f);
            rtTxtInst.sizeDelta = new Vector2(900f, 40f);
            Text txtInstruc = objTextoInstruc.GetComponent<Text>();
            txtInstruc.font = ConstructorUI.Fuente;
            txtInstruc.fontSize = 22;
            txtInstruc.alignment = TextAnchor.MiddleCenter;
            txtInstruc.color = Color.white;
            txtInstruc.text = "Arrastra a la Papelera los archivos personales sin borrar los de la empresa.";
            scriptMinijuego.textoInstrucciones = txtInstruc;

            // Texto de contador
            GameObject objTextoContador = new GameObject("TextoContador", typeof(RectTransform), typeof(Text));
            objTextoContador.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtTxtCont = objTextoContador.GetComponent<RectTransform>();
            rtTxtCont.anchorMin = new Vector2(1f, 0.5f);
            rtTxtCont.anchorMax = new Vector2(1f, 0.5f);
            rtTxtCont.pivot = new Vector2(0.5f, 0.5f);
            rtTxtCont.anchoredPosition = new Vector2(-150f, -95f);
            rtTxtCont.sizeDelta = new Vector2(180f, 50f);
            Text txtCont = objTextoContador.GetComponent<Text>();
            txtCont.font = ConstructorUI.Fuente;
            txtCont.fontSize = 14;
            txtCont.alignment = TextAnchor.UpperCenter;
            txtCont.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            scriptMinijuego.textoContador = txtCont;

            // Botón Salir estilizado
            GameObject objBtnSalir = new GameObject("BotonSalir", typeof(RectTransform), typeof(Image), typeof(Button));
            objBtnSalir.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtSalir = objBtnSalir.GetComponent<RectTransform>();
            rtSalir.anchorMin = new Vector2(1f, 1f);
            rtSalir.anchorMax = new Vector2(1f, 1f);
            rtSalir.pivot = new Vector2(1f, 1f);
            rtSalir.anchoredPosition = new Vector2(-25f, -25f);
            rtSalir.sizeDelta = new Vector2(110f, 40f);
            objBtnSalir.GetComponent<Image>().color = new Color(0.78f, 0.20f, 0.20f, 0.95f);
            Button btnSalir = objBtnSalir.GetComponent<Button>();
            ColorBlock cb = ConstructorUI.ColoresBoton();
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            btnSalir.colors = cb;
            scriptMinijuego.botonSalir = btnSalir;

            GameObject objTxtSalir = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            objTxtSalir.transform.SetParent(objBtnSalir.transform, false);
            ConstructorUI.Estirar(objTxtSalir.GetComponent<RectTransform>());
            Text tSalir = objTxtSalir.GetComponent<Text>();
            tSalir.font = ConstructorUI.Fuente;
            tSalir.fontSize = 15;
            tSalir.alignment = TextAnchor.MiddleCenter;
            tSalir.text = "✕ Salir";
            tSalir.color = Color.white;

            // Botón Listo
            Button btnListo = ConstructorUI.CrearBoton("BotonListo", panelRaiz.transform, "Listo",
                new Vector2(0f, -480f), new Vector2(160f, 44f), null);
            btnListo.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            btnListo.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            btnListo.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            btnListo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -460f);
            scriptMinijuego.botonListo = btnListo;

            // Lista de archivos
            scriptMinijuego.listaArchivos = new List<MinijuegoBorrarDatos.DatosArchivo>
            {
                // Archivos indebidos (a eliminar)
                new MinijuegoBorrarDatos.DatosArchivo("CartaDeAmor.txt", AssetDatabase.LoadAssetAtPath<Sprite>(RutaCartaAmor), false),
                new MinijuegoBorrarDatos.DatosArchivo("Fotos_Viaje.png", AssetDatabase.LoadAssetAtPath<Sprite>(RutaFotoViaje), false),

                // Archivos de la empresa (NO borrar, arruinan la tarea)
                new MinijuegoBorrarDatos.DatosArchivo("BaseDeDatos.sql", AssetDatabase.LoadAssetAtPath<Sprite>(RutaSql), true),
                new MinijuegoBorrarDatos.DatosArchivo("Reporte2026.xlsx", AssetDatabase.LoadAssetAtPath<Sprite>(RutaXlsx), true),
                new MinijuegoBorrarDatos.DatosArchivo("VideoComercial.mp4", AssetDatabase.LoadAssetAtPath<Sprite>(RutaMp4), true)
            };

            // Guardar prefab
            string rutaPrefabPanel = CarpetaPrefabsInteractuables + "/PanelMinijuegoBorrarDatos.prefab";
            GameObject prefabGuardado = PrefabUtility.SaveAsPrefabAsset(panelRaiz, rutaPrefabPanel);
            Object.DestroyImmediate(panelRaiz);

            return prefabGuardado;
        }

        private static void GenerarPrefabInteractuable(GameObject prefabPanelUI)
        {
            GameObject laptopOriginal = AssetDatabase.LoadAssetAtPath<GameObject>(RutaLaptopBase);
            if (laptopOriginal == null)
            {
                Debug.LogWarning("[GeneradorMinijuegoComputadora] No se encontró " + RutaLaptopBase);
                return;
            }

            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(laptopOriginal);
            instancia.name = "ComputadoraInteractuable";

            if (instancia.GetComponent<ResaltadoInteractuable>() == null)
            {
                instancia.AddComponent<ResaltadoInteractuable>();
            }

            ComputadoraInteractuable comp = instancia.GetComponent<ComputadoraInteractuable>();
            if (comp == null)
            {
                comp = instancia.AddComponent<ComputadoraInteractuable>();
            }

            comp.estado = ComputadoraInteractuable.EstadoSwitch.Activado;
            comp.modoJuego = ComputadoraInteractuable.ModoJuegoComputadora.BorrarDatos;

            if (prefabPanelUI != null)
            {
                comp.minijuegoBorrarDatos = prefabPanelUI.GetComponent<MinijuegoBorrarDatos>();
            }

            string rutaDestino = CarpetaPrefabsInteractuables + "/ComputadoraInteractuable.prefab";
            PrefabUtility.SaveAsPrefabAsset(instancia, rutaDestino);
            Object.DestroyImmediate(instancia);
        }
    }
}
