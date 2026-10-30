using System;
using SoltaLaPala.Dialogue;
using SoltaLaPala.Guardado;
using SoltaLaPala.Interaction;
using SoltaLaPala.Menus;
using SoltaLaPala.Personajes;
using UnityEngine;

namespace SoltaLaPala.Inventory
{
    public class InventarioJugador : MonoBehaviour, IGuardable
    {
        public const int CantidadSlots = 3;

        [Header("Mano")]
        [Tooltip("Punto donde flota el item sujeto. Si se deja vacio se crea con el offset de abajo.")]
        public Transform anclaMano;

        [Tooltip("Donde flota el item respecto al jugador, si no hay ancla asignada. " +
                 "X = derecha, Y = altura, Z = hacia adelante.")]
        public Vector3 offsetMano = new Vector3(0.35f, 1.2f, 0.6f);

        [Tooltip("Giro del item en la mano, en grados.")]
        public Vector3 rotacionMano = new Vector3(0f, 0f, 0f);

        [Tooltip("Escala del item mientras esta sujeto.")]
        public float escalaEnMano = 1f;

        [Tooltip("Cuanto flota el item arriba y abajo. 0 lo deja quieto.")]
        public float amplitudFlotado = 0.04f;

        [Tooltip("Velocidad del flotado, en ciclos por segundo.")]
        public float velocidadFlotado = 1.5f;

        [Header("Mano del modelo")]
        [Tooltip("Posicion del item respecto al hueso de la mano derecha, en metros.")]
        public Vector3 offsetEnHueso = new Vector3(0.08f, 0.02f, 0f);

        [Tooltip("Giro del item respecto al hueso de la mano derecha, en grados.")]
        public Vector3 rotacionEnHueso = Vector3.zero;

        private readonly DatosItem[] slots = new DatosItem[CantidadSlots];
        private int slotSeleccionado = 0;
        private GameObject instanciaEnMano;

        private bool anclaFlotante;

        public event Action<int> AlCambiarSlot;

        public event Action<int> AlCambiarSeleccion;

        public int SlotSeleccionado => slotSeleccionado;

        public DatosItem ItemSeleccionado => slots[slotSeleccionado];

        private void Awake()
        {
            if (anclaMano == null)
            {
                GameObject objeto = new GameObject("AnclaMano");
                objeto.transform.SetParent(transform, false);
                objeto.transform.localPosition = offsetMano;
                objeto.transform.localRotation = Quaternion.Euler(rotacionMano);
                anclaMano = objeto.transform;

                anclaFlotante = true;
            }
        }

        private void Start()
        {
            ColgarDeLaMano();
        }

        public void ColgarDeLaMano()
        {
            if (!anclaFlotante)
            {
                return;
            }

            ModeloPersonaje modelo = GetComponent<ModeloPersonaje>();
            Transform mano = modelo != null ? modelo.BuscarHueso(ModeloPersonaje.HuesoManoDerecha) : null;
            if (mano == null)
            {
                return;
            }

            anclaFlotante = false;
            anclaMano.SetParent(mano, false);
            anclaMano.localPosition = offsetEnHueso;
            anclaMano.localRotation = Quaternion.Euler(rotacionEnHueso);

            Vector3 escalaMano = mano.lossyScale;
            anclaMano.localScale = new Vector3(1f / escalaMano.x, 1f / escalaMano.y, 1f / escalaMano.z);

            ActualizarItemEnMano();
        }

        private void Update()
        {
            AnimarItemEnMano();

            if (GestorMenus.Instancia != null && GestorMenus.Instancia.HayMenuAbierto)
            {
                return;
            }

            if (GestorDialogos.Instancia != null && GestorDialogos.Instancia.DialogoActivo)
            {
                return;
            }

            for (int i = 0; i < CantidadSlots; i++)
            {
                if (ControlesJuego.Pulsada(AccionJuego.Slot1 + i))
                {
                    SeleccionarSlot(i);
                    Debug.Log($"Slot seleccionado: {i}");
                    break;
                }
            }

            if (ControlesJuego.Pulsada(AccionJuego.SoltarObjeto))
            {
                SoltarItemSeleccionado();
            }
        }

        private void AnimarItemEnMano()
        {
            if (!anclaFlotante || anclaMano == null)
            {
                return;
            }

            float desplazamiento = amplitudFlotado > 0f
                ? Mathf.Sin(Time.time * velocidadFlotado * Mathf.PI * 2f) * amplitudFlotado
                : 0f;

            anclaMano.localPosition = offsetMano + Vector3.up * desplazamiento;
            anclaMano.localRotation = Quaternion.Euler(rotacionMano);
        }

        public bool IntentarAgregarItem(DatosItem item)
        {
            if (item == null)
            {
                return false;
            }

            int destino = slots[slotSeleccionado] == null
                ? slotSeleccionado
                : ObtenerPrimerSlotVacio();

            if (destino == -1)
            {
                return false;
            }

            slots[destino] = item;
            AlCambiarSlot?.Invoke(destino);

            ModeloPersonaje modelo = GetComponent<ModeloPersonaje>();
            if (modelo != null && modelo.Animador != null)
            {
                modelo.Animador.Agarrar();
            }

            if (destino == slotSeleccionado)
            {
                ActualizarItemEnMano();
            }

            return true;
        }

        public DatosItem QuitarItem(int indiceSlot)
        {
            if (!EsIndiceValido(indiceSlot) || slots[indiceSlot] == null)
            {
                return null;
            }

            DatosItem item = slots[indiceSlot];
            slots[indiceSlot] = null;
            AlCambiarSlot?.Invoke(indiceSlot);

            if (indiceSlot == slotSeleccionado)
            {
                ActualizarItemEnMano();
            }

            return item;
        }

        public void SoltarItemSeleccionado()
        {
            if (ItemSeleccionado == null)
            {
                return;
            }

            DatosItem itemASoltar = QuitarItem(slotSeleccionado);
            if (itemASoltar == null)
            {
                return;
            }

            Transform camara = Camera.main != null ? Camera.main.transform : transform;
            Vector3 direccionLanzamiento = camara.forward;
            Vector3 posicionAparicion = transform.position + Vector3.up * 1.0f + direccionLanzamiento * 0.8f;

            GameObject objetoSuelo;
            if (itemASoltar.prefabEnMano != null)
            {
                objetoSuelo = Instantiate(itemASoltar.prefabEnMano, posicionAparicion, Quaternion.identity);
            }
            else
            {
                objetoSuelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                objetoSuelo.name = itemASoltar.nombre;
                objetoSuelo.transform.position = posicionAparicion;
                objetoSuelo.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    objetoSuelo.GetComponent<Renderer>().material = new Material(shader) { color = Color.white };
                }
            }

            objetoSuelo.name = itemASoltar.nombre;

            Collider col = objetoSuelo.GetComponent<Collider>();
            if (col == null)
            {
                col = objetoSuelo.AddComponent<BoxCollider>();
            }
            else if (col is MeshCollider mc)
            {
                mc.convex = true;
            }
            col.enabled = true;

            Rigidbody rb = objetoSuelo.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = objetoSuelo.AddComponent<Rigidbody>();
            }
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.AddForce(direccionLanzamiento * 2.5f + Vector3.up * 1.2f, ForceMode.VelocityChange);

            if (objetoSuelo.GetComponent<ObjetoSoltado>() == null)
            {
                objetoSuelo.AddComponent<ObjetoSoltado>();
            }

            ObjetoRecogible recogible = objetoSuelo.GetComponent<ObjetoRecogible>();
            if (recogible == null)
            {
                recogible = objetoSuelo.AddComponent<ObjetoRecogible>();
            }
            recogible.item = itemASoltar;

            IdentificadorObjeto ident = objetoSuelo.GetComponent<IdentificadorObjeto>();
            if (ident == null)
            {
                ident = objetoSuelo.AddComponent<IdentificadorObjeto>();
            }
            ident.AsignarIdNuevo();

            ResaltadoInteractuable resaltado = objetoSuelo.GetComponent<ResaltadoInteractuable>();
            if (resaltado == null)
            {
                resaltado = objetoSuelo.AddComponent<ResaltadoInteractuable>();
            }
            resaltado.renderers = objetoSuelo.GetComponentsInChildren<Renderer>(true);
        }

        public void SeleccionarSlot(int indiceSlot)
        {
            if (!EsIndiceValido(indiceSlot) || indiceSlot == slotSeleccionado)
            {
                return;
            }

            slotSeleccionado = indiceSlot;
            ActualizarItemEnMano();
            AlCambiarSeleccion?.Invoke(slotSeleccionado);
        }

        public void Vaciar()
        {
            for (int i = 0; i < CantidadSlots; i++)
            {
                slots[i] = null;
                AlCambiarSlot?.Invoke(i);
            }

            slotSeleccionado = 0;
            ActualizarItemEnMano();
            AlCambiarSeleccion?.Invoke(slotSeleccionado);
        }

        public static InventarioJugador DelJugador()
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");

            return jugador != null ? jugador.GetComponent<InventarioJugador>() : null;
        }

        public int BuscarSlotCon(DatosItem item, bool soloEnLaMano)
        {
            if (item == null)
            {
                return -1;
            }

            if (soloEnLaMano)
            {
                return ItemSeleccionado == item ? slotSeleccionado : -1;
            }

            for (int i = 0; i < CantidadSlots; i++)
            {
                if (slots[i] == item)
                {
                    return i;
                }
            }

            return -1;
        }

        public DatosItem ObtenerItem(int indiceSlot)
        {
            return EsIndiceValido(indiceSlot) ? slots[indiceSlot] : null;
        }

        public bool EstaLleno()
        {
            return ObtenerPrimerSlotVacio() == -1;
        }

        private int ObtenerPrimerSlotVacio()
        {
            for (int i = 0; i < CantidadSlots; i++)
            {
                if (slots[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool EsIndiceValido(int indiceSlot)
        {
            return indiceSlot >= 0 && indiceSlot < CantidadSlots;
        }

        private void ActualizarItemEnMano()
        {
            if (instanciaEnMano != null)
            {
                Destroy(instanciaEnMano);
                instanciaEnMano = null;
            }

            DatosItem item = ItemSeleccionado;

            if (item == null || item.prefabEnMano == null || anclaMano == null)
            {
                return;
            }

            instanciaEnMano = Instantiate(item.prefabEnMano, anclaMano);
            instanciaEnMano.transform.localPosition = Vector3.zero;
            instanciaEnMano.transform.localRotation = item.prefabEnMano.transform.localRotation;
            instanciaEnMano.transform.localScale = instanciaEnMano.transform.localScale * escalaEnMano;

            DesactivarFisica(instanciaEnMano);
        }

        private static void DesactivarFisica(GameObject objeto)
        {
            foreach (Collider colisionador in objeto.GetComponentsInChildren<Collider>(true))
            {
                colisionador.enabled = false;
            }

            foreach (Rigidbody cuerpo in objeto.GetComponentsInChildren<Rigidbody>(true))
            {
                cuerpo.isKinematic = true;
                cuerpo.detectCollisions = false;
            }
        }

        public void Capturar(DatosPartidaGuardada datos)
        {
            string[] ids = new string[CantidadSlots];

            for (int i = 0; i < CantidadSlots; i++)
            {
                ids[i] = slots[i] != null ? slots[i].Id : string.Empty;
            }

            datos.slotsInventario = ids;
            datos.slotSeleccionado = slotSeleccionado;
        }

        public void Restaurar(DatosPartidaGuardada datos)
        {
            ItemsRegistrados registro = ItemsRegistrados.Instancia;
            string[] ids = datos.slotsInventario;

            for (int i = 0; i < CantidadSlots; i++)
            {
                string id = (ids != null && i < ids.Length) ? ids[i] : string.Empty;
                slots[i] = registro != null ? registro.Resolver(id) : null;

                AlCambiarSlot?.Invoke(i);
            }

            slotSeleccionado = EsIndiceValido(datos.slotSeleccionado) ? datos.slotSeleccionado : 0;

            ActualizarItemEnMano();
            AlCambiarSeleccion?.Invoke(slotSeleccionado);
        }

        public bool TieneItem(string id)
        {
            for (int i = 0; i < CantidadSlots; i++)
                if (slots[i] != null && slots[i].Id == id) return true;
            return false;
        }
        public bool QuitarItem(string id)
        {
            for (int i = 0; i < CantidadSlots; i++)
            {
                if (slots[i] != null && slots[i].Id == id)
                {
                    QuitarItem(i);
                    return true;
                }
            }
            return false;
        }
    }
}
