using System.Collections.Generic;
using SoltaLaPala.Inventory;
using SoltaLaPala.Menus;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Minijuegos
{
    public class MinijuegoMicroondas : MinijuegoBase
    {
        [Header("IDs de ítems")]
        public string idComida = "Sandwich";
        public string idTenedor = "Tenedor";
        public string idComidaCaliente = "SandwichCaliente";

        [Header("Fondo UI")]
        public Image imagenFondo;

        [Header("UI")]
        public Button botonEncender;
        public Button botonTenedor;
        public Button botonSalir;
        public Slider barraCalentado;
        public GameObject particulasHumo;

        [Header("Ajustes")]
        public float velocidadCalentado = 0.15f;
        [Tooltip("Activa esto para probar el minijuego sin requerir tener sándwich o tenedor en el inventario")]
        public bool modoPrueba = false;
        public float desplazamientoPuertaAbierta = -250f;

        private readonly Dictionary<string, ArrastrableUI> iconosInventario =
            new Dictionary<string, ArrastrableUI>();

        private Image imagenPuerta;
        private Button botonPuerta;
        private RectTransform rectPuerta;
        private Vector2 posicionPuertaCerrada;
        private Vector3 escalaPuertaCerrada;
        private Image imagenBarra;
        private Color colorFondoOriginal;
        private Color colorBarraOriginal;
        private ZonaObjetivoUI zonaInterior;
        private ZonaObjetivoUI zonaSalida;
        private ArrastrableUI objetoDentro;
        private string idObjetoDentro;
        private Text textoEstado;
        private bool puertaAbierta;
        private bool calentando;
        private bool puertaRecibeRaycastsDuranteArrastre;

        protected override void AlIniciar()
        {
            LimpiarInterfazDinamica();

            Transform puerta = transform.Find("PuertaMicroondas");
            imagenPuerta = puerta != null ? puerta.GetComponent<Image>() : null;
            rectPuerta = puerta as RectTransform;
            Transform baseMicroondas = transform.Find("MicroondasAbierto");
            Image imagenBase = baseMicroondas != null ? baseMicroondas.GetComponent<Image>() : null;

            if (imagenPuerta == null || rectPuerta == null)
            {
                Debug.LogError("[MinijuegoMicroondas] Falta la imagen 'PuertaMicroondas' en el panel.", this);
                return;
            }
            if (imagenBase == null)
            {
                Debug.LogError("[MinijuegoMicroondas] Falta la imagen 'MicroondasAbierto' en el panel.", this);
                return;
            }

            imagenBase.raycastTarget = false;
            Transform fondo = transform.Find("FondoCocina");
            if (fondo != null)
            {
                fondo.SetAsFirstSibling();
                imagenBase.transform.SetSiblingIndex(1);
            }
            posicionPuertaCerrada = rectPuerta.anchoredPosition;
            escalaPuertaCerrada = rectPuerta.localScale;
            puertaRecibeRaycastsDuranteArrastre = imagenPuerta.raycastTarget;
            puertaAbierta = false;
            calentando = false;
            idObjetoDentro = string.Empty;
            objetoDentro = null;
            iconosInventario.Clear();

            botonPuerta = imagenPuerta.GetComponent<Button>();
            if (botonPuerta == null)
            {
                botonPuerta = imagenPuerta.gameObject.AddComponent<Button>();
            }
            botonPuerta.targetGraphic = imagenPuerta;
            botonPuerta.transition = Selectable.Transition.None;
            botonPuerta.onClick.RemoveAllListeners();
            botonPuerta.onClick.AddListener(AlternarPuerta);

            if (botonTenedor != null)
            {
                botonTenedor.gameObject.SetActive(false);
            }

            if (barraCalentado == null)
            {
                Debug.LogWarning("[MinijuegoMicroondas] Falta asignar 'Barra Calentado' en el Inspector.", this);
            }
            else
            {
                barraCalentado.value = 0f;
                barraCalentado.maxValue = 1f;
            }

            imagenBarra = barraCalentado != null && barraCalentado.fillRect != null
                ? barraCalentado.fillRect.GetComponent<Image>()
                : null;
            colorFondoOriginal = imagenFondo != null ? imagenFondo.color : Color.white;
            colorBarraOriginal = imagenBarra != null ? imagenBarra.color : Color.white;

            if (botonEncender != null)
            {
                botonEncender.onClick.RemoveAllListeners();
                botonEncender.onClick.AddListener(EncenderMicroondas);
            }
            if (botonSalir != null)
            {
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(IntentarSalir);
            }
            if (particulasHumo != null)
            {
                particulasHumo.SetActive(false);
            }

            CrearZonasArrastre(imagenBase);
            CrearTextoEstado();
            CrearIconosIniciales();
            MantenerPuertaAlFrente();
            ActualizarControles();
            ActualizarTextoEstado();
        }

        private void MantenerPuertaAlFrente()
        {
            if (rectPuerta != null)
            {
                rectPuerta.SetAsLastSibling();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                IntentarSalir();
            }

            if (!calentando || barraCalentado == null)
            {
                return;
            }

            float pulso = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            float velocidadActual = velocidadCalentado * Mathf.Lerp(0.7f, 1.3f, pulso);
            barraCalentado.value += velocidadActual * Time.deltaTime;
            ActualizarEfectosCalentado(pulso);

            if (barraCalentado.value >= 1f)
            {
                barraCalentado.value = 1f;
                calentando = false;
                if (particulasHumo != null)
                {
                    particulasHumo.SetActive(false);
                }
                CompletarCalentado();
            }
        }

        public void EncenderMicroondas()
        {
            if (calentando || puertaAbierta || objetoDentro == null
                || (idObjetoDentro != idComida && idObjetoDentro != idTenedor))
            {
                return;
            }

            if (barraCalentado != null)
            {
                barraCalentado.value = 0f;
            }
            if (imagenBarra != null)
            {
                imagenBarra.color = colorBarraOriginal;
            }
            if (imagenFondo != null)
            {
                imagenFondo.color = colorFondoOriginal;
            }

            calentando = true;
            if (botonEncender != null)
            {
                botonEncender.interactable = false;
            }
            if (botonPuerta != null)
            {
                botonPuerta.interactable = false;
            }
            if (objetoDentro != null)
            {
                objetoDentro.Bloqueado = true;
            }
            if (particulasHumo != null && idObjetoDentro == idTenedor)
            {
                particulasHumo.SetActive(true);
            }
            ActualizarTextoEstado();
        }

        private void AlternarPuerta()
        {
            if (calentando)
            {
                return;
            }

            puertaAbierta = !puertaAbierta;
            rectPuerta.anchoredPosition = puertaAbierta
                ? posicionPuertaCerrada + Vector2.right * desplazamientoPuertaAbierta
                : posicionPuertaCerrada;
            rectPuerta.localScale = puertaAbierta
                ? new Vector3(-Mathf.Abs(escalaPuertaCerrada.x), escalaPuertaCerrada.y, escalaPuertaCerrada.z)
                : escalaPuertaCerrada;

            ActualizarControles();
            ActualizarTextoEstado();
            MantenerPuertaAlFrente();
        }

        private void CrearZonasArrastre(Image imagenBase)
        {
            GameObject objetoInterior = CrearZona("ZonaInteriorMicroondas",
                imagenBase.transform, new Vector2(-85f, -5f), new Vector2(220f, 175f));
            zonaInterior = objetoInterior.GetComponent<ZonaObjetivoUI>();
            zonaInterior.AlSoltarObjeto += AlSoltarEnInterior;

            GameObject objetoSalida = CrearZona("ZonaSalidaMicroondas",
                transform, new Vector2(350f, 0f), new Vector2(180f, 280f));
            zonaSalida = objetoSalida.GetComponent<ZonaObjetivoUI>();
            zonaSalida.AlSoltarObjeto += AlSoltarEnSalida;
        }

        private static GameObject CrearZona(string nombre, Transform padre, Vector2 posicion, Vector2 tamano)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            objeto.transform.SetParent(padre, false);
            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;

            Image imagen = objeto.GetComponent<Image>();
            imagen.color = new Color(1f, 1f, 1f, 0f);
            imagen.raycastTarget = true;
            objeto.AddComponent<ZonaObjetivoUI>();
            return objeto;
        }

        private void CrearTextoEstado()
        {
            GameObject objeto = new GameObject("TextoEstadoMicroondas",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            objeto.transform.SetParent(transform, false);
            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -205f);
            rect.sizeDelta = new Vector2(800f, 36f);

            textoEstado = objeto.GetComponent<Text>();
            textoEstado.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoEstado.fontSize = 18;
            textoEstado.alignment = TextAnchor.MiddleCenter;
            textoEstado.color = Color.black;
            textoEstado.raycastTarget = false;
        }

        private void CrearIconosIniciales()
        {
            if (modoPrueba)
            {
                DatosItem comidaPrueba = ResolverItem(idComida);
                DatosItem tenedorPrueba = ResolverItem(idTenedor);
                if (comidaPrueba != null) CrearIcono(comidaPrueba);
                if (tenedorPrueba != null) CrearIcono(tenedorPrueba);
                return;
            }

            CrearIconoSiEstaEnInventario(idComida);
            CrearIconoSiEstaEnInventario(idTenedor);
            CrearIconoSiEstaEnInventario(idComidaCaliente);
        }

        private void CrearIconoSiEstaEnInventario(string id)
        {
            if (Inventario != null && Inventario.TieneItem(id))
            {
                DatosItem item = ResolverItem(id);
                if (item != null)
                {
                    CrearIcono(item);
                }
            }
        }

        private DatosItem ResolverItem(string id)
        {
            DatosItem item = Resources.Load<DatosItem>($"Items/{id}");
            if (item == null)
            {
                Debug.LogError($"[MinijuegoMicroondas] No se encontró el item 'Items/{id}'.", this);
            }
            else if (item.icono == null)
            {
                Debug.LogError($"[MinijuegoMicroondas] El item '{id}' no tiene sprite de inventario asignado.", item);
                return null;
            }
            return item;
        }

        private void CrearIcono(DatosItem item)
        {
            if (iconosInventario.ContainsKey(item.Id))
            {
                return;
            }

            GameObject objeto = new GameObject($"Arrastrable_{item.Id}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(ArrastrableUI));
            objeto.transform.SetParent(transform, false);

            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(80f, 80f);

            Image imagen = objeto.GetComponent<Image>();
            imagen.sprite = item.icono;
            imagen.preserveAspect = true;
            imagen.raycastTarget = true;

            ArrastrableUI arrastrable = objeto.GetComponent<ArrastrableUI>();
            arrastrable.idTipo = item.Id;
            arrastrable.AlComenzarArrastre += AlComenzarArrastre;
            arrastrable.AlSoltar += AlTerminarArrastre;
            iconosInventario.Add(item.Id, arrastrable);
            PosicionarIconosInventario();
        }

        private void PosicionarIconosInventario()
        {
            string[] ids = { idComida, idTenedor, idComidaCaliente };
            int fila = 0;
            foreach (string id in ids)
            {
                if (!iconosInventario.TryGetValue(id, out ArrastrableUI arrastrable)
                    || arrastrable == objetoDentro)
                {
                    continue;
                }

                RectTransform rect = arrastrable.transform as RectTransform;
                rect.SetParent(transform, false);
                rect.anchoredPosition = new Vector2(350f, 75f - fila * 88f);
                fila++;
            }
        }

        private void AlComenzarArrastre(ArrastrableUI arrastrable)
        {
            if (imagenPuerta != null)
            {
                puertaRecibeRaycastsDuranteArrastre = imagenPuerta.raycastTarget;
                imagenPuerta.raycastTarget = false;
            }

            if (arrastrable == objetoDentro)
            {
                arrastrable.volverAlOrigenAlSoltar = true;
            }
        }

        private void AlTerminarArrastre(ArrastrableUI arrastrable)
        {
            if (arrastrable == objetoDentro && arrastrable.volverAlOrigenAlSoltar)
            {
                arrastrable.FijarEnDestino(zonaInterior.transform, Vector2.zero);
            }

            if (imagenPuerta != null)
            {
                imagenPuerta.raycastTarget = puertaRecibeRaycastsDuranteArrastre;
            }
        }

        private void AlSoltarEnInterior(ArrastrableUI arrastrable)
        {
            if (calentando || !puertaAbierta || objetoDentro != null
                || arrastrable == objetoDentro)
            {
                return;
            }

            bool esComida = arrastrable.idTipo == idComida || arrastrable.idTipo == idComidaCaliente;
            bool esTenedorValido = arrastrable.idTipo == idTenedor;
            if (!esComida && !esTenedorValido)
            {
                return;
            }

            if (!modoPrueba && (Inventario == null || !Inventario.QuitarItem(arrastrable.idTipo)))
            {
                return;
            }

            objetoDentro = arrastrable;
            idObjetoDentro = arrastrable.idTipo;
            arrastrable.FijarEnDestino(zonaInterior.transform, Vector2.zero);
            ActualizarControles();
            ActualizarTextoEstado();
        }

        private void AlSoltarEnSalida(ArrastrableUI arrastrable)
        {
            if (calentando || !puertaAbierta || arrastrable != objetoDentro)
            {
                return;
            }

            if (!modoPrueba)
            {
                DatosItem item = ResolverItem(idObjetoDentro);
                if (Inventario == null || item == null || !Inventario.IntentarAgregarItem(item))
                {
                    Debug.LogWarning($"[MinijuegoMicroondas] No se pudo devolver '{idObjetoDentro}' al inventario.", this);
                    arrastrable.FijarEnDestino(zonaInterior.transform, Vector2.zero);
                    return;
                }
            }

            objetoDentro = null;
            idObjetoDentro = string.Empty;
            arrastrable.FijarEnDestino(transform, PosicionDisponible(arrastrable.idTipo));
            ActualizarControles();
            ActualizarTextoEstado();
            MantenerPuertaAlFrente();
        }

        private Vector2 PosicionDisponible(string id)
        {
            string[] ids = { idComida, idTenedor, idComidaCaliente };
            int fila = 0;
            foreach (string idDisponible in ids)
            {
                if (idDisponible == id)
                {
                    return new Vector2(350f, 75f - fila * 88f);
                }
                if (iconosInventario.ContainsKey(idDisponible))
                {
                    fila++;
                }
            }
            return new Vector2(350f, 75f - fila * 88f);
        }

        private void ActualizarControles()
        {
            bool objetoCalentable = idObjetoDentro == idComida || idObjetoDentro == idTenedor;
            if (botonEncender != null)
            {
                botonEncender.interactable = !calentando && !puertaAbierta && objetoDentro != null
                    && objetoCalentable;
            }
            if (botonPuerta != null)
            {
                botonPuerta.interactable = !calentando;
            }
            if (botonSalir != null)
            {
                botonSalir.interactable = !calentando && objetoDentro == null;
            }
            foreach (ArrastrableUI arrastrable in iconosInventario.Values)
            {
                if (arrastrable != null)
                {
                    arrastrable.Bloqueado = calentando;
                }
            }
        }

        private void ActualizarTextoEstado()
        {
            if (textoEstado == null)
            {
                return;
            }

            if (calentando)
            {
                textoEstado.text = "El microondas está funcionando.";
            }
            else if (objetoDentro != null && !puertaAbierta)
            {
                textoEstado.text = idObjetoDentro == idComidaCaliente
                    ? "Abre la puerta y arrastra el sándwich caliente afuera."
                    : "Cierra la puerta y enciende el microondas.";
            }
            else if (objetoDentro != null)
            {
                textoEstado.text = "Arrastra el objeto afuera o cierra la puerta.";
            }
            else if (EstadoPartida.Instancia != null
                && EstadoPartida.Instancia.EstaCompletaTarea(nameof(MinijuegoMicroondas)))
            {
                textoEstado.text = "La comida quedó arruinada. Puedes salir.";
            }
            else
            {
                textoEstado.text = "Abre la puerta y arrastra el sándwich o el tenedor adentro.";
            }
        }

        private void CompletarCalentado()
        {
            if (idObjetoDentro == idTenedor)
            {
                iconosInventario.Remove(idTenedor);
                Destroy(objetoDentro.gameObject);
                objetoDentro = null;
                idObjetoDentro = string.Empty;
                CompletarTareaNivel();
                EstadoPartida.Instancia?.RegistrarSabotaje(
                    3, 8, "Comida de la oficina arruinada");
                IntentarSalir();
                return;
            }
            else if (idObjetoDentro == idComida)
            {
                DatosItem comidaCaliente = ResolverItem(idComidaCaliente);
                if (comidaCaliente == null)
                {
                    return;
                }

                iconosInventario.Remove(idComida);
                idObjetoDentro = idComidaCaliente;
                objetoDentro.idTipo = idComidaCaliente;
                objetoDentro.GetComponent<Image>().sprite = comidaCaliente.icono;
                iconosInventario[idComidaCaliente] = objetoDentro;
            }

            ActualizarControles();
            ActualizarTextoEstado();
        }

        private void IntentarSalir()
        {
            if (calentando)
            {
                return;
            }

            if (objetoDentro != null)
            {
                ActualizarTextoEstado();
                return;
            }

            LimpiarInterfazDinamica();
            base.Salir();
        }

        private void ActualizarEfectosCalentado(float pulso)
        {
            float progreso = barraCalentado != null ? barraCalentado.value : 0f;
            if (imagenBarra != null)
            {
                Color colorCalor = Color.Lerp(
                    new Color(1f, 0.78f, 0.18f, colorBarraOriginal.a),
                    new Color(1f, 0.2f, 0.05f, colorBarraOriginal.a),
                    progreso);
                imagenBarra.color = Color.Lerp(colorCalor, Color.white, pulso * 0.18f);
            }

            if (imagenFondo != null)
            {
                Color colorCalor = new Color(1f, 0.32f, 0.08f, colorFondoOriginal.a);
                imagenFondo.color = Color.Lerp(
                    colorFondoOriginal, colorCalor, progreso * (0.18f + pulso * 0.12f));
            }
        }

        private void LimpiarInterfazDinamica()
        {
            foreach (ArrastrableUI arrastrable in iconosInventario.Values)
            {
                if (arrastrable != null)
                {
                    Destroy(arrastrable.gameObject);
                }
            }
            iconosInventario.Clear();

            if (zonaInterior != null)
            {
                Destroy(zonaInterior.gameObject);
                zonaInterior = null;
            }
            if (zonaSalida != null)
            {
                Destroy(zonaSalida.gameObject);
                zonaSalida = null;
            }
            if (textoEstado != null)
            {
                Destroy(textoEstado.gameObject);
                textoEstado = null;
            }

            if (rectPuerta != null)
            {
                rectPuerta.anchoredPosition = posicionPuertaCerrada;
                rectPuerta.localScale = escalaPuertaCerrada;
            }
            if (imagenFondo != null)
            {
                imagenFondo.color = colorFondoOriginal;
            }
            if (imagenBarra != null)
            {
                imagenBarra.color = colorBarraOriginal;
            }
            if (particulasHumo != null)
            {
                particulasHumo.SetActive(false);
            }
        }

        protected override void AlSalir()
        {
            calentando = false;
            puertaAbierta = false;
            idObjetoDentro = string.Empty;
            objetoDentro = null;
            if (rectPuerta != null)
            {
                rectPuerta.anchoredPosition = posicionPuertaCerrada;
                rectPuerta.localScale = escalaPuertaCerrada;
            }
            if (particulasHumo != null)
            {
                particulasHumo.SetActive(false);
            }
            StopAllCoroutines();
        }
    }
}
