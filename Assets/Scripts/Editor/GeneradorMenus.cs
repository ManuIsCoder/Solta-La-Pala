using System.Collections.Generic;
using SoltaLaPala.Menus;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.EditorTools
{
    // Genera los prefabs de los menus Principal, Pausa y Configuracion a partir
    // del arte de Monzon, con un Image por cada pieza (fondo, cada boton, cada
    // slider) y las referencias de cada menu ya enganchadas.
    //
    // Los prefabs van a Resources/Menus, desde donde GestorMenus los carga solo
    // en cualquier escena.
    //
    // Si un prefab ya existe se pregunta antes de regenerarlo, para no pisar
    // cambios hechos a mano.
    public static class GeneradorMenus
    {
        private const string Carpeta = "Assets/Resources/Menus";
        private const string CarpetaSpritesUI = "Assets/Resources/Sprites/UI";

        // Escala del arte respecto del canvas de 1920x1080: el arte de 720px queda
        // ocupando casi todo el alto de la pantalla.
        private const float EscalaArte = 1.4f;

        private static readonly Color ColorAzulOscuro = new Color(29f / 255f, 90f / 255f, 134f / 255f, 1f);
        private static readonly Color ColorAmarillo = new Color(252f / 255f, 226f / 255f, 63f / 255f, 1f);
        private static readonly Color ColorTextoArte = new Color(15f / 255f, 39f / 255f, 54f / 255f, 1f);

        private static readonly Color ColorOscurecer = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        private static readonly Color ColorFondo = new Color(0.12f, 0.12f, 0.17f, 0.98f);
        private static readonly Color ColorBoton = new Color(0.22f, 0.22f, 0.30f, 1f);
        private static readonly Color ColorSliderFondo = new Color(0.08f, 0.08f, 0.12f, 1f);
        private static readonly Color ColorAcento = new Color(0.95f, 0.75f, 0.25f, 1f);
        private static readonly Color ColorTexto = new Color(0.94f, 0.94f, 0.96f, 1f);

        private static Font Fuente => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        [MenuItem("Solta La Pala/Generar menus (Principal, Pausa, Configuracion)")]
        public static void Generar()
        {
            if (!AssetDatabase.IsValidFolder(Carpeta))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Menus");
            }

            ConfigurarSpritesUI();

            var creados = new List<string>();
            var existentes = new List<string>();

            GenerarSiFalta("MenuPrincipal", ConstruirPrincipal, creados, existentes);
            GenerarSiFalta("MenuPausa", ConstruirPausa, creados, existentes);
            GenerarSiFalta("MenuConfiguracion", ConstruirConfiguracion, creados, existentes);

            string mensaje = "";
            if (creados.Count > 0) mensaje += $"Generados: {string.Join(", ", creados)}\n";
            if (existentes.Count > 0) mensaje += $"No se tocaron: {string.Join(", ", existentes)}\n";
            mensaje += $"\nCarpeta: {Carpeta}\nGestorMenus los carga solo en todas las escenas.";
            EditorUtility.DisplayDialog("Menus", mensaje, "OK");
        }

        // Crea el prefab, o lo regenera si el usuario lo confirma.
        // No hace falta ponerlo en las escenas: GestorMenus lo carga de Resources.
        private static void GenerarSiFalta(string nombre, System.Func<GameObject> construir,
            List<string> creados, List<string> existentes)
        {
            string ruta = $"{Carpeta}/{nombre}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);

            bool regenerar = prefab == null || EditorUtility.DisplayDialog("Menus",
                $"Ya existe {ruta}.\n\nRegenerarlo? Se pierden los cambios que le hayas hecho a mano.",
                "Regenerar", "Dejarlo como esta");

            if (regenerar)
            {
                // Sobre la misma ruta: el prefab conserva su GUID y las instancias
                // que ya estan en las escenas se actualizan solas.
                GameObject temporal = construir();
                PrefabUtility.SaveAsPrefabAsset(temporal, ruta);
                Object.DestroyImmediate(temporal);
                creados.Add(nombre);
            }
            else
            {
                existentes.Add(nombre);
            }
        }

        // --- Menus ---

        // Sigue los mockups de Monzon: fondo de oficina de 1280x720 con el titulo
        // dibujado y dos pasos de botones sobre el mismo fondo:
        //   GrupoInicio:  Jugar, Opciones, Salir
        //   GrupoPartida: Nueva partida, Continuar partida (se abre con Jugar)
        private static GameObject ConstruirPrincipal()
        {
            GameObject raiz = CrearRaiz<MenuPrincipal>("MenuPrincipal", 100, Vector2.zero,
                out MenuPrincipal menu, out Transform fondo);

            // Fuera de la imagen (pantallas que no son 16:9) queda negro.
            raiz.GetComponentInChildren<Canvas>().transform.Find("Oscurecer").GetComponent<Image>().color = Color.black;

            Image imagenFondo = fondo.GetComponent<Image>();
            imagenFondo.sprite = SpriteUI("Menu principal/Inicio sin botones");
            imagenFondo.color = Color.white;
            Estirar(imagenFondo.rectTransform);

            // Cubre toda la pantalla sin deformarse.
            AspectRatioFitter ajuste = fondo.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            ajuste.aspectRatio = AnchoInicio / AltoInicio;

            Transform grupoInicio = CrearGrupo("GrupoInicio", fondo);
            menu.grupoInicio = grupoInicio.gameObject;
            menu.botonPrincipal = CrearBotonInicio("BotonJugar", grupoInicio, SpriteUI("Generales/Jugar"), 431f, 293f, 418f, 79f);
            menu.etiquetaPrincipal = menu.botonPrincipal.GetComponentInChildren<Text>(true);
            menu.botonConfiguracion = CrearBotonInicio("BotonOpciones", grupoInicio, SpriteUI("Generales/Opciones"), 431f, 400f, 418f, 79f);
            menu.botonSalir = CrearBotonInicio("BotonSalir", grupoInicio, SpriteUI("Generales/Salir"), 431f, 507f, 418f, 79f);

            Transform grupoPartida = CrearGrupo("GrupoPartida", fondo);
            menu.grupoPartida = grupoPartida.gameObject;
            menu.botonNuevaPartida = CrearBotonInicio("BotonNuevaPartida", grupoPartida,
                SpriteUI("Generales/Nueva partida"), 235f, 325f, 810f, 70f);
            menu.botonContinuarPartida = CrearBotonInicio("BotonContinuarPartida", grupoPartida,
                SpriteUI("Generales/Continuar partida (1)"), 235f, 444f, 810f, 70f);
            // Arranca oculto; MenuPrincipal lo muestra al pulsar Jugar.
            menu.grupoPartida.SetActive(false);

            return raiz;
        }

        // Sigue el mockup de Monzon: fondo de 927x720 con el titulo y los textos
        // dibujados, y cinco botones de icono en dos filas:
        //   Opciones, Reanudar, Salir
        //   Guardar, Reintentar
        // Las medidas estan en pixeles del fondo, desde arriba a la izquierda.
        // Ver resultados no esta en el diseno: su campo queda vacio.
        private static GameObject ConstruirPausa()
        {
            GameObject raiz = CrearRaiz<MenuPausa>("MenuPausa", 120, new Vector2(927f, 720f) * EscalaArte,
                out MenuPausa menu, out Transform fondo);

            Image imagenFondo = fondo.GetComponent<Image>();
            imagenFondo.sprite = SpriteUI("Pausa/Menu pausa sin botones");
            imagenFondo.color = Color.white;

            menu.botonConfiguracion = CrearBotonArte("BotonOpciones", fondo, SpriteUI("Generales/Ajustes"),
                79f, 125f, 206f, 207f);
            menu.botonContinuar = CrearBotonArte("BotonReanudar", fondo, SpriteUI("Generales/Renaudar botón (1)"),
                366f, 123f, 212f, 212f);
            menu.botonMenuPrincipal = CrearBotonArte("BotonSalir", fondo, SpriteUI("Generales/Volver boton (1)"),
                654f, 125f, 207f, 207f);

            menu.botonGuardar = CrearBotonArte("BotonGuardar", fondo, SpriteUI("Generales/guardar"),
                203f, 412f, 216f, 216f);
            menu.botonReiniciar = CrearBotonArte("BotonReintentar", fondo, SpriteUI("Generales/reiniciar"),
                532f, 413f, 215f, 215f);

            // Confirmacion de "Guardar", justo debajo del panel: dentro del arte
            // no queda lugar libre.
            menu.textoAviso = CrearTexto("Aviso", fondo, "", Mathf.RoundToInt(12f * EscalaArte), Vector2.zero, Vector2.zero);
            ColocarPx(menu.textoAviso.rectTransform, 0f, 722f, 927f, 20f);

            return raiz;
        }

        // Sigue el mockup de Monzon (Configuracion/config.png). Todas las medidas
        // estan en pixeles del arte de 720x720, medidas desde arriba a la izquierda.
        // Los textos fijos (titulos, nombres de acciones) ya vienen dibujados en el fondo.
        private static GameObject ConstruirConfiguracion()
        {
            GameObject raiz = CrearRaiz<MenuConfiguracion>("MenuConfiguracion", 140,
                new Vector2(720f, 720f) * EscalaArte, out MenuConfiguracion menu, out Transform fondo);

            Image imagenFondo = fondo.GetComponent<Image>();
            imagenFondo.sprite = SpriteConfig("config sin botones");
            imagenFondo.color = Color.white;

            // Audio y sensibilidad: un slider debajo de cada etiqueta dibujada.
            menu.sliderGeneral = CrearSliderArte("SliderVolumenGeneral", fondo, 201f);
            menu.sliderMusica = CrearSliderArte("SliderMusica", fondo, 261f);
            menu.sliderEfectos = CrearSliderArte("SliderEfectos", fondo, 321f);
            menu.sliderSensibilidad = CrearSliderArte("SliderSensibilidad", fondo, 426f);

            // Invertir Y y pantalla completa: el texto SI/NO viene en el sprite.
            // Alto con la proporcion del sprite (305x45) para que no se estire,
            // centrados en los huecos libres del fondo (438-504 y 530-598).
            const float altoSiNo = 298f * 45f / 305f;
            menu.spriteInvertirSi = SpriteConfig("invertir si");
            menu.spriteInvertirNo = SpriteConfig("invertir no");
            menu.botonInvertirY = CrearBotonArte("BotonInvertirY", fondo, menu.spriteInvertirNo,
                34f, 471f - altoSiNo / 2f, 298f, altoSiNo);

            menu.spritePantallaSi = SpriteConfig("pantalla comp si");
            menu.spritePantallaNo = SpriteConfig("pantalla comp no");
            menu.botonPantallaCompleta = CrearBotonArte("BotonPantallaCompleta", fondo, menu.spritePantallaNo,
                34f, 564f - altoSiNo / 2f, 298f, altoSiNo);

            // Avisos de la reasignacion de teclas, justo debajo del panel: dentro
            // del arte no queda lugar libre.
            menu.textoEstado = CrearTexto("Estado", fondo, "", Mathf.RoundToInt(12f * EscalaArte), Vector2.zero, Vector2.zero);
            ColocarPx(menu.textoEstado.rectTransform, 0f, 722f, 720f, 20f);
            menu.textoEstado.color = Color.white;

            // Una tecla por accion, alineadas con los nombres dibujados en el fondo.
            Sprite spriteTecla = SpriteConfig("tecla");
            float[] filasY = { 159f, 192f, 225f, 257f, 290f, 323f, 356f, 389f, 422f, 454f, 487f, 520f, 553f };
            AccionJuego[] acciones = ControlesJuego.TodasLasAcciones;

            menu.colorTextoTecla = ColorTextoArte;

            for (int i = 0; i < acciones.Length; i++)
            {
                // Si se agregan acciones nuevas siguen la misma separacion; el fondo
                // va a necesitar el nombre dibujado.
                float y = i < filasY.Length ? filasY[i] : filasY[filasY.Length - 1] + (i - filasY.Length + 1) * 32.8f;

                Button boton = CrearBotonArte($"Tecla{acciones[i]}", fondo, spriteTecla, 608f, y, 85f, 32f, "");
                Text textoTecla = boton.GetComponentInChildren<Text>(true);
                textoTecla.color = ColorTextoArte;
                textoTecla.resizeTextForBestFit = true;
                textoTecla.resizeTextMinSize = 8;
                textoTecla.resizeTextMaxSize = Mathf.RoundToInt(18f * EscalaArte);

                menu.filas.Add(new MenuConfiguracion.FilaControl { accion = acciones[i], boton = boton, textoTecla = textoTecla });
            }

            // El sprite de restaurar trae margen transparente (900x133, la parte
            // visible es 889x76 desde (5,28)): se agranda el rect para que la parte
            // visible calce con el mockup en (165, 598, 391x38).
            const float escalaRestaurarX = 391f / 889f;
            const float escalaRestaurarY = 38f / 76f;
            menu.botonRestaurar = CrearBotonArte("BotonRestaurar", fondo, SpriteConfig("restaurar valores"),
                165f - 5f * escalaRestaurarX, 598f - 28f * escalaRestaurarY,
                900f * escalaRestaurarX, 133f * escalaRestaurarY);

            menu.botonVolver = CrearBotonArte("BotonSalir", fondo, SpriteUI("Generales/Salir"),
                215f, 648f, 290f, 55f);

            return raiz;
        }

        // --- Piezas con arte ---

        private const float AnchoInicio = 1280f;
        private const float AltoInicio = 720f;

        // Boton del menu principal en pixeles del mockup de 1280x720.
        // Anclado por proporcion del fondo para que acompane cualquier resolucion.
        private static Button CrearBotonInicio(string nombre, Transform padre, Sprite sprite,
            float x, float y, float ancho, float alto)
        {
            Button boton = CrearBotonArte(nombre, padre, sprite, 0f, 0f, 0f, 0f);
            ColocarProporcional(boton.GetComponent<Image>().rectTransform, x, y, ancho, alto, AnchoInicio, AltoInicio);
            return boton;
        }

        // Contenedor que ocupa todo el fondo, para prender y apagar un paso de botones junto.
        private static Transform CrearGrupo(string nombre, Transform fondo)
        {
            GameObject grupo = new GameObject(nombre, typeof(RectTransform));
            grupo.transform.SetParent(fondo, false);
            Estirar((RectTransform)grupo.transform);
            return grupo.transform;
        }

        // Ubica un rect con anclas en proporcion al padre, a partir de pixeles
        // del arte medidos desde arriba a la izquierda.
        private static void ColocarProporcional(RectTransform rect, float x, float y, float ancho, float alto,
            float anchoArte, float altoArte)
        {
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchorMin = new Vector2(x / anchoArte, 1f - (y + alto) / altoArte);
            rect.anchorMax = new Vector2((x + ancho) / anchoArte, 1f - y / altoArte);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Ruta relativa a Resources/Sprites/UI, sin extension.
        private static Sprite SpriteUI(string ruta)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{CarpetaSpritesUI}/{ruta}.png");
            if (sprite == null)
            {
                Debug.LogWarning($"[GeneradorMenus] No se encontro el sprite '{ruta}' en {CarpetaSpritesUI}.");
            }
            return sprite;
        }

        private static Sprite SpriteConfig(string nombre)
        {
            return SpriteUI($"Configuracion/{nombre}");
        }

        // Boton con sprite colocado en pixeles del arte. Si no se pasa etiqueta
        // se apaga, porque el texto ya viene dibujado en el sprite.
        private static Button CrearBotonArte(string nombre, Transform padre, Sprite sprite,
            float x, float y, float ancho, float alto, string etiqueta = null)
        {
            Button boton = CrearBoton(nombre, padre, etiqueta ?? "", Vector2.zero, Vector2.zero);
            Image imagen = boton.GetComponent<Image>();
            ColocarPx(imagen.rectTransform, x, y, ancho, alto);

            if (sprite != null)
            {
                imagen.sprite = sprite;
                imagen.color = Color.white;
            }

            if (etiqueta == null)
            {
                boton.GetComponentInChildren<Text>().gameObject.SetActive(false);
            }

            return boton;
        }

        // Slider del ancho de la columna izquierda, a la altura indicada.
        // La barra no tiene arte todavia: se pinta con los colores del menu.
        // La manija usa el sprite "boton slider".
        private static Slider CrearSliderArte(string nombre, Transform padre, float y)
        {
            Slider slider = CrearSlider(nombre, padre, Vector2.zero, Vector2.zero);
            ColocarPx((RectTransform)slider.transform, 42f, y, 282f, 8f);

            slider.transform.Find("Fondo").GetComponent<Image>().color = ColorAzulOscuro;
            slider.fillRect.GetComponent<Image>().color = ColorAmarillo;

            Image manija = slider.handleRect.GetComponent<Image>();
            manija.sprite = SpriteConfig("boton slider");
            manija.preserveAspect = true;
            slider.handleRect.sizeDelta = new Vector2(16f, 24f) * EscalaArte;

            return slider;
        }

        // Coloca en pixeles del arte, con origen arriba a la izquierda del Fondo,
        // igual que se miden en el PNG.
        private static void ColocarPx(RectTransform rect, float x, float y, float ancho, float alto)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y) * EscalaArte;
            rect.sizeDelta = new Vector2(ancho, alto) * EscalaArte;
        }

        // Los PNG de UI tienen que importarse como Sprite para poder ir en un Image.
        // Point y sin compresion para que el pixel art no se vea borroso.
        private static void ConfigurarSpritesUI()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CarpetaSpritesUI }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(ruta) is TextureImporter importador))
                {
                    continue;
                }

                if (importador.textureType == TextureImporterType.Sprite
                    && importador.filterMode == FilterMode.Point)
                {
                    continue;
                }

                importador.textureType = TextureImporterType.Sprite;
                importador.spriteImportMode = SpriteImportMode.Single;
                importador.filterMode = FilterMode.Point;
                importador.mipmapEnabled = false;
                importador.textureCompression = TextureImporterCompression.Uncompressed;
                importador.SaveAndReimport();
            }
        }

        // --- Piezas ---

        // Objeto con el script del menu, su Canvas, el oscurecido de pantalla
        // completa y el fondo centrado donde van los botones.
        private static GameObject CrearRaiz<T>(string nombre, int ordenPantalla, Vector2 tamanoFondo,
            out T menu, out Transform fondo) where T : MenuBase
        {
            GameObject raiz = new GameObject(nombre);
            menu = raiz.AddComponent<T>();

            GameObject objetoCanvas = new GameObject("Canvas", typeof(RectTransform));
            objetoCanvas.transform.SetParent(raiz.transform, false);

            Canvas canvas = objetoCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ordenPantalla;

            CanvasScaler escalador = objetoCanvas.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;

            objetoCanvas.AddComponent<GraphicRaycaster>();

            menu.panel = objetoCanvas;

            // Ademas de oscurecer el juego bloquea los clicks a lo que hay detras.
            Image oscurecer = CrearImagen("Oscurecer", objetoCanvas.transform, ColorOscurecer);
            Estirar(oscurecer.rectTransform);

            // Aca va el sprite del menu sin botones.
            Image imagenFondo = CrearImagen("Fondo", objetoCanvas.transform, ColorFondo);
            RectTransform rectFondo = imagenFondo.rectTransform;
            rectFondo.anchorMin = rectFondo.anchorMax = rectFondo.pivot = new Vector2(0.5f, 0.5f);
            rectFondo.anchoredPosition = Vector2.zero;
            rectFondo.sizeDelta = tamanoFondo;

            fondo = imagenFondo.transform;
            return raiz;
        }

        // Boton con el sprite en su Image, la etiqueta y el Brillo que lo aclara
        // al pasar el mouse y al pulsar. Si el sprite ya trae el texto dibujado,
        // se apaga el hijo Etiqueta.
        private static Button CrearBoton(string nombre, Transform padre, string etiqueta, Vector2 posicion, Vector2 tamano)
        {
            Image imagen = CrearImagen(nombre, padre, ColorBoton);
            Colocar(imagen.rectTransform, posicion, tamano);

            // Recorta el Brillo a la forma del sprite.
            imagen.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            Text texto = CrearTexto("Etiqueta", imagen.transform, etiqueta, 26, Vector2.zero, Vector2.zero);
            Estirar(texto.rectTransform);

            // Ultimo hijo para que aclare tambien la etiqueta.
            Image brillo = CrearImagen("Brillo", imagen.transform, Color.white);
            Estirar(brillo.rectTransform);
            brillo.raycastTarget = false;

            BotonMenu boton = imagen.gameObject.AddComponent<BotonMenu>();
            boton.transition = Selectable.Transition.ColorTint;
            boton.targetGraphic = brillo;
            // El tinte de reposo es transparente: el Brillo no se ve hasta que hay interaccion.
            boton.colors = BotonMenu.ColoresBrillo();

            return boton;
        }

        // Slider con tres Image para sprites: Fondo, Relleno y Manija.
        private static Slider CrearSlider(string nombre, Transform padre, Vector2 posicion, Vector2 tamano)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            Colocar((RectTransform)objeto.transform, posicion, tamano);

            Image fondo = CrearImagen("Fondo", objeto.transform, ColorSliderFondo);
            Estirar(fondo.rectTransform);

            GameObject areaRelleno = new GameObject("AreaRelleno", typeof(RectTransform));
            areaRelleno.transform.SetParent(objeto.transform, false);
            Estirar((RectTransform)areaRelleno.transform);

            Image relleno = CrearImagen("Relleno", areaRelleno.transform, ColorAcento);
            // Alto completo; el Slider mueve solo el borde derecho segun el valor.
            relleno.rectTransform.anchorMin = Vector2.zero;
            relleno.rectTransform.anchorMax = new Vector2(0f, 1f);
            relleno.rectTransform.sizeDelta = Vector2.zero;

            GameObject areaManija = new GameObject("AreaManija", typeof(RectTransform));
            areaManija.transform.SetParent(objeto.transform, false);
            Estirar((RectTransform)areaManija.transform);

            Image manija = CrearImagen("Manija", areaManija.transform, Color.white);
            manija.rectTransform.sizeDelta = new Vector2(22f, tamano.y + 8f);

            Slider slider = objeto.AddComponent<Slider>();
            slider.fillRect = relleno.rectTransform;
            slider.handleRect = manija.rectTransform;
            slider.targetGraphic = manija;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }

        private static Image CrearImagen(string nombre, Transform padre, Color color)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            Image imagen = objeto.AddComponent<Image>();
            imagen.color = color;
            return imagen;
        }

        private static Text CrearTexto(string nombre, Transform padre, string contenido, int tamanoFuente,
            Vector2 posicion, Vector2 tamano)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);

            Text texto = objeto.AddComponent<Text>();
            texto.text = contenido;
            texto.font = Fuente;
            texto.fontSize = tamanoFuente;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.color = ColorTexto;
            texto.raycastTarget = false;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;

            Colocar(texto.rectTransform, posicion, tamano);
            return texto;
        }

        // Ancla arriba-centro del padre, a la altura indicada desde arriba.
        private static void Colocar(RectTransform rect, Vector2 posicion, Vector2 tamano)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
        }

        private static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
