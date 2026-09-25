using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Fabrica de trozos de UI por codigo (paneles, botones, textos, sliders).
    // Existe para que los menus no repitan 200 lineas de AddComponent cada uno.
    //
    // PROTOTIPO: igual que en InterfazInventario, esto es andamiaje para tener menus
    // funcionando sin arte. Cuando haya arte de verdad se arman los paneles en la
    // escena, se arrastran a los campos del inspector y esta generacion no se usa.
    public static class ConstructorUI
    {
        // Paleta comun para que los 7 menus se vean igual entre si.
        public static readonly Color ColorFondoPantalla = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        public static readonly Color ColorPanel = new Color(0.12f, 0.12f, 0.17f, 0.98f);
        public static readonly Color ColorBoton = new Color(0.22f, 0.22f, 0.30f, 1f);
        public static readonly Color ColorBotonResaltado = new Color(0.32f, 0.32f, 0.44f, 1f);
        public static readonly Color ColorBotonPulsado = new Color(0.16f, 0.16f, 0.22f, 1f);
        public static readonly Color ColorBotonApagado = new Color(0.18f, 0.18f, 0.20f, 0.6f);
        public static readonly Color ColorTexto = new Color(0.94f, 0.94f, 0.96f, 1f);
        public static readonly Color ColorTextoApagado = new Color(0.55f, 0.55f, 0.60f, 1f);
        public static readonly Color ColorAcento = new Color(0.95f, 0.75f, 0.25f, 1f);
        public static readonly Color ColorExito = new Color(0.45f, 0.85f, 0.45f, 1f);
        public static readonly Color ColorFallo = new Color(0.90f, 0.40f, 0.40f, 1f);

        // Fuente integrada de Unity. Se cachea porque buscarla es relativamente caro
        // y los menus crean muchos textos de golpe.
        private static Font fuenteCache;

        // Fuente por defecto para todos los textos generados.
        public static Font Fuente
        {
            get
            {
                if (fuenteCache == null)
                {
                    // En Unity 6 la fuente integrada se llama LegacyRuntime.ttf
                    // (Arial.ttf ya no existe y devuelve null).
                    fuenteCache = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return fuenteCache;
            }
        }

        // Canvas a pantalla completa que escala con la resolucion.
        // ordenPantalla decide que menu se dibuja encima de cual.
        public static Canvas CrearCanvas(string nombre, Transform padre, int ordenPantalla)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);

            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ordenPantalla;

            CanvasScaler escalador = objeto.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            // 0.5 reparte el escalado entre ancho y alto, asi el menu aguanta
            // tanto pantallas panoramicas como cuadradas sin recortarse.
            escalador.matchWidthOrHeight = 0.5f;

            objeto.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        // Fondo que cubre toda la pantalla. Ademas de oscurecer el juego, su Image
        // bloquea los clicks para que no lleguen a lo que hay detras del menu.
        public static GameObject CrearFondoPantalla(Transform padre, Color color)
        {
            GameObject fondo = CrearImagen("Fondo", padre, color);
            Estirar(fondo.GetComponent<RectTransform>());
            return fondo;
        }

        // Panel centrado del tamano indicado, donde van los botones del menu.
        public static GameObject CrearPanel(string nombre, Transform padre, Vector2 tamano)
        {
            GameObject panel = CrearImagen(nombre, padre, ColorPanel);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = tamano;

            return panel;
        }

        // GameObject con un Image del color indicado.
        public static GameObject CrearImagen(string nombre, Transform padre, Color color)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.AddComponent<Image>().color = color;
            return objeto;
        }

        // Texto anclado arriba-centro del padre, a la altura indicada desde arriba.
        public static Text CrearTexto(string nombre, Transform padre, string contenido,
            int tamanoFuente, TextAnchor alineacion, Vector2 posicion, Vector2 tamano)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);

            Text texto = objeto.AddComponent<Text>();
            texto.text = contenido;
            texto.font = Fuente;
            texto.fontSize = tamanoFuente;
            texto.alignment = alineacion;
            texto.color = ColorTexto;
            // Sin esto un texto largo desaparece del todo en vez de encogerse.
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;

            return texto;
        }

        // Boton con su etiqueta, anclado arriba-centro del padre.
        // alPulsar puede ser null para botones que se enganchan despues.
        public static Button CrearBoton(string nombre, Transform padre, string etiqueta,
            Vector2 posicion, Vector2 tamano, Action alPulsar)
        {
            GameObject objeto = CrearImagen(nombre, padre, ColorBoton);

            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;

            Button boton = objeto.AddComponent<Button>();
            boton.targetGraphic = objeto.GetComponent<Image>();
            boton.colors = ColoresBoton();

            Text texto = CrearTexto("Etiqueta", objeto.transform, etiqueta,
                26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            Estirar(texto.GetComponent<RectTransform>());

            if (alPulsar != null)
            {
                boton.onClick.AddListener(() => alPulsar());
            }

            return boton;
        }

        // Slider horizontal con fondo, relleno y manija.
        public static Slider CrearSlider(string nombre, Transform padre, float valorInicial,
            float minimo, float maximo, Vector2 posicion, Vector2 tamano, Action<float> alCambiar)
        {
            GameObject objeto = CrearImagen(nombre, padre, new Color(0.08f, 0.08f, 0.12f, 1f));

            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;

            Slider slider = objeto.AddComponent<Slider>();
            slider.minValue = minimo;
            slider.maxValue = maximo;

            // El area de relleno se estira de lado a lado; el Slider mueve su borde
            // derecho segun el valor.
            GameObject areaRelleno = new GameObject("AreaRelleno");
            areaRelleno.transform.SetParent(objeto.transform, false);
            RectTransform rectArea = areaRelleno.AddComponent<RectTransform>();
            Estirar(rectArea);

            GameObject relleno = CrearImagen("Relleno", areaRelleno.transform, ColorAcento);
            RectTransform rectRelleno = relleno.GetComponent<RectTransform>();
            rectRelleno.anchorMin = Vector2.zero;
            rectRelleno.anchorMax = new Vector2(0f, 1f);
            rectRelleno.sizeDelta = Vector2.zero;

            GameObject areaManija = new GameObject("AreaManija");
            areaManija.transform.SetParent(objeto.transform, false);
            RectTransform rectAreaManija = areaManija.AddComponent<RectTransform>();
            Estirar(rectAreaManija);

            GameObject manija = CrearImagen("Manija", areaManija.transform, Color.white);
            RectTransform rectManija = manija.GetComponent<RectTransform>();
            rectManija.sizeDelta = new Vector2(22f, tamano.y + 8f);

            slider.fillRect = rectRelleno;
            slider.handleRect = rectManija;
            slider.targetGraphic = manija.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;

            // El valor se pone despues de enchufar fillRect/handleRect: al reves
            // el Slider no tiene donde dibujarlo y arranca visualmente en cero.
            slider.value = Mathf.Clamp(valorInicial, minimo, maximo);

            if (alCambiar != null)
            {
                slider.onValueChanged.AddListener(valor => alCambiar(valor));
            }

            return slider;
        }

        // Colores de interaccion de los botones (normal, encima, pulsado, apagado).
        public static ColorBlock ColoresBoton()
        {
            ColorBlock colores = ColorBlock.defaultColorBlock;
            colores.normalColor = Color.white;
            colores.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colores.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colores.selectedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colores.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            colores.fadeDuration = 0.08f;
            return colores;
        }

        // Hace que el rect ocupe todo el padre.
        public static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Asegura que exista un EventSystem en la escena. Sin el, los botones de UI
        // no reciben clicks: es el fallo mas comun al generar menus por codigo.
        public static void AsegurarEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                return;
            }

            GameObject objeto = new GameObject("EventSystem");
            objeto.AddComponent<UnityEngine.EventSystems.EventSystem>();
            objeto.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}
