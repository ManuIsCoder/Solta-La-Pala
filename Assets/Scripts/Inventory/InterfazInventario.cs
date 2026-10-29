using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Inventory
{
    public class InterfazInventario : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Si se deja vacio se busca el InventarioJugador de la escena.")]
        public InventarioJugador inventario;
        [Tooltip("Los 3 cuadrados del inventario, en orden 1-2-3. Si estan vacios se generan solos.")]
        public SlotInterfaz[] slots = new SlotInterfaz[InventarioJugador.CantidadSlots];

        [Header("Arte")]
        [Tooltip("Sprite de la barra/marco del inventario. Si esta vacio se carga de Resources.")]
        public Sprite spriteFondoInventario;

        [Header("Ajustes de Barra y Escala")]
        [Tooltip("Escala de la barra. Subila para hacerla mas grande (ej. 1.2, 1.5, 2.0).")]
        [Range(0.5f, 3f)]
        public float escalaBarra = 1.35f;
        [Tooltip("Tamano nativo en pixeles de la imagen del inventario.")]
        public Vector2 tamanoBase = new Vector2(293f, 89f);
        [Tooltip("Margen desde la esquina inferior derecha de la pantalla (0 en X lo pega al extremo derecho).")]
        public Vector2 margen = new Vector2(0f, 30f);
        [Tooltip("Posicion (x, y desde abajo) y tamano en pixeles del sprite de cada recuadro de slot.")]
        public Rect[] recuadrosSlotEnSprite =
        {
            new Rect(63f, 17f, 64f, 64f),
            new Rect(139f, 17f, 64f, 64f),
            new Rect(215f, 17f, 64f, 64f),
        };

        [Header("Generacion automatica (prototipo sin sprite)")]
        [Tooltip("Lado del cuadrado de cada slot, en pixeles.")]
        public float tamanoSlot = 80f;
        [Tooltip("Separacion entre cuadrados, en pixeles.")]
        public float separacion = 10f;
        public Color colorFondo = new Color(0f, 0f, 0f, 0.5f);
        public Color colorSeleccion = new Color(1f, 1f, 1f, 0.9f);

        private RectTransform rectBarra;

        private void Awake()
        {
            if (inventario == null)
            {
                inventario = FindFirstObjectByType<InventarioJugador>();
            }

            if (!SlotsAsignados())
            {
                GenerarInterfaz();
            }
        }

        private void OnEnable()
        {
            if (inventario == null)
            {
                return;
            }

            inventario.AlCambiarSlot += RefrescarSlot;
            inventario.AlCambiarSeleccion += RefrescarSeleccion;

            for (int i = 0; i < InventarioJugador.CantidadSlots; i++)
            {
                RefrescarSlot(i);
            }

            RefrescarSeleccion(inventario.SlotSeleccionado);
        }

        private void OnDisable()
        {
            if (inventario == null)
            {
                return;
            }

            inventario.AlCambiarSlot -= RefrescarSlot;
            inventario.AlCambiarSeleccion -= RefrescarSeleccion;
        }

        private void RefrescarSlot(int indiceSlot)
        {
            if (!EsSlotUsable(indiceSlot))
            {
                return;
            }

            DatosItem item = inventario.ObtenerItem(indiceSlot);
            Image icono = slots[indiceSlot].icono;

            icono.sprite = item != null ? item.icono : null;

            icono.enabled = item != null && item.icono != null;
        }

        private void RefrescarSeleccion(int indiceSeleccionado)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].marcoSeleccion != null)
                {
                    slots[i].marcoSeleccion.SetActive(i == indiceSeleccionado);
                }
            }
        }

        private bool EsSlotUsable(int indiceSlot)
        {
            return inventario != null
                && indiceSlot >= 0
                && indiceSlot < slots.Length
                && slots[indiceSlot] != null
                && slots[indiceSlot].icono != null;
        }

        private bool SlotsAsignados()
        {
            if (slots == null || slots.Length == 0)
            {
                return false;
            }

            foreach (SlotInterfaz slot in slots)
            {
                if (slot == null || slot.icono == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void GenerarInterfaz()
        {
            if (spriteFondoInventario == null)
            {
                spriteFondoInventario = Resources.Load<Sprite>("Sprites/UI/Inventario");
            }

            Canvas canvas = CrearCanvas();
            slots = new SlotInterfaz[InventarioJugador.CantidadSlots];

            if (spriteFondoInventario != null)
            {
                GenerarInterfazConSprite(canvas.transform);
            }
            else
            {
                for (int i = 0; i < InventarioJugador.CantidadSlots; i++)
                {
                    slots[i] = CrearSlot(canvas.transform, i);
                }
            }
        }

        private void Update()
        {
            if (rectBarra != null)
            {
                Vector2 tamanoDeseado = tamanoBase * escalaBarra;
                Vector2 posicionDeseada = new Vector2(-margen.x, margen.y);

                if (rectBarra.sizeDelta != tamanoDeseado)
                {
                    rectBarra.sizeDelta = tamanoDeseado;
                }

                if (rectBarra.anchoredPosition != posicionDeseada)
                {
                    rectBarra.anchoredPosition = posicionDeseada;
                }
            }
        }

        private void GenerarInterfazConSprite(Transform padre)
        {
            Vector2 tamanoBarra = tamanoBase * escalaBarra;

            GameObject barra = new GameObject("BarraInventario");
            barra.transform.SetParent(padre, false);

            rectBarra = barra.AddComponent<RectTransform>();
            rectBarra.anchorMin = new Vector2(1f, 0f);
            rectBarra.anchorMax = new Vector2(1f, 0f);
            rectBarra.pivot = new Vector2(1f, 0f);
            rectBarra.anchoredPosition = new Vector2(-margen.x, margen.y);
            rectBarra.sizeDelta = tamanoBarra;

            Image imgFondo = barra.AddComponent<Image>();
            imgFondo.sprite = spriteFondoInventario;
            imgFondo.preserveAspect = true;

            for (int i = 0; i < InventarioJugador.CantidadSlots; i++)
            {
                GameObject slotObj = new GameObject($"Slot{i + 1}");
                slotObj.transform.SetParent(barra.transform, false);

                Rect recuadro = recuadrosSlotEnSprite[i];
                RectTransform rectSlot = slotObj.AddComponent<RectTransform>();
                rectSlot.anchorMin = new Vector2(recuadro.xMin / tamanoBase.x, recuadro.yMin / tamanoBase.y);
                rectSlot.anchorMax = new Vector2(recuadro.xMax / tamanoBase.x, recuadro.yMax / tamanoBase.y);
                rectSlot.offsetMin = Vector2.zero;
                rectSlot.offsetMax = Vector2.zero;

                GameObject marco = CrearImagen("Marco", slotObj.transform, new Color(1f, 1f, 1f, 0.45f));
                EstirarSobrePadre(marco.GetComponent<RectTransform>(), 0f);
                marco.transform.SetAsFirstSibling();
                marco.SetActive(false);

                GameObject icono = CrearImagen("Icono", slotObj.transform, Color.white);
                EstirarSobrePadre(icono.GetComponent<RectTransform>(), 8f);

                Image imagenIcono = icono.GetComponent<Image>();
                imagenIcono.preserveAspect = true;
                imagenIcono.enabled = false;

                slots[i] = new SlotInterfaz { icono = imagenIcono, marcoSeleccion = marco };
            }
        }

        private Canvas CrearCanvas()
        {
            GameObject objetoCanvas = new GameObject("CanvasInventario");
            objetoCanvas.transform.SetParent(transform, false);

            Canvas canvas = objetoCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler escalador = objetoCanvas.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private SlotInterfaz CrearSlot(Transform padre, int indice)
        {
            int posicionDesdeDerecha = InventarioJugador.CantidadSlots - 1 - indice;
            float x = -margen.x - posicionDesdeDerecha * (tamanoSlot + separacion) - tamanoSlot * 0.5f;
            float y = margen.y + tamanoSlot * 0.5f;

            GameObject fondo = CrearImagen($"Slot{indice + 1}", padre, colorFondo);
            AnclarAbajoDerecha(fondo.GetComponent<RectTransform>(), new Vector2(x, y), tamanoSlot);

            GameObject marco = CrearImagen("Marco", fondo.transform, colorSeleccion);
            EstirarSobrePadre(marco.GetComponent<RectTransform>(), -4f);
            marco.transform.SetAsFirstSibling();
            marco.SetActive(false);

            GameObject icono = CrearImagen("Icono", fondo.transform, Color.white);
            EstirarSobrePadre(icono.GetComponent<RectTransform>(), 10f);

            Image imagenIcono = icono.GetComponent<Image>();
            imagenIcono.preserveAspect = true;
            imagenIcono.enabled = false;

            return new SlotInterfaz { icono = imagenIcono, marcoSeleccion = marco };
        }

        private static GameObject CrearImagen(string nombre, Transform padre, Color color)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.AddComponent<Image>().color = color;
            return objeto;
        }

        private static void AnclarAbajoDerecha(RectTransform rect, Vector2 posicion, float lado)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = new Vector2(lado, lado);
        }

        private static void EstirarSobrePadre(RectTransform rect, float margenInterior)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margenInterior, margenInterior);
            rect.offsetMax = new Vector2(-margenInterior, -margenInterior);
        }
    }

    [System.Serializable]
    public class SlotInterfaz
    {
        public Image icono;
        public GameObject marcoSeleccion;
    }
}
