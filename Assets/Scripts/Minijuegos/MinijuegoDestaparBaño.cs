using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SoltaLaPala.Menus;
using SoltaLaPala.Inventory;

namespace SoltaLaPala.Minijuegos
{
    public class MinijuegoDestaparBaño : MinijuegoBase
    {
        [Header("IDs de Ítems")]
        public string idDestapador = "Destapador";
        public string idSoreteResultante = "Sorete";

        [Header("Fondo UI")]
        public Image imagenFondo;

        [Header("UI - Elementos")]
        public Image imagenInodoro;
        public Image imagenSoreteFlotante;
        public Button botonBombear;
        public Button botonSalir;
        public Slider barraDestape;
        public GameObject efectoAgua;
        public GameObject prefabSoreteRecogible;

        [Header("Ajustes")]
        public float progresoPorBombeo = 0.0666667f;
        public float perdidaProgresoPorSegundo = 0.06f;
        public float desplazamientoMaximoBombeo = 180f;
        public float margenInferiorDestapador = 12f;
        public bool modoPrueba = false;

        private bool completado = false;
        private bool tieneDestapador;
        private bool arrastrandoDestapador;
        private bool seMovioEsteFrame;
        private RectTransform rectDestapador;
        private RectTransform rectPadreDestapador;
        private Vector2 posicionInicialPuntero;
        private Vector2 posicionInicialDestapador;
        private Vector2 posicionCentroDestapador;
        private bool posicionCentroInicializada;
        private float ultimaPosicionDestapadorY;
        private float duracionEfectoAgua;
        private float tiempoEntreParticulas;
        private Canvas canvas;
        private RectTransform rectPanel;
        private GameObject soreteMundoInstancia;
        private readonly Vector3[] esquinasUI = new Vector3[4];

        private sealed class ParticulaAguaUI
        {
            public RectTransform rect;
            public Image imagen;
            public Vector2 velocidad;
            public float edad;
            public float duracion;
        }

        private readonly List<ParticulaAguaUI> particulasAgua = new List<ParticulaAguaUI>();

        protected override void AlIniciar()
        {
            tieneDestapador = modoPrueba || (Inventario != null && Inventario.TieneItem(idDestapador));
            arrastrandoDestapador = false;
            seMovioEsteFrame = false;
            canvas = GetComponentInParent<Canvas>();
            rectPanel = panelRaiz != null ? panelRaiz.transform as RectTransform : transform as RectTransform;
            if (imagenInodoro != null) imagenInodoro.raycastTarget = false;
            rectDestapador = botonBombear != null ? botonBombear.transform as RectTransform : null;
            rectPadreDestapador = rectDestapador != null ? rectDestapador.parent as RectTransform : null;
            if (rectDestapador != null)
            {
                if (!posicionCentroInicializada)
                {
                    posicionCentroDestapador = rectDestapador.anchoredPosition;
                    posicionCentroInicializada = true;
                }
                rectDestapador.anchoredPosition = posicionCentroDestapador;
            }

            if (barraDestape != null)
            {
                barraDestape.maxValue = 1f;
                barraDestape.value = completado ? 1f : 0f;
            }
            if (imagenSoreteFlotante != null) imagenSoreteFlotante.gameObject.SetActive(completado);
            if (efectoAgua != null) efectoAgua.SetActive(false);
            if (efectoAgua != null && efectoAgua.TryGetComponent(out Image imagenEfecto))
                imagenEfecto.raycastTarget = false;
            PrepararParticulasAgua();
            foreach (ParticulaAguaUI particula in particulasAgua)
                particula.rect.gameObject.SetActive(false);

            if (botonBombear != null)
            {
                botonBombear.interactable = tieneDestapador && !completado;
                botonBombear.transition = Selectable.Transition.None;
                Image fondoBoton = botonBombear.GetComponent<Image>();
                if (fondoBoton != null)
                {
                    fondoBoton.color = new Color(1f, 1f, 1f, 0f);
                    fondoBoton.raycastTarget = true;
                }

                foreach (Text texto in botonBombear.GetComponentsInChildren<Text>(true))
                    texto.gameObject.SetActive(false);

                botonBombear.onClick.RemoveAllListeners();
                ConfigurarArrastreDestapador(botonBombear);
            }

            if (botonSalir != null)
            {
                ConfigurarBotonSalirLocal();
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
                botonSalir.interactable = true;
            }
        }

        private void ConfigurarBotonSalirLocal()
        {
            Image fondo = botonSalir.GetComponent<Image>();
            if (fondo != null)
            {
                fondo.color = new Color(0.6f, 0.2f, 0.2f, 1f);
                botonSalir.targetGraphic = fondo;
            }

            ColorBlock colores = ColorBlock.defaultColorBlock;
            colores.normalColor = Color.white;
            colores.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colores.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colores.selectedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colores.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            colores.fadeDuration = 0.08f;
            botonSalir.colors = colores;

            Text etiqueta = botonSalir.GetComponentInChildren<Text>(true);
            if (etiqueta != null)
            {
                etiqueta.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                etiqueta.fontSize = 15;
                etiqueta.fontStyle = FontStyle.Bold;
                etiqueta.alignment = TextAnchor.MiddleCenter;
                etiqueta.color = Color.white;
                etiqueta.text = "Salir (Esc)";
                etiqueta.raycastTarget = false;
            }
        }

        private void ConfigurarArrastreDestapador(Button boton)
        {
            EventTrigger trigger = boton.GetComponent<EventTrigger>();
            if (trigger == null) trigger = boton.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null) trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
            trigger.triggers.Clear();

            AgregarEvento(trigger, EventTriggerType.PointerDown, data => ComenzarBombeo((PointerEventData)data));
            AgregarEvento(trigger, EventTriggerType.Drag, data => MoverDestapador((PointerEventData)data));
            AgregarEvento(trigger, EventTriggerType.PointerUp, _ => TerminarBombeo());
        }

        private static void AgregarEvento(EventTrigger trigger, EventTriggerType tipo, System.Action<BaseEventData> accion)
        {
            EventTrigger.Entry entrada = new EventTrigger.Entry { eventID = tipo };
            entrada.callback.AddListener(data => accion(data));
            trigger.triggers.Add(entrada);
        }

        private void ComenzarBombeo(PointerEventData evento)
        {
            if (completado || !tieneDestapador || rectDestapador == null || rectPadreDestapador == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectPadreDestapador, evento.position, ObtenerCamaraCanvas(), out posicionInicialPuntero))
                return;

            arrastrandoDestapador = true;
            posicionInicialDestapador = rectDestapador.anchoredPosition;
            ultimaPosicionDestapadorY = posicionInicialDestapador.y;
        }

        private void MoverDestapador(PointerEventData evento)
        {
            if (!arrastrandoDestapador || completado || rectDestapador == null || rectPadreDestapador == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectPadreDestapador, evento.position, ObtenerCamaraCanvas(), out Vector2 posicionPuntero))
                return;

            float posicionY = Mathf.Clamp(
                posicionInicialDestapador.y + posicionPuntero.y - posicionInicialPuntero.y,
                Mathf.Max(posicionCentroDestapador.y - desplazamientoMaximoBombeo, ObtenerLimiteInferiorDestapador()),
                posicionCentroDestapador.y + desplazamientoMaximoBombeo);
            rectDestapador.anchoredPosition = new Vector2(posicionInicialDestapador.x, posicionY);

            float distanciaRecorrida = Mathf.Abs(posicionY - ultimaPosicionDestapadorY);
            ultimaPosicionDestapadorY = posicionY;
            if (distanciaRecorrida <= 0f) return;

            seMovioEsteFrame = true;
            if (barraDestape != null)
            {
                float distanciaPorBombeoCompleto = Mathf.Max(1f, desplazamientoMaximoBombeo * 2f);
                barraDestape.value += progresoPorBombeo * distanciaRecorrida / distanciaPorBombeoCompleto;
                if (barraDestape.value >= barraDestape.maxValue)
                {
                    barraDestape.value = barraDestape.maxValue;
                    completado = true;
                    arrastrandoDestapador = false;
                    StartCoroutine(FinalDestapar());
                }
            }

            duracionEfectoAgua = 0.18f;
            if (efectoAgua != null) efectoAgua.SetActive(true);
            tiempoEntreParticulas -= Time.deltaTime;
            if (tiempoEntreParticulas <= 0f)
            {
                EmitirParticulasAgua();
                tiempoEntreParticulas = 0.08f;
            }
        }

        private void TerminarBombeo()
        {
            arrastrandoDestapador = false;
        }

        private Camera ObtenerCamaraCanvas()
        {
            return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
        }

        private float ObtenerLimiteInferiorDestapador()
        {
            if (imagenInodoro == null || rectDestapador == null || rectPadreDestapador == null)
                return posicionCentroDestapador.y - desplazamientoMaximoBombeo;

            imagenInodoro.rectTransform.GetWorldCorners(esquinasUI);
            float bordeInferiorInodoro = Mathf.Min(
                rectPadreDestapador.InverseTransformPoint(esquinasUI[0]).y,
                rectPadreDestapador.InverseTransformPoint(esquinasUI[3]).y);

            rectDestapador.GetWorldCorners(esquinasUI);
            float mitadAltoDestapador = Mathf.Abs(
                rectPadreDestapador.InverseTransformPoint(esquinasUI[1]).y
                - rectPadreDestapador.InverseTransformPoint(esquinasUI[0]).y) * 0.5f;

            return bordeInferiorInodoro + mitadAltoDestapador + margenInferiorDestapador;
        }

        private void PrepararParticulasAgua()
        {
            if (rectPanel == null || efectoAgua == null || particulasAgua.Count > 0) return;
            Image imagenFuente = efectoAgua.GetComponent<Image>();
            if (imagenFuente == null || imagenFuente.sprite == null) return;

            for (int i = 0; i < 18; i++)
            {
                GameObject objeto = new GameObject("GotaAgua", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                objeto.transform.SetParent(rectPanel, false);
                RectTransform rect = objeto.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                Image imagen = objeto.GetComponent<Image>();
                imagen.sprite = imagenFuente.sprite;
                imagen.preserveAspect = true;
                imagen.raycastTarget = false;
                objeto.SetActive(false);
                particulasAgua.Add(new ParticulaAguaUI { rect = rect, imagen = imagen });
            }
        }

        private void EmitirParticulasAgua()
        {
            if (imagenInodoro == null || rectPanel == null || particulasAgua.Count == 0) return;

            Camera camara = ObtenerCamaraCanvas();
            Vector2 posicionPantalla = RectTransformUtility.WorldToScreenPoint(camara, imagenInodoro.rectTransform.position);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectPanel, posicionPantalla, camara, out Vector2 centro))
                return;

            for (int i = 0; i < 3; i++)
            {
                ParticulaAguaUI particula = particulasAgua.Find(elemento => !elemento.rect.gameObject.activeSelf);
                if (particula == null) return;

                particula.rect.anchoredPosition = centro + new Vector2(Random.Range(-38f, 38f), Random.Range(0f, 18f));
                particula.rect.sizeDelta = Vector2.one * Random.Range(12f, 22f);
                particula.rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-30f, 30f));
                particula.imagen.color = new Color(0.45f, 0.83f, 1f, 1f);
                particula.velocidad = new Vector2(Random.Range(-100f, 100f), Random.Range(110f, 220f));
                particula.edad = 0f;
                particula.duracion = Random.Range(0.35f, 0.65f);
                particula.rect.SetAsLastSibling();
                particula.rect.gameObject.SetActive(true);
            }
        }

        private void ActualizarParticulasAgua()
        {
            foreach (ParticulaAguaUI particula in particulasAgua)
            {
                if (!particula.rect.gameObject.activeSelf) continue;

                particula.edad += Time.deltaTime;
                if (particula.edad >= particula.duracion)
                {
                    particula.rect.gameObject.SetActive(false);
                    continue;
                }

                particula.rect.anchoredPosition += particula.velocidad * Time.deltaTime;
                particula.velocidad += Vector2.down * (380f * Time.deltaTime);
                Color color = particula.imagen.color;
                color.a = 1f - particula.edad / particula.duracion;
                particula.imagen.color = color;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Salir();

            ActualizarParticulasAgua();

            if (!seMovioEsteFrame && !completado && barraDestape != null)
            {
                barraDestape.value = Mathf.MoveTowards(
                    barraDestape.value, 0f, perdidaProgresoPorSegundo * Time.deltaTime);
            }

            if (duracionEfectoAgua > 0f)
            {
                duracionEfectoAgua -= Time.deltaTime;
                if (duracionEfectoAgua <= 0f && efectoAgua != null)
                    efectoAgua.SetActive(false);
            }

            seMovioEsteFrame = false;
        }

        private IEnumerator FinalDestapar()
        {
            if (botonBombear != null) botonBombear.interactable = false;
            if (botonSalir != null) botonSalir.interactable = true;

            // Aparece el sorete flotando
            if (imagenSoreteFlotante != null)
                imagenSoreteFlotante.gameObject.SetActive(true);

            // Suma impacto de tarea normal (el baño no se arruina)
            if (EstadoPartida.Instancia != null)
                EstadoPartida.Instancia.RegistrarSabotaje(1, 0);

            CompletarTareaNivel();
            Inventario?.QuitarItem(idDestapador);

            DatosItem sorete = Resources.Load<DatosItem>("Items/" + idSoreteResultante);
            bool agregadoAlInventario = sorete != null
                && Inventario != null
                && Inventario.IntentarAgregarItem(sorete);

            if (!agregadoAlInventario && prefabSoreteRecogible != null)
            {
                CrearSoreteEnElSuelo();
            }
            else if (!agregadoAlInventario)
            {
                Debug.LogWarning($"[MinijuegoDestaparBaño] No se pudo crear el resultado '{idSoreteResultante}'.", this);
            }

            yield return new WaitForSecondsRealtime(1.5f);
            if (panelRaiz != null && panelRaiz.activeInHierarchy)
                Salir();
        }

        private void CrearSoreteEnElSuelo()
        {
            Transform jugador = Inventario != null ? Inventario.transform : GameObject.FindGameObjectWithTag("Player")?.transform;
            if (jugador == null)
            {
                soreteMundoInstancia = Instantiate(prefabSoreteRecogible);
                soreteMundoInstancia.transform.position = Vector3.zero;
                return;
            }

            Transform camara = Camera.main != null ? Camera.main.transform : jugador;
            Vector3 direccion = camara.forward;
            direccion.y = 0f;
            if (direccion.sqrMagnitude < 0.001f) direccion = jugador.forward;
            direccion.Normalize();

            Vector3 posicion = jugador.position + direccion * 1.25f + Vector3.up * 0.1f;
            Vector3 origenRayo = posicion + Vector3.up * 2f;
            if (Physics.Raycast(origenRayo, Vector3.down, out RaycastHit impacto,
                    6f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (impacto.transform != jugador && !impacto.transform.IsChildOf(jugador))
                    posicion = impacto.point + Vector3.up * 0.1f;
            }

            soreteMundoInstancia = Instantiate(prefabSoreteRecogible);
            soreteMundoInstancia.transform.position = posicion;
        }

        public override void Reiniciar()
        {
            StopAllCoroutines();
            if (soreteMundoInstancia != null)
            {
                Destroy(soreteMundoInstancia);
                soreteMundoInstancia = null;
            }
            completado = false;
            arrastrandoDestapador = false;
            seMovioEsteFrame = false;
            if (barraDestape != null) barraDestape.value = 0f;
            if (imagenSoreteFlotante != null) imagenSoreteFlotante.gameObject.SetActive(false);
            if (efectoAgua != null) efectoAgua.SetActive(false);
            if (botonSalir != null) botonSalir.interactable = true;
            if (botonBombear != null)
                botonBombear.interactable = modoPrueba || (Inventario != null && Inventario.TieneItem(idDestapador));
            if (rectDestapador != null) rectDestapador.anchoredPosition = posicionCentroDestapador;
            foreach (ParticulaAguaUI particula in particulasAgua)
                particula.rect.gameObject.SetActive(false);
        }

        protected override void AlSalir()
        {
            StopAllCoroutines();
            arrastrandoDestapador = false;
            if (efectoAgua != null) efectoAgua.SetActive(false);
            foreach (ParticulaAguaUI particula in particulasAgua)
                particula.rect.gameObject.SetActive(false);
        }

    }
}