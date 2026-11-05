using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoltaLaPala.Inventory;

namespace SoltaLaPala.Minijuegos
{
    public class MinijuegoCafetera : MinijuegoBase
    {
        private sealed class ParticulaUI
        {
            public RectTransform rect;
            public Image imagen;
            public Vector2 velocidad;
            public Vector2 gravedad;
            public float edad;
            public float duracion;
        }

        [Header("IDs de ítems necesarios y resultados")]
        public string idTaza = "TazaCafe";
        public string idSalero = "Salero";
        public string idCafeListo = "CafeListo";
        public string idCafeListoConSal = "CafeListoConSal";

        [Header("Fondo UI")]
        public Image imagenFondo;

        [Header("UI - Botones")]
        public Button botonCafe;
        public Button botonSal;
        public Button botonSalir;

        [Header("UI - Barra de llenado")]
        public Slider barraLlenado;

        [Header("UI - Emisores de partículas")]
        public GameObject particulasCafe;
        public GameObject particulasSal;

        [Header("Ajustes")]
        public float velocidadDeLlenado = 0.2f;
        public int sospechaSabotaje = 5;

        private readonly List<ParticulaUI> gotasCafe = new List<ParticulaUI>();
        private readonly List<ParticulaUI> granosSal = new List<ParticulaUI>();
        private static Sprite spriteGotaCafe;
        private Image imagenTazaUI;
        private Text textoEstado;
        private bool cayendoCafe;
        private bool tieneSal;
        private bool sabotajeSal;
        private bool tazaEnCafetera;
        private bool cafePreparado;
        private bool completado;
        private float acumuladorCafe;
        private float acumuladorSal;

        public bool Completado => completado;
        public bool TazaEnCafetera => tazaEnCafetera;

        protected override void AlIniciar()
        {
            if (completado)
            {
                Salir();
                return;
            }

            bool tieneTaza = Inventario != null && Inventario.TieneItem(idTaza);
            tieneSal = Inventario != null && Inventario.TieneItem(idSalero);
            cayendoCafe = false;
            acumuladorSal = 0f;

            if (!tieneTaza && !tazaEnCafetera)
            {
                Debug.LogWarning("[MinijuegoCafetera] Necesitas una taza para preparar cafe.");
                Salir();
                return;
            }

            Debug.Log($"[MinijuegoCafetera] Iniciado. TieneTaza: {tieneTaza}, TieneSal: {tieneSal}");

            if (barraLlenado == null) Debug.LogWarning("[MinijuegoCafetera] Falta asignar 'Barra Llenado'.");
            if (botonCafe == null) Debug.LogWarning("[MinijuegoCafetera] Falta asignar 'Boton Cafe'.");

            if (barraLlenado != null)
            {
                barraLlenado.maxValue = 1f;
                barraLlenado.value = cafePreparado ? 1f : barraLlenado.value;
            }

            if (botonCafe != null)
            {
                botonCafe.interactable = !cayendoCafe;
                botonCafe.onClick.RemoveAllListeners();
                botonCafe.onClick.AddListener(PresionarCafetera);
            }

            if (botonSal != null)
            {
                botonSal.gameObject.SetActive(tieneSal);
                botonSal.interactable = tieneSal && !sabotajeSal;
                botonSal.onClick.RemoveAllListeners();
                ArrastrarSaleroUI arrastre = botonSal.GetComponent<ArrastrarSaleroUI>();
                if (arrastre == null) arrastre = botonSal.gameObject.AddComponent<ArrastrarSaleroUI>();
                arrastre.Configurar(this);
            }

            if (botonSalir != null)
            {
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
            }

            if (particulasCafe != null)
            {
                Image imagenEmisor = particulasCafe.GetComponent<Image>();
                if (imagenEmisor != null)
                {
                    imagenEmisor.enabled = false;
                    imagenEmisor.raycastTarget = false;
                }
                particulasCafe.SetActive(true);
            }

            if (particulasSal != null)
            {
                Image imagenEmisor = particulasSal.GetComponent<Image>();
                if (imagenEmisor != null)
                {
                    imagenEmisor.enabled = false;
                    imagenEmisor.raycastTarget = false;
                }
                particulasSal.SetActive(true);
            }

            PrepararPool(particulasCafe, gotasCafe, 16, new Vector2(7f, 10f),
                new Color(0.24f, 0.12f, 0.06f, 1f));
            PrepararPool(particulasSal, granosSal, 12, new Vector2(4f, 4f), Color.white);
            DesactivarParticulas(gotasCafe);
            DesactivarParticulas(granosSal);
            PrepararTazaUI();
            PrepararTextoEstado();
            ActualizarTazaUI();
            ActualizarTextoEstado();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Salir();

            float deltaTiempo = Time.deltaTime;
            ActualizarParticulas(gotasCafe, deltaTiempo);
            ActualizarParticulas(granosSal, deltaTiempo);

            if (cayendoCafe && barraLlenado != null)
            {
                barraLlenado.value += velocidadDeLlenado * deltaTiempo;
                acumuladorCafe += deltaTiempo * 18f;
                while (acumuladorCafe >= 1f)
                {
                    EmitirParticula(gotasCafe, particulasCafe, false);
                    acumuladorCafe -= 1f;
                }

                if (barraLlenado.value >= 1f)
                {
                    barraLlenado.value = 1f;
                    cayendoCafe = false;
                    cafePreparado = true;
                    if (botonCafe != null) botonCafe.interactable = true;
                    ActualizarTazaUI();
                    ActualizarTextoEstado();
                }
            }

            if (sabotajeSal && acumuladorSal > 0f)
            {
                acumuladorSal -= deltaTiempo;
                EmitirParticula(granosSal, particulasSal, true);
            }
            else if (sabotajeSal && particulasSal != null
                && !granosSal.Exists(elemento => elemento.rect.gameObject.activeSelf))
            {
                particulasSal.SetActive(false);
            }
        }

        public void PresionarCafetera()
        {
            if (completado || cayendoCafe || Inventario == null)
            {
                return;
            }

            if (!tazaEnCafetera)
            {
                if (!Inventario.TieneItem(idTaza))
                {
                    Debug.LogWarning("[MinijuegoCafetera] Necesitas una taza para preparar cafe.");
                    return;
                }

                if (!Inventario.QuitarItem(idTaza))
                {
                    Debug.LogError("[MinijuegoCafetera] No se pudo colocar la taza en la cafetera.");
                    return;
                }

                tazaEnCafetera = true;
                ActualizarTazaUI();
                ActualizarTextoEstado();
                return;
            }

            if (cafePreparado)
            {
                Completar();
                return;
            }

            cayendoCafe = true;
            if (botonCafe != null) botonCafe.interactable = false;
            if (particulasCafe != null) particulasCafe.SetActive(true);
            ActualizarTextoEstado();
        }

        internal bool IntentarAgregarSal(Vector2 posicionPantalla, Camera camaraEvento)
        {
            if (completado || !tazaEnCafetera || !cafePreparado || sabotajeSal
                || !tieneSal || Inventario == null || !Inventario.TieneItem(idSalero)
                || imagenTazaUI == null
                || !RectTransformUtility.RectangleContainsScreenPoint(
                    imagenTazaUI.rectTransform, posicionPantalla, camaraEvento))
            {
                return false;
            }

            sabotajeSal = true;
            acumuladorSal = 0.7f;
            if (particulasSal != null)
            {
                RectTransform emisor = particulasSal.transform as RectTransform;
                if (emisor != null)
                {
                    emisor.anchoredPosition = imagenTazaUI.rectTransform.anchoredPosition
                        + Vector2.up * 18f;
                }
                particulasSal.SetActive(true);
            }
            if (botonSal != null) botonSal.interactable = false;
            ActualizarTextoEstado();
            return true;
        }

        private void PrepararPool(GameObject emisor, List<ParticulaUI> pool, int cantidad,
            Vector2 tamano, Color color)
        {
            if (emisor == null || pool.Count > 0)
            {
                return;
            }

            for (int i = 0; i < cantidad; i++)
            {
                GameObject objeto = new GameObject("Particula", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                objeto.transform.SetParent(emisor.transform, false);

                RectTransform rect = objeto.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = tamano;

                Image imagen = objeto.GetComponent<Image>();
                imagen.color = color;
                imagen.sprite = ObtenerSpriteGotaCafe();
                imagen.raycastTarget = false;
                objeto.SetActive(false);
                pool.Add(new ParticulaUI { rect = rect, imagen = imagen });
            }
        }

        private static void DesactivarParticulas(List<ParticulaUI> pool)
        {
            foreach (ParticulaUI particula in pool)
            {
                particula.rect.gameObject.SetActive(false);
                Color color = particula.imagen.color;
                color.a = 1f;
                particula.imagen.color = color;
            }
        }

        private void EmitirParticula(List<ParticulaUI> pool, GameObject emisor, bool esSal)
        {
            if (emisor == null || pool.Count == 0)
            {
                return;
            }

            ParticulaUI particula = pool.Find(elemento => !elemento.rect.gameObject.activeSelf);
            if (particula == null)
            {
                return;
            }

            RectTransform rectEmisor = emisor.transform as RectTransform;
            if (rectEmisor == null)
            {
                return;
            }

            particula.rect.anchoredPosition = esSal
                ? new Vector2(Random.Range(-8f, 8f), Random.Range(-4f, 4f))
                : new Vector2(Random.Range(-4f, 4f), rectEmisor.rect.height * 0.45f);
            if (!esSal)
            {
                particula.rect.sizeDelta = new Vector2(Random.Range(5f, 8f), Random.Range(7f, 11f));
                particula.imagen.color = new Color(
                    Random.Range(0.22f, 0.34f), Random.Range(0.07f, 0.13f), 0.025f, 1f);
            }
            particula.velocidad = esSal
                ? new Vector2(Random.Range(-25f, 25f), Random.Range(65f, 125f))
                : new Vector2(Random.Range(-5f, 5f), Random.Range(-210f, -150f));
            particula.gravedad = esSal ? new Vector2(0f, -150f) : Vector2.zero;
            particula.duracion = esSal ? 0.65f : 0.5f;
            particula.edad = 0f;
            Color color = particula.imagen.color;
            color.a = 1f;
            particula.imagen.color = color;
            particula.rect.gameObject.SetActive(true);
        }

        private static Sprite ObtenerSpriteGotaCafe()
        {
            if (spriteGotaCafe != null) return spriteGotaCafe;

            const int resolucion = 16;
            Texture2D textura = new Texture2D(resolucion, resolucion, TextureFormat.RGBA32, false);
            textura.name = "GotaCafe";
            textura.filterMode = FilterMode.Bilinear;
            textura.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < resolucion; y++)
            {
                for (int x = 0; x < resolucion; x++)
                {
                    float dx = (x + 0.5f - resolucion * 0.5f) / (resolucion * 0.47f);
                    float dy = (y + 0.5f - resolucion * 0.5f) / (resolucion * 0.47f);
                    textura.SetPixel(x, y, dx * dx + dy * dy <= 1f ? Color.white : Color.clear);
                }
            }

            textura.Apply(false, true);
            spriteGotaCafe = Sprite.Create(textura,
                new Rect(0f, 0f, resolucion, resolucion), new Vector2(0.5f, 0.5f), resolucion);
            return spriteGotaCafe;
        }

        private void PrepararTazaUI()
        {
            if (imagenTazaUI != null || panelRaiz == null) return;

            GameObject objeto = new GameObject("Taza en cafetera",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            objeto.transform.SetParent(panelRaiz.transform, false);

            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(46f, 46f);
            RectTransform emisor = particulasCafe != null
                ? particulasCafe.transform as RectTransform
                : null;
            rect.anchoredPosition = emisor != null
                ? emisor.anchoredPosition + Vector2.down * 55f
                : new Vector2(-60f, -70f);

            imagenTazaUI = objeto.GetComponent<Image>();
            imagenTazaUI.preserveAspect = true;
            imagenTazaUI.raycastTarget = false;
            imagenTazaUI.gameObject.SetActive(false);
        }

        private void PrepararTextoEstado()
        {
            if (textoEstado != null || panelRaiz == null) return;

            GameObject objeto = new GameObject("Instrucciones cafetera",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            objeto.transform.SetParent(panelRaiz.transform, false);

            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 14f);
            rect.sizeDelta = new Vector2(790f, 48f);

            textoEstado = objeto.GetComponent<Text>();
            textoEstado.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textoEstado.fontSize = 17;
            textoEstado.alignment = TextAnchor.MiddleCenter;
            textoEstado.horizontalOverflow = HorizontalWrapMode.Wrap;
            textoEstado.verticalOverflow = VerticalWrapMode.Truncate;
            textoEstado.color = new Color(0.12f, 0.1f, 0.08f, 1f);
            textoEstado.raycastTarget = false;
            Outline contorno = objeto.AddComponent<Outline>();
            contorno.effectColor = new Color(1f, 1f, 1f, 0.85f);
            contorno.effectDistance = Vector2.one;
        }

        private void ActualizarTazaUI()
        {
            if (imagenTazaUI == null) return;

            DatosItem item = Resources.Load<DatosItem>(
                "Items/" + (cafePreparado ? idCafeListo : idTaza));
            if (item == null || item.icono == null)
            {
                Debug.LogError("[MinijuegoCafetera] Falta el sprite del item mostrado en la cafetera.");
                return;
            }

            imagenTazaUI.sprite = item.icono;
            imagenTazaUI.gameObject.SetActive(tazaEnCafetera);
        }

        private void ActualizarTextoEstado()
        {
            if (textoEstado == null) return;

            if (!tazaEnCafetera)
            {
                textoEstado.text = "Pulsa la cafetera para colocar la taza.";
            }
            else if (!cafePreparado && !cayendoCafe)
            {
                textoEstado.text = "Taza colocada. Pulsa la cafetera para preparar el café.";
            }
            else if (!cafePreparado)
            {
                textoEstado.text = "Preparando el café...";
            }
            else if (sabotajeSal)
            {
                textoEstado.text = "Sal agregada. Pulsa la cafetera para retirar la taza.";
            }
            else if (tieneSal)
            {
                textoEstado.text = "Pasa el salero por encima del café o retira la taza.";
            }
            else
            {
                textoEstado.text = "Café listo. Pulsa la cafetera para retirar la taza.";
            }
        }

        private static void ActualizarParticulas(List<ParticulaUI> pool, float deltaTiempo)
        {
            foreach (ParticulaUI particula in pool)
            {
                if (!particula.rect.gameObject.activeSelf)
                {
                    continue;
                }

                particula.edad += deltaTiempo;
                if (particula.edad >= particula.duracion)
                {
                    particula.rect.gameObject.SetActive(false);
                    continue;
                }

                particula.rect.anchoredPosition += particula.velocidad * deltaTiempo;
                particula.velocidad += particula.gravedad * deltaTiempo;
                Color color = particula.imagen.color;
                color.a = 1f - particula.edad / particula.duracion;
                particula.imagen.color = color;
            }
        }

        private void Completar()
        {
            if (Inventario == null || !tazaEnCafetera)
            {
                Debug.LogError("[MinijuegoCafetera] No se encontro la taza dentro de la cafetera.");
                if (botonCafe != null) botonCafe.interactable = true;
                if (barraLlenado != null) barraLlenado.value = 0f;
                return;
            }

            string idResultado = sabotajeSal ? idCafeListoConSal : idCafeListo;
            DatosItem taza = Resources.Load<DatosItem>("Items/" + idTaza);
            DatosItem cafeListo = Resources.Load<DatosItem>("Items/" + idResultado);
            if (taza == null || cafeListo == null)
            {
                Debug.LogError($"[MinijuegoCafetera] No se encontro Resources/Items/{idTaza} o {idResultado}.");
                if (botonCafe != null) botonCafe.interactable = true;
                if (barraLlenado != null) barraLlenado.value = 0f;
                return;
            }

            if (sabotajeSal && !Inventario.TieneItem(idSalero))
            {
                Debug.LogError("[MinijuegoCafetera] Se agrego sal sin conservar el salero en el inventario.");
                return;
            }

            bool salRetirada = sabotajeSal && Inventario.QuitarItem(idSalero);
            if (sabotajeSal && !salRetirada)
            {
                Debug.LogError("[MinijuegoCafetera] No se pudo consumir el salero.");
                return;
            }

            if (!Inventario.IntentarAgregarItem(cafeListo))
            {
                if (salRetirada
                    && !Inventario.IntentarAgregarItem(Resources.Load<DatosItem>("Items/" + idSalero)))
                {
                    Debug.LogError("[MinijuegoCafetera] No se pudo devolver el salero al inventario.");
                }
                Debug.LogError("[MinijuegoCafetera] No se pudo guardar el cafe listo; la taza sigue en la cafetera.");
                return;
            }

            completado = true;
            tazaEnCafetera = false;
            cafePreparado = false;
            sabotajeSal = false;
            Salir();
        }

        protected override void AlSalir()
        {
            cayendoCafe = false;
            acumuladorSal = 0f;
            if (particulasCafe != null) particulasCafe.SetActive(false);
            if (particulasSal != null) particulasSal.SetActive(false);
            DesactivarParticulas(gotasCafe);
            DesactivarParticulas(granosSal);
        }

        public override void Reiniciar()
        {
            completado = false;
            cayendoCafe = false;
            sabotajeSal = false;
            tazaEnCafetera = false;
            cafePreparado = false;
            acumuladorCafe = 0f;
            acumuladorSal = 0f;
            if (barraLlenado != null) barraLlenado.value = 0f;
            if (particulasCafe != null) particulasCafe.SetActive(false);
            if (particulasSal != null) particulasSal.SetActive(false);
            DesactivarParticulas(gotasCafe);
            DesactivarParticulas(granosSal);
            ActualizarTazaUI();
            ActualizarTextoEstado();
        }
    }
}
