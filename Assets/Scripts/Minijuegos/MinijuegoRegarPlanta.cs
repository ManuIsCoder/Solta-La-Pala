using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SoltaLaPala.Menus;
using SoltaLaPala.Inventory;

namespace SoltaLaPala.Minijuegos
{
    public class MinijuegoRegarPlanta : MinijuegoBase
    {
        [Header("IDs de Ítems")]
        public string idAgua = "BotellaAgua";
        public string idEnergetica = "BebidaEnergetica";

        [Header("Fondo UI")]
        public Image imagenFondo;

        [Header("Sprites de la Planta")]
        public Image imagenPlanta;
        public Sprite spritePlantaNormal;
        public Sprite spritePlantaCrecida;
        public Sprite spritePlantaMarchita;

        [Header("UI - Botones / Arrastrables")]
        public Button botonRegarAgua;
        public Button botonRegarEnergetica;
        public Button botonSalir;
        public Slider barraHidratacion;
        public Sprite spriteGota;

        [Header("Ajustes")]
        public float velocidadRegado = 0.35f;
        public bool modoPrueba = false;
        [Range(0f, 90f)] public float inclinacionMinima = 12f;
        public float gradosPorPixel = 1.5f;
        [Range(0f, 90f)] public float inclinacionMaxima = 85f;
        public Color colorAgua = new Color(0.35f, 0.8f, 1f, 1f);
        public Color colorEnergetica = new Color(0.3f, 0.95f, 0.35f, 1f);

        private sealed class GotaUI
        {
            public RectTransform rectTransform;
            public Image imagen;
            public Vector2 velocidad;
            public float edad;
        }

        private readonly List<GotaUI> gotas = new List<GotaUI>();
        private bool arrastrando = false;
        private bool regando = false;
        private bool esSabotaje = false;
        private bool completado = false;
        private Button botonAgarrado;
        private RectTransform rectBotonAgarrado;
        private RectTransform rectSpriteBotella;
        private RectTransform rectPadreBotella;
        private Vector2 pivotOriginalBotella;
        private Vector2 posicionPunteroInicial;
        private float inclinacionActual;
        private float acumuladorGotas;
        private Canvas canvas;
        private RectTransform rectPanel;
        private GameObject avisoSinLiquido;

        protected override void AlIniciar()
        {
            bool tieneAgua = !completado && (modoPrueba || (Inventario != null && Inventario.TieneItem(idAgua)));
            bool tieneEnergetica = !completado && (modoPrueba || (Inventario != null && Inventario.TieneItem(idEnergetica)));
            bool sinLiquido = !tieneAgua && !tieneEnergetica;
            canvas = GetComponentInParent<Canvas>();
            rectPanel = panelRaiz != null ? panelRaiz.transform as RectTransform : transform as RectTransform;
            arrastrando = false;
            regando = false;
            esSabotaje = false;
            botonAgarrado = null;
            inclinacionActual = 0f;
            acumuladorGotas = 0f;

            if (barraHidratacion != null)
            {
                barraHidratacion.maxValue = 1f;
                barraHidratacion.value = completado ? 1f : 0f;
            }
            if (!completado && imagenPlanta != null && spritePlantaNormal != null)
                imagenPlanta.sprite = spritePlantaNormal;
            PrepararGotas();

            if (botonRegarAgua != null)
            {
                botonRegarAgua.gameObject.SetActive(tieneAgua);
                botonRegarAgua.interactable = true;
                botonRegarAgua.onClick.RemoveAllListeners();
                ConfigurarArrastre(botonRegarAgua, false);
            }

            if (botonRegarEnergetica != null)
            {
                botonRegarEnergetica.gameObject.SetActive(tieneEnergetica);
                botonRegarEnergetica.interactable = true;
                botonRegarEnergetica.onClick.RemoveAllListeners();
                ConfigurarArrastre(botonRegarEnergetica, true);
            }

            if (botonSalir != null)
            {
                ConstructorUI.ConfigurarBotonSalir(botonSalir, "Salir (Esc)");
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
            }

            MostrarAvisoSinLiquido(sinLiquido, completado
                ? "Ya regaste la planta. Reinicia el nivel para volver a jugar."
                : "Necesitas un líquido para regar la planta");
        }

        private void ConfigurarArrastre(Button boton, bool conEnergetica)
        {
            boton.transition = Selectable.Transition.None;
            Image fondoBoton = boton.GetComponent<Image>();
            if (fondoBoton != null)
            {
                fondoBoton.color = new Color(1f, 1f, 1f, 0f);
                fondoBoton.raycastTarget = true;
            }

            foreach (Image imagenHija in boton.GetComponentsInChildren<Image>(true))
            {
                if (imagenHija != fondoBoton) imagenHija.raycastTarget = false;
            }
            foreach (Text textoHijo in boton.GetComponentsInChildren<Text>(true))
                textoHijo.raycastTarget = false;

            EventTrigger trigger = boton.GetComponent<EventTrigger>();
            if (trigger == null) trigger = boton.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
            trigger.triggers.Clear();

            AgregarEvento(trigger, EventTriggerType.PointerDown, data =>
                AgarrarBotella(boton, conEnergetica, (PointerEventData)data));
            AgregarEvento(trigger, EventTriggerType.Drag, data =>
                InclinarBotella((PointerEventData)data));
            AgregarEvento(trigger, EventTriggerType.PointerUp, _ => SoltarBotella());
        }

        private static void AgregarEvento(EventTrigger trigger, EventTriggerType tipo, System.Action<BaseEventData> accion)
        {
            EventTrigger.Entry entrada = new EventTrigger.Entry { eventID = tipo };
            entrada.callback.AddListener(data => accion(data));
            trigger.triggers.Add(entrada);
        }

        private void AgarrarBotella(Button boton, bool conEnergetica, PointerEventData evento)
        {
            if (completado || regando || (conEnergetica ? !modoPrueba && (Inventario == null || !Inventario.TieneItem(idEnergetica))
                                          : !modoPrueba && (Inventario == null || !Inventario.TieneItem(idAgua))))
                return;

            arrastrando = true;
            botonAgarrado = boton;
            rectBotonAgarrado = boton.transform as RectTransform;
            rectSpriteBotella = ObtenerRectSpriteBotella(boton);
            rectPadreBotella = rectBotonAgarrado.parent as RectTransform;
            pivotOriginalBotella = rectBotonAgarrado.pivot;
            posicionPunteroInicial = evento.position;
            inclinacionActual = 0f;
            esSabotaje = conEnergetica;

            if (rectPadreBotella != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectPadreBotella, evento.position, ObtenerCamaraCanvas(), out Vector2 posicionLocal))
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectBotonAgarrado, evento.position, ObtenerCamaraCanvas(), out Vector2 puntoAgarrado);
                Vector2 nuevoPivot = new Vector2(
                    puntoAgarrado.x / rectBotonAgarrado.rect.width + pivotOriginalBotella.x,
                    puntoAgarrado.y / rectBotonAgarrado.rect.height + pivotOriginalBotella.y);
                rectBotonAgarrado.pivot = new Vector2(Mathf.Clamp01(nuevoPivot.x), Mathf.Clamp01(nuevoPivot.y));
                rectBotonAgarrado.anchoredPosition = posicionLocal;
            }
        }

        private void InclinarBotella(PointerEventData evento)
        {
            if (!arrastrando || rectBotonAgarrado == null) return;

            if (rectPadreBotella != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectPadreBotella, evento.position, ObtenerCamaraCanvas(), out Vector2 posicionLocal))
            {
                rectBotonAgarrado.anchoredPosition = posicionLocal;
            }

            inclinacionActual = Mathf.Clamp(
                (evento.position.y - posicionPunteroInicial.y) * gradosPorPixel,
                -inclinacionMaxima,
                inclinacionMaxima);
            rectBotonAgarrado.localRotation = Quaternion.Euler(0f, 0f, -inclinacionActual);
            regando = Mathf.Abs(inclinacionActual) >= inclinacionMinima;
        }

        private void SoltarBotella()
        {
            arrastrando = false;
            regando = false;
            inclinacionActual = 0f;
            if (rectBotonAgarrado != null)
            {
                Vector3 centroMundo = rectBotonAgarrado.TransformPoint(rectBotonAgarrado.rect.center);
                rectBotonAgarrado.pivot = pivotOriginalBotella;
                rectBotonAgarrado.localRotation = Quaternion.identity;
                if (rectPadreBotella != null)
                {
                    Vector2 centroActual = rectPadreBotella.InverseTransformPoint(centroMundo);
                    Vector2 centroNuevo = rectPadreBotella.InverseTransformPoint(
                        rectBotonAgarrado.TransformPoint(rectBotonAgarrado.rect.center));
                    rectBotonAgarrado.anchoredPosition += centroActual - centroNuevo;
                }
            }
            botonAgarrado = null;
            rectBotonAgarrado = null;
            rectSpriteBotella = null;
            rectPadreBotella = null;
        }

        private static RectTransform ObtenerRectSpriteBotella(Button boton)
        {
            foreach (Image imagen in boton.GetComponentsInChildren<Image>(true))
            {
                if (imagen.transform != boton.transform && imagen.sprite != null)
                    return imagen.rectTransform;
            }

            return boton.transform as RectTransform;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Salir();

            ActualizarGotas();
            if (!completado && arrastrando && regando)
            {
                if (barraHidratacion != null)
                {
                    barraHidratacion.value = Mathf.MoveTowards(barraHidratacion.value, barraHidratacion.maxValue,
                        velocidadRegado * Time.deltaTime);
                    if (barraHidratacion.value >= barraHidratacion.maxValue)
                    {
                        completado = true;
                        arrastrando = false;
                        regando = false;
                        if (rectBotonAgarrado != null) rectBotonAgarrado.localRotation = Quaternion.identity;
                        if (botonRegarAgua != null) botonRegarAgua.interactable = false;
                        if (botonRegarEnergetica != null) botonRegarEnergetica.interactable = false;
                        StartCoroutine(AnimacionFinal());
                    }
                }

                acumuladorGotas += Time.deltaTime * 18f;
                while (acumuladorGotas >= 1f)
                {
                    EmitirGota();
                    acumuladorGotas -= 1f;
                }
            }
        }

        private void PrepararGotas()
        {
            if (rectPanel == null || spriteGota == null || gotas.Count > 0) return;

            for (int i = 0; i < 16; i++)
            {
                GameObject objetoGota = new GameObject("GotaUI", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                objetoGota.transform.SetParent(rectPanel, false);
                RectTransform rect = objetoGota.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(24f, 24f);
                Image imagen = objetoGota.GetComponent<Image>();
                imagen.sprite = spriteGota;
                imagen.preserveAspect = true;
                imagen.raycastTarget = false;
                objetoGota.SetActive(false);
                gotas.Add(new GotaUI { rectTransform = rect, imagen = imagen });
            }
        }

        private void EmitirGota()
        {
            if (rectBotonAgarrado == null || rectSpriteBotella == null || gotas.Count == 0 || rectPanel == null) return;

            GotaUI gota = gotas.Find(elemento => !elemento.rectTransform.gameObject.activeSelf);
            if (gota == null) return;

            Vector3 posicionTapa = rectSpriteBotella.TransformPoint(new Vector3(
                rectSpriteBotella.rect.width * (0.5f - rectSpriteBotella.pivot.x),
                rectSpriteBotella.rect.height * (1f - rectSpriteBotella.pivot.y),
                0f));
            Vector2 posicionPantalla = RectTransformUtility.WorldToScreenPoint(
                ObtenerCamaraCanvas(),
                posicionTapa);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectPanel, posicionPantalla, ObtenerCamaraCanvas(),
                    out Vector2 posicionLocal))
                return;

            Vector2 destino = Vector2.zero;
            if (imagenPlanta != null)
            {
                Vector2 pantallaPlanta = RectTransformUtility.WorldToScreenPoint(
                    ObtenerCamaraCanvas(),
                    imagenPlanta.rectTransform.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectPanel, pantallaPlanta, ObtenerCamaraCanvas(),
                    out destino);
            }

            gota.rectTransform.anchoredPosition = posicionLocal;
            gota.velocidad = (destino - posicionLocal).normalized * Random.Range(210f, 300f)
                + new Vector2(Random.Range(-35f, 35f), Random.Range(-20f, 20f));
            gota.edad = 0f;
            gota.imagen.color = esSabotaje ? colorEnergetica : colorAgua;
            gota.rectTransform.localRotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(gota.velocidad.y, gota.velocidad.x) * Mathf.Rad2Deg - 90f);
            float tamano = Random.Range(20f, 27f);
            gota.rectTransform.sizeDelta = Vector2.one * tamano;
            gota.rectTransform.SetAsLastSibling();
            gota.rectTransform.gameObject.SetActive(true);
        }

        private Camera ObtenerCamaraCanvas()
        {
            return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
        }

        private void MostrarAvisoSinLiquido(bool mostrar, string texto)
        {
            if (avisoSinLiquido == null && panelRaiz != null)
            {
                avisoSinLiquido = new GameObject("AvisoSinLiquido", typeof(RectTransform), typeof(Image));
                avisoSinLiquido.transform.SetParent(panelRaiz.transform, false);
                ConstructorUI.Estirar(avisoSinLiquido.GetComponent<RectTransform>());

                Image fondoAviso = avisoSinLiquido.GetComponent<Image>();
                fondoAviso.color = new Color(0.02f, 0.04f, 0.05f, 0.78f);
                fondoAviso.raycastTarget = false;

                GameObject objetoTexto = new GameObject("Mensaje", typeof(RectTransform), typeof(Text));
                objetoTexto.transform.SetParent(avisoSinLiquido.transform, false);
                RectTransform rectTexto = objetoTexto.GetComponent<RectTransform>();
                rectTexto.anchorMin = new Vector2(0.5f, 0.5f);
                rectTexto.anchorMax = new Vector2(0.5f, 0.5f);
                rectTexto.pivot = new Vector2(0.5f, 0.5f);
                rectTexto.sizeDelta = new Vector2(700f, 140f);

                Text mensaje = objetoTexto.GetComponent<Text>();
                mensaje.font = ConstructorUI.Fuente;
                mensaje.fontSize = 30;
                mensaje.fontStyle = FontStyle.Bold;
                mensaje.alignment = TextAnchor.MiddleCenter;
                mensaje.color = Color.white;
                mensaje.raycastTarget = false;
                avisoSinLiquido.SetActive(false);
            }

            if (avisoSinLiquido == null) return;
            Text textoAviso = avisoSinLiquido.GetComponentInChildren<Text>(true);
            if (textoAviso != null) textoAviso.text = texto;
            avisoSinLiquido.SetActive(mostrar);
            if (mostrar)
            {
                avisoSinLiquido.transform.SetAsLastSibling();
                if (botonSalir != null) botonSalir.transform.SetAsLastSibling();
            }
        }

        private void ActualizarGotas()
        {
            foreach (GotaUI gota in gotas)
            {
                if (!gota.rectTransform.gameObject.activeSelf) continue;

                gota.edad += Time.deltaTime;
                if (gota.edad >= 0.55f)
                {
                    gota.rectTransform.gameObject.SetActive(false);
                    continue;
                }

                gota.rectTransform.anchoredPosition += gota.velocidad * Time.deltaTime;
                gota.velocidad += Vector2.down * (500f * Time.deltaTime);
                Color color = gota.imagen.color;
                color.a = 1f - gota.edad / 0.55f;
                gota.imagen.color = color;
            }
        }

        private IEnumerator AnimacionFinal()
        {
            // Cambiar el sprite de la planta
            if (imagenPlanta != null)
            {
                imagenPlanta.sprite = esSabotaje ? spritePlantaMarchita : spritePlantaCrecida;
            }

            int impacto = esSabotaje ? 3 : 1;
            int sospecha = esSabotaje ? 7 : 0;

            if (EstadoPartida.Instancia != null)
                EstadoPartida.Instancia.RegistrarSabotaje(
                    impacto,
                    sospecha,
                    esSabotaje ? "Planta de la oficina marchitada" : null);

            CompletarTareaNivel();

            if (esSabotaje)
                Inventario?.QuitarItem(idEnergetica);
            else
                Inventario?.QuitarItem(idAgua);

            yield return new WaitForSeconds(1.2f);
            Salir();
        }

        public override void Reiniciar()
        {
            StopAllCoroutines();
            completado = false;
            arrastrando = false;
            regando = false;
            esSabotaje = false;
            if (barraHidratacion != null) barraHidratacion.value = 0f;
            if (imagenPlanta != null && spritePlantaNormal != null) imagenPlanta.sprite = spritePlantaNormal;
            if (botonRegarAgua != null) botonRegarAgua.interactable = true;
            if (botonRegarEnergetica != null) botonRegarEnergetica.interactable = true;
            if (avisoSinLiquido != null) avisoSinLiquido.SetActive(false);
            foreach (GotaUI gota in gotas)
                gota.rectTransform.gameObject.SetActive(false);
        }

        protected override void AlSalir()
        {
            SoltarBotella();
            regando = false;
            StopAllCoroutines();
            foreach (GotaUI gota in gotas)
                gota.rectTransform.gameObject.SetActive(false);
        }
    }
}