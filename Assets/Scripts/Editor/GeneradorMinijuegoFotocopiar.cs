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
        private const string RutaIconoInforme = CarpetaSpritesMinijuegos + "/documento_importante_24px.png";
        private const string RutaIconoChiqui = CarpetaSpritesMinijuegos + "/chiqui tapia formato hoja.png";
        private const string RutaAudioImpresora = "Assets/Resources/Audio/Objetos/Impresora2.mp3";

        [MenuItem("Solta La Pala/Generar Minijuego Fotocopiar (Prefabs y Canvas UI Cuadrado)")]
        public static void GenerarTodo()
        {
            AsegurarCarpetas();
            ConfigurarSprites();

            GameObject prefabPanelUI = GenerarPrefabPanelUICuadrado();
            GenerarPrefabInteractuable(prefabPanelUI);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GeneradorMinijuegoFotocopiar] Prefabs y Canvas UI Cuadrado generados en " + CarpetaPrefabsInteractuables);
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

            // 1. Buscar o crear Canvas
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
                impExistente = instImp.GetComponent<ImpresoraInteractuable>();
                Undo.RegisterCreatedObjectUndo(instImp, "Instanciar Impresora Interactuable");
                Selection.activeGameObject = instImp;
            }

            // 4. Conectar referencias mutuas
            if (scriptPanel != null && impExistente != null)
            {
                scriptPanel.impresora = impExistente;
                scriptPanel.ActualizarPantallaSegunEstadoImpresora();
            }

            // 5. Vincular a las computadoras en modo Fotocopiar
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
                "¡Minijuego de Fotocopiar configurado!\n\n" +
                "Flujo:\n" +
                "1. Computadora -> Muestra 'Pon el documento en la impresora'.\n" +
                "2. Impresora -> Presiona [E] para poner el documento (Informes o Foto del Chiqui).\n" +
                "3. Computadora -> Presiona IMPRIMIR.\n" +
                "4. Impresora -> Expulsa la Carpeta lista para recoger.", 
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

        private static GameObject GenerarPrefabPanelUICuadrado()
        {
            Sprite spriteFondo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaFondoComputadora);
            Sprite spriteInforme = AssetDatabase.LoadAssetAtPath<Sprite>(RutaIconoInforme);
            Sprite spriteChiqui = AssetDatabase.LoadAssetAtPath<Sprite>(RutaIconoChiqui);
            AudioClip clipAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaAudioImpresora);

            // 1. Raíz del Panel: CUADRADO (700 x 700)
            GameObject panelRaiz = new GameObject("PanelMinijuegoFotocopiar", typeof(RectTransform));
            panelRaiz.layer = LayerMask.NameToLayer("UI");

            RectTransform rtRaiz = panelRaiz.GetComponent<RectTransform>();
            rtRaiz.sizeDelta = new Vector2(700f, 700f);
            rtRaiz.anchorMin = new Vector2(0.5f, 0.5f);
            rtRaiz.anchorMax = new Vector2(0.5f, 0.5f);
            rtRaiz.pivot = new Vector2(0.5f, 0.5f);

            MinijuegoFotocopiar script = panelRaiz.AddComponent<MinijuegoFotocopiar>();
            script.panelRaiz = panelRaiz;
            script.spriteFondoComputadora = spriteFondo;
            script.spriteIconoInforme = spriteInforme;
            script.spriteIconoChiqui = spriteChiqui;
            script.clipImpresora = clipAudio;

            // 2. Fondo Cuadrado
            GameObject objFondo = new GameObject("FondoComputadora", typeof(RectTransform), typeof(Image));
            objFondo.transform.SetParent(panelRaiz.transform, false);
            ConstructorUI.Estirar(objFondo.GetComponent<RectTransform>());
            Image imgFondo = objFondo.GetComponent<Image>();
            imgFondo.sprite = spriteFondo;
            imgFondo.color = Color.white;
            script.imagenFondo = imgFondo;

            // 3. Botón Salir
            GameObject objBtnSalir = new GameObject("BotonSalir", typeof(RectTransform), typeof(Image), typeof(Button));
            objBtnSalir.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtSalir = objBtnSalir.GetComponent<RectTransform>();
            rtSalir.anchorMin = new Vector2(1f, 1f);
            rtSalir.anchorMax = new Vector2(1f, 1f);
            rtSalir.pivot = new Vector2(1f, 1f);
            rtSalir.anchoredPosition = new Vector2(-15f, -15f);
            rtSalir.sizeDelta = new Vector2(90f, 34f);
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
            tSalir.fontSize = 14;
            tSalir.alignment = TextAnchor.MiddleCenter;
            tSalir.text = "✕ Salir";
            tSalir.color = Color.white;

            // 4. Título
            GameObject objTit = new GameObject("Titulo", typeof(RectTransform), typeof(Text));
            objTit.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtTit = objTit.GetComponent<RectTransform>();
            rtTit.anchorMin = new Vector2(0.5f, 1f);
            rtTit.anchorMax = new Vector2(0.5f, 1f);
            rtTit.pivot = new Vector2(0.5f, 1f);
            rtTit.anchoredPosition = new Vector2(0f, -20f);
            rtTit.sizeDelta = new Vector2(500f, 32f);
            Text txtTit = objTit.GetComponent<Text>();
            txtTit.text = "<b>TERMINAL DE FOTOCOPIADO</b>";
            txtTit.font = ConstructorUI.Fuente;
            txtTit.fontSize = 20;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.color = Color.white;
            script.textoTituloVentana = txtTit;

            // 5. Marco Central Cuadrado para Estado
            GameObject panelCentro = new GameObject("PanelCentro", typeof(RectTransform), typeof(Image));
            panelCentro.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtCentro = panelCentro.GetComponent<RectTransform>();
            rtCentro.anchorMin = new Vector2(0.5f, 0.5f);
            rtCentro.anchorMax = new Vector2(0.5f, 0.5f);
            rtCentro.pivot = new Vector2(0.5f, 0.5f);
            rtCentro.anchoredPosition = new Vector2(0f, 25f);
            rtCentro.sizeDelta = new Vector2(580f, 440f);
            panelCentro.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.19f, 0.90f);

            // Icono Central
            GameObject objIcono = new GameObject("IconoEstado", typeof(RectTransform), typeof(Image));
            objIcono.transform.SetParent(panelCentro.transform, false);
            RectTransform rtIcono = objIcono.GetComponent<RectTransform>();
            rtIcono.anchorMin = new Vector2(0.5f, 1f);
            rtIcono.anchorMax = new Vector2(0.5f, 1f);
            rtIcono.pivot = new Vector2(0.5f, 1f);
            rtIcono.anchoredPosition = new Vector2(0f, -25f);
            rtIcono.sizeDelta = new Vector2(110f, 110f);
            Image imgIcono = objIcono.GetComponent<Image>();
            imgIcono.preserveAspect = true;
            imgIcono.sprite = spriteInforme;
            script.imagenIconoEstado = imgIcono;

            // Mensaje Principal
            GameObject objMsgPrinc = new GameObject("MensajePrincipal", typeof(RectTransform), typeof(Text));
            objMsgPrinc.transform.SetParent(panelCentro.transform, false);
            RectTransform rtMsgPrinc = objMsgPrinc.GetComponent<RectTransform>();
            rtMsgPrinc.anchorMin = new Vector2(0.5f, 1f);
            rtMsgPrinc.anchorMax = new Vector2(0.5f, 1f);
            rtMsgPrinc.pivot = new Vector2(0.5f, 1f);
            rtMsgPrinc.anchoredPosition = new Vector2(0f, -150f);
            rtMsgPrinc.sizeDelta = new Vector2(520f, 35f);
            Text tMsgPrinc = objMsgPrinc.GetComponent<Text>();
            tMsgPrinc.font = ConstructorUI.Fuente;
            tMsgPrinc.fontSize = 17;
            tMsgPrinc.alignment = TextAnchor.MiddleCenter;
            tMsgPrinc.color = Color.white;
            tMsgPrinc.text = "<color=#FFCC00><b>⚠ BANDEJA DE ESCÁNER VACÍA</b></color>";
            script.textoMensajePrincipal = tMsgPrinc;

            // Submensaje Instrucción
            GameObject objSub = new GameObject("SubmensajeInstruccion", typeof(RectTransform), typeof(Text));
            objSub.transform.SetParent(panelCentro.transform, false);
            RectTransform rtSub = objSub.GetComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 1f);
            rtSub.anchorMax = new Vector2(0.5f, 1f);
            rtSub.pivot = new Vector2(0.5f, 1f);
            rtSub.anchoredPosition = new Vector2(0f, -195f);
            rtSub.sizeDelta = new Vector2(520f, 140f);
            Text tSub = objSub.GetComponent<Text>();
            tSub.font = ConstructorUI.Fuente;
            tSub.fontSize = 14;
            tSub.alignment = TextAnchor.MiddleCenter;
            tSub.color = new Color(0.85f, 0.90f, 1f, 0.95f);
            tSub.text = "No se detectó ningún documento en la impresora.\n\n" +
                        "<size=17><color=#55CCFF><b>➔ Pon el documento en la impresora</b></color></size>\n\n" +
                        "<i>(Ve a la impresora y presiona [E] con los Informes o la Foto de Chiqui).</i>";
            script.textoSubmensajeInstruccion = tSub;

            // Barra Progreso
            GameObject objBarra = new GameObject("BarraProgreso", typeof(RectTransform), typeof(Slider));
            objBarra.transform.SetParent(panelCentro.transform, false);
            RectTransform rtProg = objBarra.GetComponent<RectTransform>();
            rtProg.anchorMin = new Vector2(0.5f, 0f);
            rtProg.anchorMax = new Vector2(0.5f, 0f);
            rtProg.pivot = new Vector2(0.5f, 0f);
            rtProg.anchoredPosition = new Vector2(0f, 25f);
            rtProg.sizeDelta = new Vector2(500f, 16f);

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
            script.barraProgreso = slider;

            // Botón Inferior: IMPRIMIR
            GameObject objBtnImp = new GameObject("BotonImprimir", typeof(RectTransform), typeof(Image), typeof(Button));
            objBtnImp.transform.SetParent(panelRaiz.transform, false);
            RectTransform rtBtnImp = objBtnImp.GetComponent<RectTransform>();
            rtBtnImp.anchorMin = new Vector2(0.5f, 0f);
            rtBtnImp.anchorMax = new Vector2(0.5f, 0f);
            rtBtnImp.pivot = new Vector2(0.5f, 0f);
            rtBtnImp.anchoredPosition = new Vector2(0f, 25f);
            rtBtnImp.sizeDelta = new Vector2(340f, 52f);
            objBtnImp.GetComponent<Image>().color = new Color(0.18f, 0.52f, 0.32f, 1f);

            Button btnImp = objBtnImp.GetComponent<Button>();
            ColorBlock cbImp = ConstructorUI.ColoresBoton();
            cbImp.normalColor = Color.white;
            cbImp.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cbImp.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            cbImp.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            btnImp.colors = cbImp;
            btnImp.interactable = false;
            script.botonImprimir = btnImp;

            GameObject objTxtBtn = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            objTxtBtn.transform.SetParent(objBtnImp.transform, false);
            ConstructorUI.Estirar(objTxtBtn.GetComponent<RectTransform>());
            Text tBtn = objTxtBtn.GetComponent<Text>();
            tBtn.font = ConstructorUI.Fuente;
            tBtn.fontSize = 17;
            tBtn.fontStyle = FontStyle.Bold;
            tBtn.alignment = TextAnchor.MiddleCenter;
            tBtn.color = Color.white;
            tBtn.text = "IMPRIMIR (Bandeja Vacía)";

            // AudioSource
            AudioSource audio = panelRaiz.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.loop = false;
            audio.clip = clipAudio;
            script.audioSource = audio;

            string rutaPrefabPanel = CarpetaPrefabsInteractuables + "/PanelMinijuegoFotocopiar.prefab";
            GameObject prefabGuardado = PrefabUtility.SaveAsPrefabAsset(panelRaiz, rutaPrefabPanel);
            Object.DestroyImmediate(panelRaiz);

            return prefabGuardado;
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

            string rutaDestino = CarpetaPrefabsInteractuables + "/ImpresoraInteractuable.prefab";
            PrefabUtility.SaveAsPrefabAsset(instancia, rutaDestino);
            Object.DestroyImmediate(instancia);
        }
    }
}
