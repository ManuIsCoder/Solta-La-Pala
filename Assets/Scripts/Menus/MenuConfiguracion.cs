using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Configuracion: audio, raton, pantalla y reasignacion de teclas.
    //
    // La reasignacion funciona asi: pulsas el boton de una accion, el menu entra en
    // modo escucha y la siguiente tecla que pulses queda asignada. ESC cancela.
    public class MenuConfiguracion : MenuBase
    {
        protected override int OrdenPantalla => 140;

        // Fila de la lista de controles: la etiqueta de la accion y el boton con su tecla.
        [System.Serializable]
        public class FilaControl
        {
            public AccionJuego accion;
            public Button boton;
            public Text textoTecla;
        }

        [Header("Prefab (Resources/Menus)")]
        public Slider sliderGeneral;
        public Slider sliderMusica;
        public Slider sliderEfectos;
        [Tooltip("Un solo slider para los dos ejes. Si esta asignado, X e Y se ignoran.")]
        public Slider sliderSensibilidad;
        public Slider sliderSensibilidadX;
        public Slider sliderSensibilidadY;
        public Button botonInvertirY;
        public Text textoInvertirY;
        public Sprite spriteInvertirSi;
        public Sprite spriteInvertirNo;
        public Button botonPantallaCompleta;
        public Text textoPantallaCompleta;
        public Sprite spritePantallaSi;
        public Sprite spritePantallaNo;
        public Button botonRestaurar;
        public Button botonVolver;
        public Text textoEstado;
        [Tooltip("Una fila por accion reasignable.")]
        public List<FilaControl> filas = new List<FilaControl>();
        [Tooltip("Color del nombre de la tecla dentro de cada boton.")]
        public Color colorTextoTecla = ConstructorUI.ColorTexto;

        protected override void Enlazar()
        {
            EngancharSlider(sliderGeneral, valor => AjustesJuego.VolumenGeneral = valor);
            EngancharSlider(sliderMusica, valor => AjustesJuego.VolumenMusica = valor);
            EngancharSlider(sliderEfectos, valor => AjustesJuego.VolumenEfectos = valor);

            if (sliderSensibilidadX != null)
            {
                sliderSensibilidadX.minValue = AjustesJuego.SensibilidadMinima;
                sliderSensibilidadX.maxValue = AjustesJuego.SensibilidadMaxima;
            }
            if (sliderSensibilidadY != null)
            {
                sliderSensibilidadY.minValue = AjustesJuego.SensibilidadMinima;
                sliderSensibilidadY.maxValue = AjustesJuego.SensibilidadMaxima;
            }
            EngancharSlider(sliderSensibilidadX, valor => AjustesJuego.SensibilidadX = valor);
            EngancharSlider(sliderSensibilidadY, valor => AjustesJuego.SensibilidadY = valor);

            if (sliderSensibilidad != null)
            {
                sliderSensibilidad.minValue = AjustesJuego.SensibilidadMinima;
                sliderSensibilidad.maxValue = AjustesJuego.SensibilidadMaxima;
            }
            EngancharSlider(sliderSensibilidad, valor =>
            {
                AjustesJuego.SensibilidadX = valor;
                AjustesJuego.SensibilidadY = valor * ProporcionSensibilidadY;
            });

            Enganchar(botonInvertirY, () => { AjustesJuego.InvertirY = !AjustesJuego.InvertirY; RefrescarValores(); });
            Enganchar(botonPantallaCompleta, () => { AjustesJuego.PantallaCompleta = !AjustesJuego.PantallaCompleta; RefrescarValores(); });
            Enganchar(botonRestaurar, RestaurarTodo);
            Enganchar(botonVolver, IntentarVolver);

            foreach (FilaControl fila in filas)
            {
                // Copia local: sin esto todos los botones compartirian la ultima fila.
                AccionJuego accionFila = fila.accion;
                Enganchar(fila.boton, () => EmpezarEscucha(accionFila));
            }
        }

        // El slider unico mueve los dos ejes manteniendo la proporcion de fabrica
        // (200 horizontal, 150 vertical): el eje Y siempre va un poco mas lento.
        private const float ProporcionSensibilidadY = 0.75f;

        private static void EngancharSlider(Slider slider, UnityEngine.Events.UnityAction<float> alCambiar)
        {
            if (slider != null)
            {
                slider.onValueChanged.AddListener(alCambiar);
            }
        }

        // Accion que se esta reasignando ahora mismo, o null si no hay ninguna.
        private AccionJuego? accionEscuchando;

        // Mientras se escucha una tecla se ignoran las demas pulsaciones del menu.
        private bool EstaEscuchando => accionEscuchando.HasValue;

        protected override void Construir(Transform raiz)
        {
            GameObject panelFondo = CrearFondoYPanel(raiz, new Vector2(1000f, 860f));
            Transform padre = panelFondo.transform;

            CrearTitulo(padre, "CONFIGURACION", 1000f);

            // Dos columnas: ajustes a la izquierda, controles a la derecha.
            ConstruirColumnaAjustes(padre, -250f);
            ConstruirColumnaControles(padre, 260f);

            textoEstado = ConstructorUI.CrearTexto("Estado", padre, "",
                18, TextAnchor.MiddleCenter, new Vector2(0f, -720f), new Vector2(900f, 28f));
            textoEstado.color = ConstructorUI.ColorTextoApagado;

            ConstructorUI.CrearBoton("BotonRestaurar", padre, "Restaurar valores",
                new Vector2(-150f, -770f), new Vector2(280f, 54f), RestaurarTodo);

            ConstructorUI.CrearBoton("BotonVolver", padre, "Volver",
                new Vector2(150f, -770f), new Vector2(280f, 54f), IntentarVolver);
        }

        // Columna izquierda: volumen, raton y pantalla.
        private void ConstruirColumnaAjustes(Transform padre, float x)
        {
            float y = -110f;

            CrearSubtitulo(padre, "AUDIO", x, y);
            y -= 50f;

            sliderGeneral = CrearFilaSlider(padre, "Volumen general", x, ref y,
                AjustesJuego.VolumenGeneral, 0f, 1f, valor => AjustesJuego.VolumenGeneral = valor);

            sliderMusica = CrearFilaSlider(padre, "Musica", x, ref y,
                AjustesJuego.VolumenMusica, 0f, 1f, valor => AjustesJuego.VolumenMusica = valor);

            sliderEfectos = CrearFilaSlider(padre, "Efectos", x, ref y,
                AjustesJuego.VolumenEfectos, 0f, 1f, valor => AjustesJuego.VolumenEfectos = valor);

            y -= 24f;
            CrearSubtitulo(padre, "RATON", x, y);
            y -= 50f;

            sliderSensibilidadX = CrearFilaSlider(padre, "Sensibilidad X", x, ref y,
                AjustesJuego.SensibilidadX, AjustesJuego.SensibilidadMinima, AjustesJuego.SensibilidadMaxima,
                valor => AjustesJuego.SensibilidadX = valor);

            sliderSensibilidadY = CrearFilaSlider(padre, "Sensibilidad Y", x, ref y,
                AjustesJuego.SensibilidadY, AjustesJuego.SensibilidadMinima, AjustesJuego.SensibilidadMaxima,
                valor => AjustesJuego.SensibilidadY = valor);

            Button botonInvertir = ConstructorUI.CrearBoton("BotonInvertirY", padre, "",
                new Vector2(x, y), new Vector2(400f, 46f),
                () => { AjustesJuego.InvertirY = !AjustesJuego.InvertirY; RefrescarValores(); });
            textoInvertirY = botonInvertir.GetComponentInChildren<Text>();
            y -= 70f;

            CrearSubtitulo(padre, "PANTALLA", x, y);
            y -= 50f;

            Button botonPantalla = ConstructorUI.CrearBoton("BotonPantallaCompleta", padre, "",
                new Vector2(x, y), new Vector2(400f, 46f),
                () => { AjustesJuego.PantallaCompleta = !AjustesJuego.PantallaCompleta; RefrescarValores(); });
            textoPantallaCompleta = botonPantalla.GetComponentInChildren<Text>();
        }

        // Columna derecha: una fila por accion reasignable.
        private void ConstruirColumnaControles(Transform padre, float x)
        {
            float y = -110f;

            CrearSubtitulo(padre, "CONTROLES", x, y);
            y -= 46f;

            ConstructorUI.CrearTexto("AyudaControles", padre,
                "Pulsa una accion y despues la tecla nueva.",
                16, TextAnchor.MiddleCenter, new Vector2(x, y), new Vector2(420f, 24f))
                .color = ConstructorUI.ColorTextoApagado;
            y -= 34f;

            foreach (AccionJuego accion in ControlesJuego.TodasLasAcciones)
            {
                ConstructorUI.CrearTexto($"Etiqueta{accion}", padre, ControlesJuego.ObtenerNombre(accion),
                    20, TextAnchor.MiddleLeft, new Vector2(x - 110f, y), new Vector2(200f, 34f));

                // La accion se captura en una variable local: sin esto todos los
                // botones compartirian la ultima del bucle.
                AccionJuego accionFila = accion;

                Button boton = ConstructorUI.CrearBoton($"Tecla{accion}", padre, "",
                    new Vector2(x + 120f, y), new Vector2(170f, 34f), () => EmpezarEscucha(accionFila));

                Text textoTecla = boton.GetComponentInChildren<Text>();
                if (textoTecla != null)
                {
                    textoTecla.fontSize = 18;
                }

                filas.Add(new FilaControl { accion = accionFila, boton = boton, textoTecla = textoTecla });
                y -= 42f;
            }
        }

        private static void CrearSubtitulo(Transform padre, string texto, float x, float y)
        {
            ConstructorUI.CrearTexto($"Subtitulo{texto}", padre, texto,
                24, TextAnchor.MiddleCenter, new Vector2(x, y), new Vector2(420f, 32f))
                .color = ConstructorUI.ColorAcento;
        }

        // Etiqueta + slider. Avanza la 'y' para la fila siguiente.
        private Slider CrearFilaSlider(Transform padre, string etiqueta, float x, ref float y,
            float valor, float minimo, float maximo, System.Action<float> alCambiar)
        {
            ConstructorUI.CrearTexto($"Etiqueta{etiqueta}", padre, etiqueta,
                20, TextAnchor.MiddleLeft, new Vector2(x - 110f, y), new Vector2(240f, 28f));

            Slider slider = ConstructorUI.CrearSlider($"Slider{etiqueta}", padre, valor, minimo, maximo,
                new Vector2(x + 90f, y - 32f), new Vector2(380f, 16f), alCambiar);

            y -= 74f;
            return slider;
        }

        // Al abrirse refresca todo por si los valores cambiaron desde otra pantalla.
        protected override void AlMostrar()
        {
            CancelarEscucha();
            RefrescarValores();
            RefrescarTeclas();
        }

        // Vuelca los valores guardados en los sliders y botones.
        private void RefrescarValores()
        {
            // SetValueWithoutNotify evita que poner el valor dispare el onValueChanged
            // y vuelva a guardar en bucle.
            sliderGeneral?.SetValueWithoutNotify(AjustesJuego.VolumenGeneral);
            sliderMusica?.SetValueWithoutNotify(AjustesJuego.VolumenMusica);
            sliderEfectos?.SetValueWithoutNotify(AjustesJuego.VolumenEfectos);
            sliderSensibilidadX?.SetValueWithoutNotify(AjustesJuego.SensibilidadX);
            sliderSensibilidadY?.SetValueWithoutNotify(AjustesJuego.SensibilidadY);
            sliderSensibilidad?.SetValueWithoutNotify(AjustesJuego.SensibilidadX);

            PonerSprite(botonInvertirY, AjustesJuego.InvertirY ? spriteInvertirSi : spriteInvertirNo);
            PonerSprite(botonPantallaCompleta, AjustesJuego.PantallaCompleta ? spritePantallaSi : spritePantallaNo);

            if (textoInvertirY != null)
            {
                textoInvertirY.text = AjustesJuego.InvertirY
                    ? "Invertir eje Y: SI"
                    : "Invertir eje Y: NO";
            }

            if (textoPantallaCompleta != null)
            {
                textoPantallaCompleta.text = AjustesJuego.PantallaCompleta
                    ? "Pantalla completa: SI"
                    : "Pantalla completa: NO";
            }
        }

        // Los botones de SI/NO cambian de sprite en vez de texto cuando hay arte.
        private static void PonerSprite(Button boton, Sprite sprite)
        {
            // GetComponent y no boton.image: en BotonMenu el targetGraphic es el Brillo.
            Image imagen = boton != null ? boton.GetComponent<Image>() : null;
            if (imagen != null && sprite != null)
            {
                imagen.sprite = sprite;
            }
        }

        // Nombre corto para que entre en la cajita de la tecla.
        private static string NombreTeclaCorto(KeyCode tecla)
        {
            switch (tecla)
            {
                case KeyCode.LeftShift: return "↑IZQ";
                case KeyCode.RightShift: return "↑DER";
                case KeyCode.LeftControl: return "CTRL I";
                case KeyCode.RightControl: return "CTRL D";
                case KeyCode.Space: return "ESP";
                case KeyCode.Return: return "ENTER";
                default: return MenuTutorial.NombreTecla(tecla).ToUpperInvariant();
            }
        }

        // Pone en cada boton la tecla que tiene asignada su accion.
        private void RefrescarTeclas()
        {
            foreach (FilaControl fila in filas)
            {
                if (fila.textoTecla == null)
                {
                    continue;
                }

                // La fila que se esta reasignando muestra un aviso en vez de la tecla.
                if (EstaEscuchando && accionEscuchando.Value == fila.accion)
                {
                    // Corto para que entre en la cajita; el detalle lo da textoEstado.
                    fila.textoTecla.text = "...";
                    fila.textoTecla.color = ConstructorUI.ColorAcento;
                    continue;
                }

                KeyCode tecla = ControlesJuego.ObtenerTecla(fila.accion);
                fila.textoTecla.text = NombreTeclaCorto(tecla);
                fila.textoTecla.color = tecla == KeyCode.None
                    ? ConstructorUI.ColorFallo
                    : colorTextoTecla;
            }
        }

        // Entra en modo escucha para reasignar una accion.
        private void EmpezarEscucha(AccionJuego accion)
        {
            accionEscuchando = accion;

            if (textoEstado != null)
            {
                textoEstado.text = $"Pulsa la tecla nueva para '{ControlesJuego.ObtenerNombre(accion)}'. ESC cancela.";
            }

            RefrescarTeclas();
        }

        private void CancelarEscucha()
        {
            accionEscuchando = null;

            if (textoEstado != null)
            {
                textoEstado.text = "";
            }
        }

        // Mientras se escucha, captura la primera tecla pulsada y la asigna.
        private void Update()
        {
            if (!EstaEscuchando)
            {
                return;
            }

            // ESC cancela en vez de asignarse: si no, seria facil perder la tecla de pausa.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelarEscucha();
                RefrescarTeclas();
                return;
            }

            KeyCode pulsada = LeerTeclaPulsada();
            if (pulsada == KeyCode.None)
            {
                return;
            }

            AccionJuego accion = accionEscuchando.Value;
            ControlesJuego.AsignarTecla(accion, pulsada);
            CancelarEscucha();

            if (textoEstado != null)
            {
                textoEstado.text = $"'{ControlesJuego.ObtenerNombre(accion)}' asignada a {MenuTutorial.NombreTecla(pulsada)}.";
            }

            RefrescarTeclas();
        }

        // Recorre los KeyCode y devuelve el primero que se pulso este frame.
        // Se ignoran los clicks del raton porque se usan para navegar el propio menu.
        private static KeyCode LeerTeclaPulsada()
        {
            foreach (KeyCode tecla in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (tecla >= KeyCode.Mouse0 && tecla <= KeyCode.Mouse6)
                {
                    continue;
                }

                if (Input.GetKeyDown(tecla))
                {
                    return tecla;
                }
            }

            return KeyCode.None;
        }

        // Vuelve a los valores de fabrica, tanto ajustes como teclas.
        private void RestaurarTodo()
        {
            if (EstaEscuchando)
            {
                return;
            }

            AjustesJuego.RestaurarPorDefecto();
            ControlesJuego.RestaurarPorDefecto();

            RefrescarValores();
            RefrescarTeclas();

            if (textoEstado != null)
            {
                textoEstado.text = "Valores restaurados.";
            }
        }

        // No dejar salir a media reasignacion: seria facil dejar una accion sin tecla.
        private void IntentarVolver()
        {
            if (EstaEscuchando)
            {
                CancelarEscucha();
                RefrescarTeclas();
                return;
            }

            Gestor?.Volver();
        }
    }
}
