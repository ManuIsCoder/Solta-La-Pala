using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SoltaLaPala.Minijuegos;
using SoltaLaPala.Interaction;
using SoltaLaPala.Menus;

namespace SoltaLaPala.Editor
{
    public static class GeneradorMinijuegoFotocopiar
    {
        private const string CarpetaPrefabsInteractuables = "Assets/Prefabs/Interactuable minijuego";
        private const string CarpetaSpritesMinijuegos = "Assets/Sprites/Minijuegos";
        private const string RutaImpresoraBase = "Assets/Prefabs/con sonido/Impresora.prefab";

        private const string RutaFondoComputadora = CarpetaSpritesMinijuegos + "/fondo computadora.png";
        private const string RutaImpresoraSprite = CarpetaSpritesMinijuegos + "/impresora-sinfondo.png";
        private const string RutaIconoInforme = CarpetaSpritesMinijuegos + "/documento_importante_24px.png";
        private const string RutaIconoChiqui = CarpetaSpritesMinijuegos + "/chiqui tapia formato hoja.png";
        private const string RutaAudioImpresora = "Assets/Resources/Audio/Objetos/Impresora2.mp3";

        [MenuItem("Solta La Pala/Generar Minijuego Fotocopiar (Prefabs y Canvas UI)")]
        public static void GenerarTodo()
        {
            AsegurarCarpetas();
            ConfigurarSprites();

            GameObject prefabPanelUI = GenerarPrefabPanelUI();
            GenerarPrefabInteractuable(prefabPanelUI);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GeneradorMinijuegoFotocopiar] Prefabs y Canvas UI generados en " + CarpetaPrefabsInteractuables);
        }

        [MenuItem("Solta La Pala/Colocar Minijuego Fotocopiar en Escena Actual")]
        public static void ColocarEnEscenaActual()
        {
            GenerarTodo();

            string rutaPrefabPanel = CarpetaPrefabsInteractuables + "/PanelMinijuegoFotocopiar.prefab";
            string rutaPrefabInteractuable = CarpetaPrefabsInteractuables + "/ImpresoraInteractuable.prefab";

            GameObject prefabPanel = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefabPanel);
            GameObject prefabInteractuable = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefabInteractuable);

            if (prefabPanel == null)
            {
                EditorUtility.DisplayDialog("Error", "No se encontró el prefab del Panel de Fotocopiar.", "Aceptar");
                return;
            }

            // 1. Buscar o crear Canvas de minijuegos
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = ConstructorUI.CrearCanvas("Canvasminijuegos", null, 10);
            }

            // 2. Instanciar o conectar el panel en el Canvas
            MinijuegoFotocopiar panelExistente = Object.FindObjectOfType<MinijuegoFotocopiar>(true);
            MinijuegoFotocopiar scriptPanel;
            if (panelExistente == null)
            {
                GameObject instPanel = (GameObject)PrefabUtility.InstantiatePrefab(prefabPanel, canvas.transform);
                instPanel.SetActive(false);
                scriptPanel = instPanel.GetComponent<MinijuegoFotocopiar>();
                Undo.RegisterCreatedObjectUndo(instPanel, "Instanciar Panel Minijuego Fotocopiar");
            }
            else
            {
                scriptPanel = panelExistente;
            }

            // 3. Buscar impresora existente o instanciarla
            ImpresoraInteractuable impExistente = Object.FindObjectOfType<ImpresoraInteractuable>();
            if (impExistente == null && prefabInteractuable != null)
            {
                GameObject instImp = (GameObject)PrefabUtility.InstantiatePrefab(prefabInteractuable);
                instImp.transform.position = new Vector3(0f, 1f, 0f);
                ImpresoraInteractuable imp = instImp.GetComponent<ImpresoraInteractuable>();
                if (imp != null)
                {
                    imp.minijuego = scriptPanel;
                }
                Undo.RegisterCreatedObjectUndo(instImp, "Instanciar Impresora Interactuable");
                Selection.activeGameObject = instImp;
            }
            else if (impExistente != null)
            {
                impExistente.minijuego = scriptPanel;
                Selection.activeGameObject = impExistente.gameObject;
            }

            // 4. Si hay computadoras en la escena con modo Fotocopiar, conectarles la referencia
            ComputadoraInteractuable[] comps = Object.FindObjectsOfType<ComputadoraInteractuable>();
            foreach (var comp in comps)
            {
                if (comp.modoJuego == ComputadoraInteractuable.ModoJuegoComputadora.Fotocopiar)
                {
                    comp.minijuegoFotocopiar = scriptPanel;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Éxito", 
                "¡Minijuego de Fotocopiar colocado en la escena actual!\n\n" +
                "- Canvas UI: PanelMinijuegoFotocopiar vinculado.\n" +
                "- Impresora interactuable lista con sonido y minijuego asignado.", 
                "Aceptar");
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
                RutaImpresoraSprite,
                RutaIconoInforme,
                RutaIconoChiqui
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

                if (!importador.alphaIsTransparency)
                {
                    importador.alphaIsTransparency = true;
                    necesitaReimport = true;
                }

                if (importador.mipmapEnabled)
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
            // Cargar Sprites
            Sprite spriteFondo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaFondoComputadora);
            Sprite spriteImpresora = AssetDatabase.LoadAssetAtPath<Sprite>(RutaImpresoraSprite);
            Sprite spriteInforme = AssetDatabase.LoadAssetAtPath<Sprite>(RutaIconoInforme);
            Sprite spriteChiqui = AssetDatabase.LoadAssetAtPath<Sprite>(RutaIconoChiqui);
            AudioClip clipAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaAudioImpresora);

            // 1. Raíz del Panel (reciclando la escala y estructura de la computadora)
            GameObject panelRaiz = new GameObject("PanelMinijuegoFotocopiar", typeof(RectTransform));
            panelRaiz.layer = LayerMask.NameToLayer("UI");

            RectTransform rtRaiz = panelRaiz.GetComponent<RectTransform>();
            rtRaiz.sizeDelta = new Vector2(1150f, 748f);
            rtRaiz.anchorMin = new Vector2(0.5f, 0.5f);
            rtRaiz.anchorMax = new Vector2(0.5f, 0.5f);
            rtRaiz.pivot = new Vector2(0.5f, 0.5f);

            MinijuegoFotocopiar script = panelRaiz.AddComponent<MinijuegoFotocopiar>();
            script.panelRaiz = panelRaiz;
            script.spriteFondoComputadora = spriteFondo;
            script.spriteImpresora = spriteImpresora;
            script.spriteIconoInforme = spriteInforme;
            script.spriteIconoChiqui = spriteChiqui;
            script.clipImpresora = clipAudio;

            // 2. Fondo de la Computadora
            GameObject objFondo = new GameObject("FondoComputadora", typeof(RectTransform), typeof(Image));
            objFondo.transform.SetParent(panelRaiz.transform, false);
            ConstructorUI.Estirar(objFondo.GetComponent<RectTransform>());
            Image imgFondo = objFondo.GetComponent<Image>();
            imgFondo.sprite = spriteFondo;
            imgFondo.color = Color.white;
            script.imagenFondo = imgFondo;

            // 3. Botón Salir superior
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
            script.botonSalir = btnSalir;

            GameObject objTxtSalir = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            objTxtSalir.transform.SetParent(objBtnSalir.transform, false);
            ConstructorUI.Estirar(objTxtSalir.GetComponent<RectTransform>());
            Text tSalir = objTxtSalir.GetComponent<Text>();
            tSalir.font = ConstructorUI.Fuente;
            tSalir.fontSize = 15;
            tSalir.alignment = TextAnchor.MiddleCenter;
            tSalir.text = "✕ Salir";
            tSalir.color = Color.white;

            // 4. Título Principal
            GameObject objTit = new GameObject("TituloMinijuego", typeof(RectTransform), typeof(Text));
            objTit.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtTit = objTit.GetComponent<RectTransform>();
            rtTit.anchorMin = new Vector2(0.5f, 1f);
            rtTit.anchorMax = new Vector2(0.5f, 1f);
            rtTit.pivot = new Vector2(0.5f, 1f);
            rtTit.anchoredPosition = new Vector2(0f, -28f);
            rtTit.sizeDelta = new Vector2(700f, 40f);
            Text txtTit = objTit.GetComponent<Text>();
            txtTit.text = "<b>SISTEMA DE FOTOCOPIADO E IMPRESIÓN</b>";
            txtTit.font = ConstructorUI.Fuente;
            txtTit.fontSize = 22;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.color = Color.white;
            script.textoInstrucciones = txtTit;

            // 5. Subtítulo
            GameObject objSub = new GameObject("Subtitulo", typeof(RectTransform), typeof(Text));
            objSub.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtSub = objSub.GetComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 1f);
            rtSub.anchorMax = new Vector2(0.5f, 1f);
            rtSub.pivot = new Vector2(0.5f, 1f);
            rtSub.anchoredPosition = new Vector2(0f, -65f);
            rtSub.sizeDelta = new Vector2(800f, 30f);
            Text txtSub = objSub.GetComponent<Text>();
            txtSub.text = "Selecciona el documento en la computadora y presiona <b>FOTOCOPIAR</b>.";
            txtSub.font = ConstructorUI.Fuente;
            txtSub.fontSize = 15;
            txtSub.alignment = TextAnchor.MiddleCenter;
            txtSub.color = new Color(0.8f, 0.85f, 0.95f, 0.9f);

            // ==========================================
            // LADO IZQUIERDO: SELECCIÓN EN PC
            // ==========================================
            GameObject panelArchivos = new GameObject("PanelArchivosPC", typeof(RectTransform), typeof(Image));
            panelArchivos.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtPnlArch = panelArchivos.GetComponent<RectTransform>();
            rtPnlArch.anchorMin = new Vector2(0f, 0.5f);
            rtPnlArch.anchorMax = new Vector2(0f, 0.5f);
            rtPnlArch.pivot = new Vector2(0f, 0.5f);
            rtPnlArch.anchoredPosition = new Vector2(50f, 30f);
            rtPnlArch.sizeDelta = new Vector2(440f, 480f);
            panelArchivos.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.24f, 0.85f);

            // Título PC
            GameObject objTxtPC = new GameObject("TituloPC", typeof(RectTransform), typeof(Text));
            objTxtPC.transform.SetParent(panelArchivos.transform, false);
            RectTransform rtTxtPC = objTxtPC.GetComponent<RectTransform>();
            rtTxtPC.anchorMin = new Vector2(0.5f, 1f);
            rtTxtPC.anchorMax = new Vector2(0.5f, 1f);
            rtTxtPC.pivot = new Vector2(0.5f, 1f);
            rtTxtPC.anchoredPosition = new Vector2(0f, -15f);
            rtTxtPC.sizeDelta = new Vector2(400f, 30f);
            Text tPC = objTxtPC.GetComponent<Text>();
            tPC.text = "<b>📁 ARCHIVOS EN EL ESCRITORIO</b>";
            tPC.font = ConstructorUI.Fuente;
            tPC.fontSize = 16;
            tPC.alignment = TextAnchor.MiddleCenter;
            tPC.color = new Color(0.9f, 0.95f, 1f, 1f);

            // Tarjeta Informes
            Image marcoInf;
            script.botonSeleccionarInforme = CrearTarjeta(panelArchivos.transform, "BotonInforme",
                "Informes del Jefe (Oficial)", "Documento solicitado para fotocopiar",
                spriteInforme, new Vector2(0f, -65f), out marcoInf);
            script.marcoSeleccionInforme = marcoInf;

            // Tarjeta Chiqui
            Image marcoChiqui;
            script.botonSeleccionarChiqui = CrearTarjeta(panelArchivos.transform, "BotonChiqui",
                "Foto de Chiqui Tapia", "¡CUIDADO! Arruinará la tarea (Sabotaje)",
                spriteChiqui, new Vector2(0f, -225f), out marcoChiqui);
            script.marcoSeleccionChiqui = marcoChiqui;

            // ==========================================
            // LADO DERECHO: SALA DE IMPRESORA / ESCÁNER
            // ==========================================
            GameObject panelImpresora = new GameObject("PanelImpresora", typeof(RectTransform), typeof(Image));
            panelImpresora.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtPnlImp = panelImpresora.GetComponent<RectTransform>();
            rtPnlImp.anchorMin = new Vector2(1f, 0.5f);
            rtPnlImp.anchorMax = new Vector2(1f, 0.5f);
            rtPnlImp.pivot = new Vector2(1f, 0.5f);
            rtPnlImp.anchoredPosition = new Vector2(-50f, 30f);
            rtPnlImp.sizeDelta = new Vector2(560f, 480f);
            panelImpresora.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.19f, 0.85f);

            // Título Impresora
            GameObject objTxtImp = new GameObject("TituloImp", typeof(RectTransform), typeof(Text));
            objTxtImp.transform.SetParent(panelImpresora.transform, false);
            RectTransform rtTxtImp = objTxtImp.GetComponent<RectTransform>();
            rtTxtImp.anchorMin = new Vector2(0.5f, 1f);
            rtTxtImp.anchorMax = new Vector2(0.5f, 1f);
            rtTxtImp.pivot = new Vector2(0.5f, 1f);
            rtTxtImp.anchoredPosition = new Vector2(0f, -15f);
            rtTxtImp.sizeDelta = new Vector2(500f, 30f);
            Text tImp = objTxtImp.GetComponent<Text>();
            tImp.text = "<b>🖨 IMPRESORA / FOTOCOPIADORA OFICIAL</b>";
            tImp.font = ConstructorUI.Fuente;
            tImp.fontSize = 16;
            tImp.alignment = TextAnchor.MiddleCenter;
            tImp.color = new Color(0.9f, 0.95f, 1f, 1f);

            // Gráfico Impresora
            GameObject objImgImp = new GameObject("GraficoImpresora", typeof(RectTransform), typeof(Image));
            objImgImp.transform.SetParent(panelImpresora.transform, false);
            RectTransform rtImgImp = objImgImp.GetComponent<RectTransform>();
            rtImgImp.anchorMin = new Vector2(0.5f, 1f);
            rtImgImp.anchorMax = new Vector2(0.5f, 1f);
            rtImgImp.pivot = new Vector2(0.5f, 1f);
            rtImgImp.anchoredPosition = new Vector2(0f, -55f);
            rtImgImp.sizeDelta = new Vector2(240f, 190f);
            Image imgImp = objImgImp.GetComponent<Image>();
            imgImp.preserveAspect = true;
            imgImp.sprite = spriteImpresora;
            script.imagenImpresoraVisual = imgImp;

            // Bandeja de Escaneo
            GameObject objBandeja = new GameObject("BandejaEscaner", typeof(RectTransform), typeof(Image));
            objBandeja.transform.SetParent(panelImpresora.transform, false);
            RectTransform rtBandeja = objBandeja.GetComponent<RectTransform>();
            rtBandeja.anchorMin = new Vector2(0.5f, 1f);
            rtBandeja.anchorMax = new Vector2(0.5f, 1f);
            rtBandeja.pivot = new Vector2(0.5f, 1f);
            rtBandeja.anchoredPosition = new Vector2(0f, -255f);
            rtBandeja.sizeDelta = new Vector2(480f, 75f);
            objBandeja.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.95f);

            // Icono en bandeja
            GameObject objDocBandeja = new GameObject("IconoDocumentoBandeja", typeof(RectTransform), typeof(Image));
            objDocBandeja.transform.SetParent(objBandeja.transform, false);
            RectTransform rtDocBan = objDocBandeja.GetComponent<RectTransform>();
            rtDocBan.anchorMin = new Vector2(0f, 0.5f);
            rtDocBan.anchorMax = new Vector2(0f, 0.5f);
            rtDocBan.pivot = new Vector2(0f, 0.5f);
            rtDocBan.anchoredPosition = new Vector2(15f, 0f);
            rtDocBan.sizeDelta = new Vector2(55f, 55f);
            Image imgDocBan = objDocBandeja.GetComponent<Image>();
            imgDocBan.preserveAspect = true;
            script.imagenDocumentoEnBandeja = imgDocBan;

            // Texto en bandeja
            GameObject objTxtBan = new GameObject("TxtBandeja", typeof(RectTransform), typeof(Text));
            objTxtBan.transform.SetParent(objBandeja.transform, false);
            RectTransform rtTxtBan = objTxtBan.GetComponent<RectTransform>();
            rtTxtBan.anchorMin = new Vector2(0f, 0f);
            rtTxtBan.anchorMax = new Vector2(1f, 1f);
            rtTxtBan.offsetMin = new Vector2(85f, 5f);
            rtTxtBan.offsetMax = new Vector2(-15f, -5f);
            Text tBan = objTxtBan.GetComponent<Text>();
            tBan.font = ConstructorUI.Fuente;
            tBan.fontSize = 14;
            tBan.alignment = TextAnchor.MiddleLeft;
            tBan.text = "<color=#AAAAAA><i>Bandeja de escáner vacía. Selecciona un archivo.</i></color>";
            script.textoDocumentoEnBandeja = tBan;

            // Barra Progreso
            GameObject objBarra = new GameObject("BarraProgreso", typeof(RectTransform), typeof(Slider));
            objBarra.transform.SetParent(panelImpresora.transform, false);
            RectTransform rtProg = objBarra.GetComponent<RectTransform>();
            rtProg.anchorMin = new Vector2(0.5f, 1f);
            rtProg.anchorMax = new Vector2(0.5f, 1f);
            rtProg.pivot = new Vector2(0.5f, 1f);
            rtProg.anchoredPosition = new Vector2(0f, -340f);
            rtProg.sizeDelta = new Vector2(480f, 18f);

            Slider slider = objBarra.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;

            GameObject bgBarra = new GameObject("FondoBarra", typeof(RectTransform), typeof(Image));
            bgBarra.transform.SetParent(objBarra.transform, false);
            ConstructorUI.Estirar(bgBarra.GetComponent<RectTransform>());
            bgBarra.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 1f);

            GameObject areaRelleno = new GameObject("AreaRelleno", typeof(RectTransform));
            areaRelleno.transform.SetParent(objBarra.transform, false);
            ConstructorUI.Estirar(areaRelleno.GetComponent<RectTransform>());
            GameObject fillBarra = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillBarra.transform.SetParent(areaRelleno.transform, false);
            RectTransform rtFill = fillBarra.GetComponent<RectTransform>();
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = new Vector2(0f, 1f);
            rtFill.sizeDelta = Vector2.zero;
            fillBarra.GetComponent<Image>().color = new Color(0.25f, 0.75f, 0.45f, 1f);
            slider.fillRect = rtFill;
            script.barraProgresoImpresion = slider;

            // Texto Estado
            GameObject objTxtEst = new GameObject("TextoEstado", typeof(RectTransform), typeof(Text));
            objTxtEst.transform.SetParent(panelImpresora.transform, false);
            RectTransform rtTxtEst = objTxtEst.GetComponent<RectTransform>();
            rtTxtEst.anchorMin = new Vector2(0.5f, 1f);
            rtTxtEst.anchorMax = new Vector2(0.5f, 1f);
            rtTxtEst.pivot = new Vector2(0.5f, 1f);
            rtTxtEst.anchoredPosition = new Vector2(0f, -365f);
            rtTxtEst.sizeDelta = new Vector2(480f, 26f);
            Text tEst = objTxtEst.GetComponent<Text>();
            tEst.font = ConstructorUI.Fuente;
            tEst.fontSize = 14;
            tEst.alignment = TextAnchor.MiddleCenter;
            tEst.color = Color.white;
            tEst.text = "Selecciona un documento en la computadora para colocarlo en la impresora.";
            script.textoEstadoProceso = tEst;

            // Botón Fotocopiar
            GameObject objBtnFoto = new GameObject("BotonFotocopiar", typeof(RectTransform), typeof(Image), typeof(Button));
            objBtnFoto.transform.SetParent(panelImpresora.transform, false);
            RectTransform rtBtnFoto = objBtnFoto.GetComponent<RectTransform>();
            rtBtnFoto.anchorMin = new Vector2(0.5f, 0f);
            rtBtnFoto.anchorMax = new Vector2(0.5f, 0f);
            rtBtnFoto.pivot = new Vector2(0.5f, 0f);
            rtBtnFoto.anchoredPosition = new Vector2(0f, 20f);
            rtBtnFoto.sizeDelta = new Vector2(320f, 50f);
            objBtnFoto.GetComponent<Image>().color = new Color(0.18f, 0.52f, 0.32f, 1f);

            Button btnFoto = objBtnFoto.GetComponent<Button>();
            ColorBlock cbFoto = ConstructorUI.ColoresBoton();
            cbFoto.normalColor = Color.white;
            cbFoto.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cbFoto.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            cbFoto.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            btnFoto.colors = cbFoto;
            script.botonFotocopiar = btnFoto;

            GameObject objTxtBtnFoto = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            objTxtBtnFoto.transform.SetParent(objBtnFoto.transform, false);
            ConstructorUI.Estirar(objTxtBtnFoto.GetComponent<RectTransform>());
            Text tBtnFoto = objTxtBtnFoto.GetComponent<Text>();
            tBtnFoto.font = ConstructorUI.Fuente;
            tBtnFoto.fontSize = 18;
            tBtnFoto.fontStyle = FontStyle.Bold;
            tBtnFoto.alignment = TextAnchor.MiddleCenter;
            tBtnFoto.color = Color.white;
            tBtnFoto.text = "🖨 FOTOCOPIAR";

            // AudioSource
            AudioSource audio = panelRaiz.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.loop = false;
            audio.clip = clipAudio;
            script.audioSource = audio;

            // Guardar Prefab del Panel UI
            string rutaPrefabPanel = CarpetaPrefabsInteractuables + "/PanelMinijuegoFotocopiar.prefab";
            GameObject prefabGuardado = PrefabUtility.SaveAsPrefabAsset(panelRaiz, rutaPrefabPanel);
            Object.DestroyImmediate(panelRaiz);

            return prefabGuardado;
        }

        private static Button CrearTarjeta(Transform padre, string nombreObj, string titulo, string desc, Sprite icono, Vector2 pos, out Image marco)
        {
            GameObject tarjeta = new GameObject(nombreObj, typeof(RectTransform), typeof(Image), typeof(Button));
            tarjeta.transform.SetParent(padre, false);
            RectTransform rt = tarjeta.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400f, 140f);

            marco = tarjeta.GetComponent<Image>();
            marco.color = new Color(0.25f, 0.30f, 0.40f, 0.4f);

            Button btn = tarjeta.GetComponent<Button>();
            btn.colors = ConstructorUI.ColoresBoton();

            GameObject interior = new GameObject("Interior", typeof(RectTransform), typeof(Image));
            interior.transform.SetParent(tarjeta.transform, false);
            RectTransform rtInt = interior.GetComponent<RectTransform>();
            rtInt.anchorMin = Vector2.zero;
            rtInt.anchorMax = Vector2.one;
            rtInt.offsetMin = new Vector2(3f, 3f);
            rtInt.offsetMax = new Vector2(-3f, -3f);
            interior.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            GameObject objIcono = new GameObject("Icono", typeof(RectTransform), typeof(Image));
            objIcono.transform.SetParent(interior.transform, false);
            RectTransform rtIcono = objIcono.GetComponent<RectTransform>();
            rtIcono.anchorMin = new Vector2(0f, 0.5f);
            rtIcono.anchorMax = new Vector2(0f, 0.5f);
            rtIcono.pivot = new Vector2(0f, 0.5f);
            rtIcono.anchoredPosition = new Vector2(15f, 0f);
            rtIcono.sizeDelta = new Vector2(90f, 90f);
            Image imgIcono = objIcono.GetComponent<Image>();
            imgIcono.preserveAspect = true;
            imgIcono.sprite = icono;

            GameObject objTit = new GameObject("Titulo", typeof(RectTransform), typeof(Text));
            objTit.transform.SetParent(interior.transform, false);
            RectTransform rtTit = objTit.GetComponent<RectTransform>();
            rtTit.anchorMin = new Vector2(0f, 1f);
            rtTit.anchorMax = new Vector2(1f, 1f);
            rtTit.pivot = new Vector2(0f, 1f);
            rtTit.anchoredPosition = new Vector2(120f, -20f);
            rtTit.sizeDelta = new Vector2(-135f, 30f);
            Text tTit = objTit.GetComponent<Text>();
            tTit.text = $"<b>{titulo}</b>";
            tTit.font = ConstructorUI.Fuente;
            tTit.fontSize = 15;
            tTit.alignment = TextAnchor.UpperLeft;
            tTit.color = Color.white;

            GameObject objDesc = new GameObject("Desc", typeof(RectTransform), typeof(Text));
            objDesc.transform.SetParent(interior.transform, false);
            RectTransform rtDesc = objDesc.GetComponent<RectTransform>();
            rtDesc.anchorMin = new Vector2(0f, 0f);
            rtDesc.anchorMax = new Vector2(1f, 1f);
            rtDesc.offsetMin = new Vector2(120f, 35f);
            rtDesc.offsetMax = new Vector2(-15f, -50f);
            Text tDesc = objDesc.GetComponent<Text>();
            tDesc.text = desc;
            tDesc.font = ConstructorUI.Fuente;
            tDesc.fontSize = 12;
            tDesc.alignment = TextAnchor.UpperLeft;
            tDesc.color = new Color(0.75f, 0.8f, 0.9f, 0.9f);

            GameObject objAccion = new GameObject("IndicadorAccion", typeof(RectTransform), typeof(Text));
            objAccion.transform.SetParent(interior.transform, false);
            RectTransform rtAcc = objAccion.GetComponent<RectTransform>();
            rtAcc.anchorMin = new Vector2(0f, 0f);
            rtAcc.anchorMax = new Vector2(1f, 0f);
            rtAcc.pivot = new Vector2(0f, 0f);
            rtAcc.anchoredPosition = new Vector2(120f, 12f);
            rtAcc.sizeDelta = new Vector2(-135f, 22f);
            Text tAcc = objAccion.GetComponent<Text>();
            tAcc.text = "➔ <i>Clic para poner en impresora</i>";
            tAcc.font = ConstructorUI.Fuente;
            tAcc.fontSize = 12;
            tAcc.alignment = TextAnchor.MiddleLeft;
            tAcc.color = new Color(0.4f, 0.8f, 1f, 1f);

            return btn;
        }

        private static void GenerarPrefabInteractuable(GameObject prefabPanelUI)
        {
            GameObject impOriginal = AssetDatabase.LoadAssetAtPath<GameObject>(RutaImpresoraBase);
            if (impOriginal == null)
            {
                Debug.LogWarning("[GeneradorMinijuegoFotocopiar] No se encontró " + RutaImpresoraBase);
                return;
            }

            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(impOriginal);
            instancia.name = "ImpresoraInteractuable";

            if (instancia.GetComponent<ResaltadoInteractuable>() == null)
            {
                instancia.AddComponent<ResaltadoInteractuable>();
            }

            ImpresoraInteractuable imp = instancia.GetComponent<ImpresoraInteractuable>();
            if (imp == null)
            {
                imp = instancia.AddComponent<ImpresoraInteractuable>();
            }

            imp.estado = ImpresoraInteractuable.EstadoSwitch.Activado;

            if (prefabPanelUI != null)
            {
                imp.minijuego = prefabPanelUI.GetComponent<MinijuegoFotocopiar>();
            }

            string rutaDestino = CarpetaPrefabsInteractuables + "/ImpresoraInteractuable.prefab";
            PrefabUtility.SaveAsPrefabAsset(instancia, rutaDestino);
            Object.DestroyImmediate(instancia);
        }
    }
}
