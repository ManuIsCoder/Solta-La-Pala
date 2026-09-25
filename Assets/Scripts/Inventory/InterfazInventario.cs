using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Inventory
{
    // Dibuja los 3 slots del inventario como cuadrados abajo a la derecha de la pantalla
    // y resalta el que esta seleccionado.
    //
    // PROTOTIPO: si no se asignan los slots a mano, el script se construye solo el Canvas
    // y los cuadrados al arrancar. Cuando haya arte de verdad, se arman los slots en la
    // escena, se arrastran al array y la generacion automatica se desactiva sola.
    public class InterfazInventario : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Si se deja vacio se busca el InventarioJugador de la escena.")]
        public InventarioJugador inventario;
        [Tooltip("Los 3 cuadrados del inventario, en orden 1-2-3. Si estan vacios se generan solos.")]
        public SlotInterfaz[] slots = new SlotInterfaz[InventarioJugador.CantidadSlots];

        [Header("Generacion automatica (prototipo)")]
        [Tooltip("Lado del cuadrado de cada slot, en pixeles.")]
        public float tamanoSlot = 80f;
        [Tooltip("Separacion entre cuadrados, en pixeles.")]
        public float separacion = 10f;
        [Tooltip("Margen desde la esquina inferior derecha de la pantalla.")]
        public Vector2 margen = new Vector2(20f, 20f);
        public Color colorFondo = new Color(0f, 0f, 0f, 0.5f);
        public Color colorSeleccion = new Color(1f, 1f, 1f, 0.9f);

        // Construye la UI si hace falta y engancha el inventario.
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

        // Se suscribe a los eventos del inventario para refrescar la UI solo cuando algo cambia.
        private void OnEnable()
        {
            if (inventario == null)
            {
                return;
            }

            inventario.AlCambiarSlot += RefrescarSlot;
            inventario.AlCambiarSeleccion += RefrescarSeleccion;

            // Dibujar el estado inicial: los eventos solo avisan de cambios futuros.
            for (int i = 0; i < InventarioJugador.CantidadSlots; i++)
            {
                RefrescarSlot(i);
            }

            RefrescarSeleccion(inventario.SlotSeleccionado);
        }

        // Se desuscribe de los eventos del inventario.
        private void OnDisable()
        {
            if (inventario == null)
            {
                return;
            }

            inventario.AlCambiarSlot -= RefrescarSlot;
            inventario.AlCambiarSeleccion -= RefrescarSeleccion;
        }

        // Redibuja un slot concreto: pone el icono del item o, si esta vacio, esconde el icono.
        private void RefrescarSlot(int indiceSlot)
        {
            if (!EsSlotUsable(indiceSlot))
            {
                return;
            }

            DatosItem item = inventario.ObtenerItem(indiceSlot);
            Image icono = slots[indiceSlot].icono;

            icono.sprite = item != null ? item.icono : null;

            // Sin sprite el Image pintaria un cuadrado blanco solido, por eso se apaga.
            icono.enabled = item != null && item.icono != null;
        }

        // Marca visualmente que slot esta seleccionado y apaga el resto.
        // El slot seleccionado se resalta aunque este vacio.
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

        // True si el slot existe y tiene el icono enchufado.
        private bool EsSlotUsable(int indiceSlot)
        {
            return inventario != null
                && indiceSlot >= 0
                && indiceSlot < slots.Length
                && slots[indiceSlot] != null
                && slots[indiceSlot].icono != null;
        }

        // True si ya hay slots configurados a mano en el inspector.
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

        // Crea por codigo el Canvas y los 3 cuadrados anclados abajo a la derecha.
        // Solo para prototipar: cuando haya arte se arma a mano en la escena.
        private void GenerarInterfaz()
        {
            Canvas canvas = CrearCanvas();
            slots = new SlotInterfaz[InventarioJugador.CantidadSlots];

            for (int i = 0; i < InventarioJugador.CantidadSlots; i++)
            {
                slots[i] = CrearSlot(canvas.transform, i);
            }
        }

        // Canvas a pantalla completa que escala con la resolucion.
        private Canvas CrearCanvas()
        {
            GameObject objetoCanvas = new GameObject("CanvasInventario");
            objetoCanvas.transform.SetParent(transform, false);

            Canvas canvas = objetoCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler escalador = objetoCanvas.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);

            return canvas;
        }

        // Un cuadrado: fondo + marco de seleccion + icono del item.
        private SlotInterfaz CrearSlot(Transform padre, int indice)
        {
            // Los slots se cuentan desde la derecha, asi el slot 1 queda mas a la izquierda.
            int posicionDesdeDerecha = InventarioJugador.CantidadSlots - 1 - indice;
            float x = -margen.x - posicionDesdeDerecha * (tamanoSlot + separacion) - tamanoSlot * 0.5f;
            float y = margen.y + tamanoSlot * 0.5f;

            GameObject fondo = CrearImagen($"Slot{indice + 1}", padre, colorFondo);
            AnclarAbajoDerecha(fondo.GetComponent<RectTransform>(), new Vector2(x, y), tamanoSlot);

            // El marco vive debajo del icono y se enciende/apaga segun la seleccion.
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

        // GameObject con un Image del color indicado.
        private static GameObject CrearImagen(string nombre, Transform padre, Color color)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.AddComponent<Image>().color = color;
            return objeto;
        }

        // Ancla el rect a la esquina inferior derecha de la pantalla.
        private static void AnclarAbajoDerecha(RectTransform rect, Vector2 posicion, float lado)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = new Vector2(lado, lado);
        }

        // Hace que el rect ocupe todo el padre, con un margen interior.
        // Un margen negativo lo hace mas grande que el padre (para el marco de seleccion).
        private static void EstirarSobrePadre(RectTransform rect, float margenInterior)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margenInterior, margenInterior);
            rect.offsetMax = new Vector2(-margenInterior, -margenInterior);
        }
    }

    // Un cuadrado individual del inventario: el icono del item y el marco de seleccion.
    [System.Serializable]
    public class SlotInterfaz
    {
        public Image icono;
        public GameObject marcoSeleccion;
    }
}
