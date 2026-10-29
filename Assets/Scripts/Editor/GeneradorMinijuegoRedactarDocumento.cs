using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SoltaLaPala.Minijuegos;
using SoltaLaPala.Interaction;
using SoltaLaPala.Menus;
using SoltaLaPala.Inventory;
using SoltaLaPala.Guardado;

namespace SoltaLaPala.Editor
{
    public static class GeneradorMinijuegoRedactarDocumento
    {
        private const string CarpetaPrefabsInteractuables = "Assets/Prefabs/Interactuable minijuego";
        private const string CarpetaSpritesMinijuegos = "Assets/Sprites/Minijuegos";
        private const string CarpetaItems = "Assets/Resources/Items";

        private const string RutaFondoEscritorio = CarpetaSpritesMinijuegos + "/fondo Escritorio.png";
        private const string RutaPapelBlanco = CarpetaSpritesMinijuegos + "/papelblanco-sinfondo.png";
        private const string RutaPapelEscrito = CarpetaSpritesMinijuegos + "/papelescrito-sinfondo.png";
        private const string RutaDibujo = "Assets/Materials/dibujo.png";

        private const string RutaBiromeIcono = CarpetaSpritesMinijuegos + "/birome_24px.png";
        private const string RutaCrayolaIcono = CarpetaSpritesMinijuegos + "/crayolas_24px.png";
        private const string RutaDocIcono = CarpetaSpritesMinijuegos + "/documento_importante_24px.png";
        private const string RutaDibujoIcono = CarpetaSpritesMinijuegos + "/hoja_dibujo_nene_24px.png";

        private const string RutaHojaFbx = "Assets/librerias office/LowPolyOfficeProps_FULL/Meshes/SheetOfPaper_A4_2Sided.fbx";
        private const string RutaBiromePrefab = "Assets/librerias office/Office Pack/Prefabs/Small Objects/Pen A.prefab";
        private const string RutaCrayolasFbx = "Assets/Modelos/crayolas.fbx";
        private const string RutaDocumentoPrefab = "Assets/Modelos/documento Variant.prefab";
        private const string RutaDibujoPrefab = "Assets/Modelos/Dibujo.prefab";

        [MenuItem("Solta La Pala/Generar Minijuego Redactar Documento (Prefabs y UI)")]
        public static void GenerarTodo()
        {
            AsegurarCarpetas();
            ConfigurarTexturasYSprites();
            CrearOActualizarItems();

            GameObject prefabPanelUI = GenerarPrefabPanelUI();
            GenerarPrefabInteractuable(prefabPanelUI);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GeneradorMinijuegoRedactarDocumento] Prefabs generados en " + CarpetaPrefabsInteractuables);
        }

        [MenuItem("Solta La Pala/Colocar Minijuego Redactar Documento en Escena Actual")]
        public static void ColocarEnEscenaActual()
        {
            GenerarTodo();

            string rutaPrefabPanel = CarpetaPrefabsInteractuables + "/PanelMinijuegoRedactarDocumento.prefab";
            string rutaPrefabInteractuable = CarpetaPrefabsInteractuables + "/HojaInteractuable.prefab";

            GameObject prefabPanel = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefabPanel);
            GameObject prefabInteractuable = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefabInteractuable);

            if (prefabPanel == null || prefabInteractuable == null)
            {
                EditorUtility.DisplayDialog("Error", "No se encontraron los prefabs generados.", "Aceptar");
                return;
            }

            // Buscar Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = ConstructorUI.CrearCanvas("Canvasminijuegos", null, 10);
            }

            // Buscar si ya existe el panel
            MinijuegoRedactarDocumento panelExistente = Object.FindObjectOfType<MinijuegoRedactarDocumento>(true);
            MinijuegoRedactarDocumento scriptPanel;
            if (panelExistente == null)
            {
                GameObject instPanel = (GameObject)PrefabUtility.InstantiatePrefab(prefabPanel, canvas.transform);
                instPanel.SetActive(false);
                scriptPanel = instPanel.GetComponent<MinijuegoRedactarDocumento>();
                Undo.RegisterCreatedObjectUndo(instPanel, "Instanciar Panel Minijuego Redactar Documento");
            }
            else
            {
                scriptPanel = panelExistente;
            }

            // Instanciar Hoja interactuable en escena
            HojaInteractuable hojaExistente = Object.FindObjectOfType<HojaInteractuable>();
            if (hojaExistente == null)
            {
                GameObject instHoja = (GameObject)PrefabUtility.InstantiatePrefab(prefabInteractuable);
                instHoja.transform.position = new Vector3(1.2f, 1f, 0.5f);
                HojaInteractuable hoja = instHoja.GetComponent<HojaInteractuable>();
                if (hoja != null)
                {
                    hoja.minijuego = scriptPanel;
                }
                Undo.RegisterCreatedObjectUndo(instHoja, "Instanciar Hoja Interactuable");
                Selection.activeGameObject = instHoja;
            }
            else
            {
                hojaExistente.minijuego = scriptPanel;
                Selection.activeGameObject = hojaExistente.gameObject;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Éxito", "Minijuego de Redactar Documento colocado en la escena actual correctamente.", "Aceptar");
        }

        private static void AsegurarCarpetas()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            if (!AssetDatabase.IsValidFolder(CarpetaPrefabsInteractuables))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Interactuable minijuego");

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (!AssetDatabase.IsValidFolder(CarpetaItems))
                AssetDatabase.CreateFolder("Assets/Resources", "Items");
        }

        private static void ConfigurarTexturasYSprites()
        {
            string[] rutasSprites = new[]
            {
                RutaFondoEscritorio,
                RutaBiromeIcono,
                RutaCrayolaIcono,
                RutaDocIcono,
                RutaDibujoIcono,
                RutaPapelBlanco
            };

            foreach (var ruta in rutasSprites)
            {
                if (AssetImporter.GetAtPath(ruta) is TextureImporter imp)
                {
                    bool mod = false;
                    if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; mod = true; }
                    if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; mod = true; }
                    if (imp.mipmapEnabled) { imp.mipmapEnabled = false; mod = true; }
                    if (mod) imp.SaveAndReimport();
                }
            }

            // Texturas que requieren lectura directa de píxeles para el minijuego
            string[] rutasTexturasLectura = new[]
            {
                RutaPapelEscrito,
                RutaDibujo,
                RutaPapelBlanco
            };

            foreach (var ruta in rutasTexturasLectura)
            {
                if (AssetImporter.GetAtPath(ruta) is TextureImporter imp)
                {
                    bool mod = false;
                    if (!imp.isReadable) { imp.isReadable = true; mod = true; }
                    if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; mod = true; }
                    if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; mod = true; }
                    if (mod) imp.SaveAndReimport();
                }
            }
        }

        private static void CrearOActualizarItems()
        {
            CrearItemIndividual("Birome", "Birome", RutaBiromeIcono, RutaBiromePrefab, "Una lapicera estándar de oficina para firmar y redactar informes.", false);
            CrearItemIndividual("Crayolas", "Crayolas", RutaCrayolaIcono, RutaCrayolasFbx, "Caja de crayolas infantiles. Ideales para garabatear o sabotear.", true);
            CrearItemIndividual("Documento", "Documento redactado", RutaDocIcono, RutaDocumentoPrefab, "Informe oficial de la empresa redactado en limpio.", false);
            CrearItemIndividual("Dibujo", "Dibujo infantil", RutaDibujoIcono, RutaDibujoPrefab, "Una hoja oficial arruinada con el dibujo de una casita.", true);

            // Actualizar catálogo general de ItemsRegistrados
            ItemsRegistrados catalogo = Resources.Load<ItemsRegistrados>("ItemsRegistrados");
            if (catalogo != null)
            {
                foreach (string guid in AssetDatabase.FindAssets("t:DatosItem", new[] { CarpetaItems }))
                {
                    string ruta = AssetDatabase.GUIDToAssetPath(guid);
                    DatosItem item = AssetDatabase.LoadAssetAtPath<DatosItem>(ruta);
                    if (item != null && !catalogo.items.Contains(item))
                    {
                        catalogo.items.Add(item);
                    }
                }
                EditorUtility.SetDirty(catalogo);
            }
        }

        private static void CrearItemIndividual(string id, string nombre, string rutaIcono, string rutaPrefab, string descripcion, bool esSospechoso)
        {
            string rutaAsset = $"{CarpetaItems}/{id}.asset";
            DatosItem item = AssetDatabase.LoadAssetAtPath<DatosItem>(rutaAsset);
            bool esNuevo = false;

            if (item == null)
            {
                item = ScriptableObject.CreateInstance<DatosItem>();
                esNuevo = true;
            }

            item.nombre = nombre;
            item.icono = AssetDatabase.LoadAssetAtPath<Sprite>(rutaIcono);
            item.prefabEnMano = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
            item.descripcion = descripcion;
            item.esSospechoso = esSospechoso;

            // Asignar campo id privado
            SerializedObject so = new SerializedObject(item);
            SerializedProperty propId = so.FindProperty("id");
            if (propId != null)
            {
                propId.stringValue = id;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (esNuevo)
            {
                AssetDatabase.CreateAsset(item, rutaAsset);
            }
            else
            {
                EditorUtility.SetDirty(item);
            }
        }

        private static GameObject GenerarPrefabPanelUI()
        {
            GameObject panelRaiz = new GameObject("PanelMinijuegoRedactarDocumento", typeof(RectTransform));
            panelRaiz.layer = LayerMask.NameToLayer("UI");

            RectTransform rtRaiz = panelRaiz.GetComponent<RectTransform>();
            rtRaiz.sizeDelta = new Vector2(1920, 1080);
            rtRaiz.anchorMin = new Vector2(0.5f, 0.5f);
            rtRaiz.anchorMax = new Vector2(0.5f, 0.5f);
            rtRaiz.pivot = new Vector2(0.5f, 0.5f);

            MinijuegoRedactarDocumento script = panelRaiz.AddComponent<MinijuegoRedactarDocumento>();
            script.panelRaiz = panelRaiz;

            // Cargar recursos
            Sprite spFondo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaFondoEscritorio);
            Sprite spPapel = AssetDatabase.LoadAssetAtPath<Sprite>(RutaPapelBlanco);
            Texture2D texReporte = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaPapelEscrito);
            Texture2D texDibujo = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaDibujo);
            Sprite spBirome = AssetDatabase.LoadAssetAtPath<Sprite>(RutaBiromeIcono);
            Sprite spCrayola = AssetDatabase.LoadAssetAtPath<Sprite>(RutaCrayolaIcono);
            Sprite spDocIcono = AssetDatabase.LoadAssetAtPath<Sprite>(RutaDocIcono);
            Sprite spDibujoIcono = AssetDatabase.LoadAssetAtPath<Sprite>(RutaDibujoIcono);

            script.spriteFondoEscritorio = spFondo;
            script.spritePapelBlanco = spPapel;
            script.texturaReporte = texReporte;
            script.texturaDibujo = texDibujo;
            script.spriteBirome = spBirome;
            script.spriteCrayola = spCrayola;
            script.spriteIconoDocumento = spDocIcono;
            script.spriteIconoDibujo = spDibujoIcono;

            // 1. Imagen de Fondo
            GameObject objFondo = new GameObject("FondoEscritorio", typeof(RectTransform), typeof(Image));
            objFondo.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtFondo = objFondo.GetComponent<RectTransform>();
            ConstructorUI.Estirar(rtFondo);
            Image imgFondo = objFondo.GetComponent<Image>();
            imgFondo.sprite = spFondo;
            imgFondo.color = Color.white;
            script.imagenFondoEscritorio = imgFondo;

            // 2. Contenedor de la Hoja
            GameObject objPapel = new GameObject("PapelBlanco", typeof(RectTransform), typeof(Image));
            objPapel.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtPapel = objPapel.GetComponent<RectTransform>();
            rtPapel.anchorMin = new Vector2(0.58f, 0.5f);
            rtPapel.anchorMax = new Vector2(0.58f, 0.5f);
            rtPapel.pivot = new Vector2(0.5f, 0.5f);
            rtPapel.sizeDelta = new Vector2(500, 640);

            Image imgPapel = objPapel.GetComponent<Image>();
            imgPapel.sprite = spPapel;
            imgPapel.color = Color.white;
            script.rectTransformPapel = rtPapel;
            script.imagenPapelFondo = imgPapel;

            // Capa de revelado (RawImage) sobre la hoja
            GameObject objRev = new GameObject("CapaRevelado", typeof(RectTransform), typeof(RawImage));
            objRev.transform.SetParent(objPapel.transform, false);
            RectTransform rtRev = objRev.GetComponent<RectTransform>();
            EstirarConMargenes(rtRev, 20, 20, 20, 20);
            RawImage rawRev = objRev.GetComponent<RawImage>();
            script.rawImageRevelado = rawRev;

            // 3. Panel Lateral Izquierdo de Controles
            GameObject panelHerramientas = new GameObject("PanelHerramientas", typeof(RectTransform), typeof(Image));
            panelHerramientas.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtPH = panelHerramientas.GetComponent<RectTransform>();
            rtPH.anchorMin = new Vector2(0.05f, 0.10f);
            rtPH.anchorMax = new Vector2(0.32f, 0.90f);
            rtPH.offsetMin = Vector2.zero;
            rtPH.offsetMax = Vector2.zero;

            Image imgPH = panelHerramientas.GetComponent<Image>();
            imgPH.color = new Color(0.11f, 0.13f, 0.18f, 0.92f);

            // Título lateral
            GameObject objTit = new GameObject("Titulo", typeof(RectTransform), typeof(Text));
            objTit.transform.SetParent(panelHerramientas.transform, false);
            RectTransform rtTit = objTit.GetComponent<RectTransform>();
            rtTit.anchorMin = new Vector2(0, 1);
            rtTit.anchorMax = new Vector2(1, 1);
            rtTit.pivot = new Vector2(0.5f, 1);
            rtTit.sizeDelta = new Vector2(0, 50);
            rtTit.anchoredPosition = new Vector2(0, -15);
            Text txtTit = objTit.GetComponent<Text>();
            txtTit.font = ConstructorUI.Fuente;
            txtTit.fontSize = 22;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.text = "Redactar Documento";
            txtTit.color = Color.white;

            // Botón Birome
            Image marcoBirome;
            GameObject objBtnBio = CrearBotonHerramientaUI(panelHerramientas.transform, "BotonBirome", "Escribir con Birome", spBirome, new Vector2(0, -100), out marcoBirome);
            script.botonBirome = objBtnBio.GetComponent<Button>();
            script.marcoSeleccionBirome = marcoBirome;

            // Botón Crayola
            Image marcoCrayola;
            GameObject objBtnCra = CrearBotonHerramientaUI(panelHerramientas.transform, "BotonCrayola", "Dibujar con Crayolas", spCrayola, new Vector2(0, -185), out marcoCrayola);
            script.botonCrayola = objBtnCra.GetComponent<Button>();
            script.marcoSeleccionCrayola = marcoCrayola;

            // Texto herramienta activa
            GameObject objTxtHer = new GameObject("TextoHerramientaActiva", typeof(RectTransform), typeof(Text));
            objTxtHer.transform.SetParent(panelHerramientas.transform, false);
            RectTransform rtTxtHer = objTxtHer.GetComponent<RectTransform>();
            rtTxtHer.anchorMin = new Vector2(0, 0.5f);
            rtTxtHer.anchorMax = new Vector2(1, 0.5f);
            rtTxtHer.pivot = new Vector2(0.5f, 0.5f);
            rtTxtHer.sizeDelta = new Vector2(-20, 36);
            rtTxtHer.anchoredPosition = new Vector2(0, -10);
            Text txtHer = objTxtHer.GetComponent<Text>();
            txtHer.font = ConstructorUI.Fuente;
            txtHer.fontSize = 15;
            txtHer.alignment = TextAnchor.MiddleCenter;
            txtHer.color = Color.white;
            txtHer.text = "Herramienta: Birome";
            script.textoHerramientaActiva = txtHer;

            // Barra de progreso
            GameObject objBarraProg = new GameObject("BarraProgreso", typeof(RectTransform), typeof(Slider));
            objBarraProg.transform.SetParent(panelHerramientas.transform, false);
            RectTransform rtBP = objBarraProg.GetComponent<RectTransform>();
            rtBP.anchorMin = new Vector2(0.1f, 0.36f);
            rtBP.anchorMax = new Vector2(0.9f, 0.36f);
            rtBP.pivot = new Vector2(0.5f, 0.5f);
            rtBP.sizeDelta = new Vector2(0, 22);

            Slider sldProg = objBarraProg.GetComponent<Slider>();
            sldProg.minValue = 0f;
            sldProg.maxValue = 1f;

            GameObject bgBarra = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgBarra.transform.SetParent(objBarraProg.transform, false);
            ConstructorUI.Estirar(bgBarra.GetComponent<RectTransform>());
            bgBarra.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.26f, 1f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(objBarraProg.transform, false);
            ConstructorUI.Estirar(fillArea.GetComponent<RectTransform>());

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            ConstructorUI.Estirar(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.22f, 0.75f, 0.45f, 1f);
            sldProg.fillRect = fill.GetComponent<RectTransform>();
            script.barraProgreso = sldProg;

            // Texto porcentaje
            GameObject objTxtProg = new GameObject("TextoProgreso", typeof(RectTransform), typeof(Text));
            objTxtProg.transform.SetParent(panelHerramientas.transform, false);
            RectTransform rtTP = objTxtProg.GetComponent<RectTransform>();
            rtTP.anchorMin = new Vector2(0.1f, 0.31f);
            rtTP.anchorMax = new Vector2(0.9f, 0.31f);
            rtTP.pivot = new Vector2(0.5f, 0.5f);
            rtTP.sizeDelta = new Vector2(0, 24);
            Text txtProg = objTxtProg.GetComponent<Text>();
            txtProg.font = ConstructorUI.Fuente;
            txtProg.fontSize = 15;
            txtProg.fontStyle = FontStyle.Bold;
            txtProg.alignment = TextAnchor.MiddleCenter;
            txtProg.text = "0%";
            txtProg.color = Color.white;
            script.textoProgreso = txtProg;

            // Texto instrucciones
            GameObject objInst = new GameObject("TextoInstrucciones", typeof(RectTransform), typeof(Text));
            objInst.transform.SetParent(panelHerramientas.transform, false);
            RectTransform rtInst = objInst.GetComponent<RectTransform>();
            rtInst.anchorMin = new Vector2(0.08f, 0.16f);
            rtInst.anchorMax = new Vector2(0.92f, 0.28f);
            rtInst.offsetMin = Vector2.zero;
            rtInst.offsetMax = Vector2.zero;
            Text txtInst = objInst.GetComponent<Text>();
            txtInst.font = ConstructorUI.Fuente;
            txtInst.fontSize = 13;
            txtInst.alignment = TextAnchor.MiddleCenter;
            txtInst.color = new Color(0.9f, 0.92f, 0.96f, 1f);
            txtInst.text = "Mantén presionado el clic y pasa la Birome sobre la hoja para redactar el informe.";
            script.textoInstrucciones = txtInst;

            // Botón Salir
            GameObject objBtnSalir = new GameObject("BotonSalir", typeof(RectTransform), typeof(Image), typeof(Button));
            objBtnSalir.transform.SetParent(panelHerramientas.transform, false);
            RectTransform rtSalir = objBtnSalir.GetComponent<RectTransform>();
            rtSalir.anchorMin = new Vector2(0.1f, 0.04f);
            rtSalir.anchorMax = new Vector2(0.9f, 0.04f);
            rtSalir.pivot = new Vector2(0.5f, 0);
            rtSalir.sizeDelta = new Vector2(0, 42);

            Image imgSalir = objBtnSalir.GetComponent<Image>();
            imgSalir.color = new Color(0.72f, 0.22f, 0.22f, 0.95f);
            Button btnSalir = objBtnSalir.GetComponent<Button>();
            script.botonSalir = btnSalir;

            GameObject objTxtSalir = new GameObject("TextoSalir", typeof(RectTransform), typeof(Text));
            objTxtSalir.transform.SetParent(objBtnSalir.transform, false);
            ConstructorUI.Estirar(objTxtSalir.GetComponent<RectTransform>());
            Text txtSalir = objTxtSalir.GetComponent<Text>();
            txtSalir.font = ConstructorUI.Fuente;
            txtSalir.fontSize = 15;
            txtSalir.fontStyle = FontStyle.Bold;
            txtSalir.alignment = TextAnchor.MiddleCenter;
            txtSalir.color = Color.white;
            txtSalir.text = "✕ Salir (Esc)";

            // 4. Cursor flotante
            GameObject objCursor = new GameObject("CursorHerramienta", typeof(RectTransform), typeof(Image));
            objCursor.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtCursor = objCursor.GetComponent<RectTransform>();
            rtCursor.sizeDelta = new Vector2(40, 40);
            rtCursor.pivot = new Vector2(0f, 1f);
            Image imgCursor = objCursor.GetComponent<Image>();
            imgCursor.sprite = spBirome;
            imgCursor.raycastTarget = false;
            objCursor.SetActive(false);
            script.cursorHerramienta = imgCursor;
            script.rectCursor = rtCursor;

            // Guardar prefab
            string rutaPrefab = CarpetaPrefabsInteractuables + "/PanelMinijuegoRedactarDocumento.prefab";
            GameObject prefabGuardado = PrefabUtility.SaveAsPrefabAsset(panelRaiz, rutaPrefab);
            Object.DestroyImmediate(panelRaiz);

            return prefabGuardado;
        }

        private static GameObject CrearBotonHerramientaUI(Transform padre, string nombre, string texto, Sprite icono, Vector2 posAnchored, out Image marco)
        {
            GameObject botonObj = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
            botonObj.transform.SetParent(padre, false);
            RectTransform rt = botonObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 1);
            rt.anchorMax = new Vector2(0.92f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, 62);
            rt.anchoredPosition = posAnchored;

            Image img = botonObj.GetComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.33f, 1f);

            // Marco de selección
            GameObject objMarco = new GameObject("MarcoSeleccion", typeof(RectTransform), typeof(Image));
            objMarco.transform.SetParent(botonObj.transform, false);
            RectTransform rtMarco = objMarco.GetComponent<RectTransform>();
            EstirarConMargenes(rtMarco, -3, -3, -3, -3);
            marco = objMarco.GetComponent<Image>();
            marco.color = new Color(0.35f, 0.82f, 1f, 0.75f);
            marco.enabled = false;

            // Icono
            if (icono != null)
            {
                GameObject objIcono = new GameObject("Icono", typeof(RectTransform), typeof(Image));
                objIcono.transform.SetParent(botonObj.transform, false);
                RectTransform rtIco = objIcono.GetComponent<RectTransform>();
                rtIco.anchorMin = new Vector2(0, 0.5f);
                rtIco.anchorMax = new Vector2(0, 0.5f);
                rtIco.pivot = new Vector2(0.5f, 0.5f);
                rtIco.sizeDelta = new Vector2(36, 36);
                rtIco.anchoredPosition = new Vector2(30, 0);

                Image imgIco = objIcono.GetComponent<Image>();
                imgIco.sprite = icono;
                imgIco.preserveAspect = true;
            }

            // Texto
            GameObject objTxt = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            objTxt.transform.SetParent(botonObj.transform, false);
            RectTransform rtTxt = objTxt.GetComponent<RectTransform>();
            EstirarConMargenes(rtTxt, 58, 4, 10, 4);

            Text txt = objTxt.GetComponent<Text>();
            txt.font = ConstructorUI.Fuente;
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.white;
            txt.text = texto;

            return botonObj;
        }

        private static void EstirarConMargenes(RectTransform rect, float izquierda, float abajo, float derecha, float arriba)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(izquierda, abajo);
            rect.offsetMax = new Vector2(-derecha, -arriba);
        }

        private static void GenerarPrefabInteractuable(GameObject prefabPanelUI)
        {
            GameObject hojaOriginal = AssetDatabase.LoadAssetAtPath<GameObject>(RutaHojaFbx);
            if (hojaOriginal == null)
            {
                Debug.LogWarning("[GeneradorMinijuegoRedactarDocumento] No se encontró " + RutaHojaFbx);
                return;
            }

            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(hojaOriginal);
            instancia.name = "HojaInteractuable";

            if (instancia.GetComponent<Collider>() == null)
            {
                BoxCollider col = instancia.AddComponent<BoxCollider>();
                col.size = new Vector3(0.25f, 0.02f, 0.35f);
            }

            if (instancia.GetComponent<ResaltadoInteractuable>() == null)
            {
                instancia.AddComponent<ResaltadoInteractuable>();
            }

            HojaInteractuable hoja = instancia.GetComponent<HojaInteractuable>();
            if (hoja == null)
            {
                hoja = instancia.AddComponent<HojaInteractuable>();
            }

            hoja.estado = HojaInteractuable.EstadoSwitch.Activado;

            if (prefabPanelUI != null)
            {
                hoja.minijuego = prefabPanelUI.GetComponent<MinijuegoRedactarDocumento>();
            }

            string rutaDestino = CarpetaPrefabsInteractuables + "/HojaInteractuable.prefab";
            PrefabUtility.SaveAsPrefabAsset(instancia, rutaDestino);
            Object.DestroyImmediate(instancia);
        }
    }
}
