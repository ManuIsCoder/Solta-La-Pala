using System;
using SoltaLaPala.Guardado;
using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.Inventory
{
    // Inventario del jugador estilo PEAK / hotbar de Minecraft: 3 slots fijos,
    // se cambia de slot con las teclas 1, 2 y 3.
    // Siempre hay un slot seleccionado, pero ese slot puede estar vacio:
    // al usar o soltar un item el slot se queda vacio y el PJ queda con las manos vacias.
    // Solo cuando los 3 slots estan llenos es imposible tener las manos vacias.
    public class InventarioJugador : MonoBehaviour, IGuardable
    {
        public const int CantidadSlots = 3;

        [Header("Mano")]
        [Tooltip("Punto delante del jugador donde flota el item sujeto (luego sera el hueso de la mano). " +
                 "Si se deja vacio se crea solo con el offset de abajo.")]
        public Transform anclaMano;

        [Tooltip("Donde flota el item respecto al jugador, si no hay ancla asignada. " +
                 "X = derecha, Y = altura, Z = hacia adelante.")]
        public Vector3 offsetMano = new Vector3(0.35f, 1.2f, 0.6f);

        [Tooltip("Giro del item en la mano, en grados.")]
        public Vector3 rotacionMano = new Vector3(0f, 0f, 0f);

        [Tooltip("Escala del item mientras esta sujeto. Los objetos del mundo " +
                 "suelen ser demasiado grandes vistos de cerca.")]
        public float escalaEnMano = 1f;

        [Tooltip("Cuanto flota el item arriba y abajo. 0 lo deja quieto.")]
        public float amplitudFlotado = 0.04f;

        [Tooltip("Velocidad del flotado, en ciclos por segundo.")]
        public float velocidadFlotado = 1.5f;

        private readonly DatosItem[] slots = new DatosItem[CantidadSlots];
        private int slotSeleccionado = 0;
        private GameObject instanciaEnMano;

        // True si el ancla la creo este script. Solo entonces se le aplican
        // offsetMano y el flotado: un ancla puesta a mano (o un hueso del
        // esqueleto) manda ella, y moverla seria pelearse con la animacion.
        private bool anclaCreada;

        // Se dispara cuando cambia el contenido de un slot. Parametro: indice del slot.
        public event Action<int> AlCambiarSlot;

        // Se dispara cuando cambia el slot seleccionado. Parametro: nuevo indice.
        public event Action<int> AlCambiarSeleccion;

        public int SlotSeleccionado => slotSeleccionado;

        // Item del slot seleccionado, o null si ese slot esta vacio (manos vacias).
        public DatosItem ItemSeleccionado => slots[slotSeleccionado];

        // Si no hay ancla puesta en el inspector, se crea una delante del jugador
        // para que el item sujeto se vea flotando como si lo llevara en la mano.
        private void Awake()
        {
            if (anclaMano == null)
            {
                GameObject objeto = new GameObject("AnclaMano");
                objeto.transform.SetParent(transform, false);
                objeto.transform.localPosition = offsetMano;
                objeto.transform.localRotation = Quaternion.Euler(rotacionMano);
                anclaMano = objeto.transform;

                anclaCreada = true;
            }
        }

        // Lee las teclas de slot y cambia el slot seleccionado.
        private void Update()
        {
            AnimarItemEnMano();

            // Con un menu abierto el juego esta pausado y el jugador esta navegando
            // la UI: no debe cambiar de item sin querer.
            if (GestorMenus.Instancia != null && GestorMenus.Instancia.HayMenuAbierto)
            {
                return;
            }

            // Slot1, Slot2 y Slot3 son valores consecutivos del enum, asi que
            // se generan sumando el indice en vez de escribir un if por tecla.
            for (int i = 0; i < CantidadSlots; i++)
            {
                if (ControlesJuego.Pulsada(AccionJuego.Slot1 + i))
                {
                    SeleccionarSlot(i);
                    Debug.Log($"Slot seleccionado: {i}");
                    break;
                }
            }
        }

        // Mueve el ancla arriba y abajo para que el item se vea flotando en vez de
        // clavado en el aire. Solo si el ancla la creamos nosotros.
        private void AnimarItemEnMano()
        {
            if (!anclaCreada || anclaMano == null)
            {
                return;
            }

            // Se reposiciona desde offsetMano en vez de acumular sobre la posicion
            // actual, asi tocar el offset en el inspector durante Play se ve al momento
            // y el flotado no deriva con el tiempo.
            float desplazamiento = amplitudFlotado > 0f
                ? Mathf.Sin(Time.time * velocidadFlotado * Mathf.PI * 2f) * amplitudFlotado
                : 0f;

            anclaMano.localPosition = offsetMano + Vector3.up * desplazamiento;
            anclaMano.localRotation = Quaternion.Euler(rotacionMano);
        }

        // Mete el item en el slot seleccionado si esta libre; si no, en el primer slot vacio.
        // Devuelve false si los 3 slots estan ocupados.
        // Si acaba en el slot seleccionado, el item pasa directamente a la mano.
        public bool IntentarAgregarItem(DatosItem item)
        {
            if (item == null)
            {
                return false;
            }

            // Prioridad al slot que tenes activo: recoges algo y te queda en la mano,
            // que es lo que espera el jugador.
            int destino = slots[slotSeleccionado] == null
                ? slotSeleccionado
                : ObtenerPrimerSlotVacio();

            if (destino == -1)
            {
                return false;
            }

            slots[destino] = item;
            AlCambiarSlot?.Invoke(destino);

            if (destino == slotSeleccionado)
            {
                ActualizarItemEnMano();
            }

            return true;
        }

        // Vacia el slot y devuelve el item que habia (para soltarlo, usarlo o darselo a un NPC).
        // La seleccion no se mueve: si era el slot seleccionado, el PJ queda con las manos vacias.
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

        // Selecciona el slot indicado y actualiza lo que hay en la mano.
        // Ignora indices fuera de rango y reselecciones del slot que ya estaba activo.
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

        // Devuelve el item de un slot, o null si esta vacio o el indice no es valido.
        public DatosItem ObtenerItem(int indiceSlot)
        {
            return EsIndiceValido(indiceSlot) ? slots[indiceSlot] : null;
        }

        // True si los 3 slots estan ocupados (no se puede recoger nada mas).
        public bool EstaLleno()
        {
            return ObtenerPrimerSlotVacio() == -1;
        }

        // Devuelve el primer indice de slot vacio, o -1 si no hay ninguno.
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

        // True si el indice cae dentro de los slots existentes.
        private static bool EsIndiceValido(int indiceSlot)
        {
            return indiceSlot >= 0 && indiceSlot < CantidadSlots;
        }

        // Sincroniza la mano con el slot seleccionado: destruye la instancia anterior y,
        // si el slot tiene item, instancia su prefab en la anclaMano
        // (de momento flotando delante del PJ; mas adelante disparara la animacion de sujetar).
        // Si el slot esta vacio simplemente deja las manos vacias.
        private void ActualizarItemEnMano()
        {
            if (instanciaEnMano != null)
            {
                Destroy(instanciaEnMano);
                instanciaEnMano = null;
            }

            DatosItem item = ItemSeleccionado;

            // Slot vacio, item sin prefab o sin ancla configurada: manos vacias y ya esta.
            if (item == null || item.prefabEnMano == null || anclaMano == null)
            {
                return;
            }

            // Se instancia como hijo del ancla para que siga al PJ sin tener que
            // reposicionarlo cada frame.
            instanciaEnMano = Instantiate(item.prefabEnMano, anclaMano);
            instanciaEnMano.transform.localPosition = Vector3.zero;
            instanciaEnMano.transform.localRotation = Quaternion.identity;
            instanciaEnMano.transform.localScale = Vector3.one * escalaEnMano;

            DesactivarFisica(instanciaEnMano);
        }

        // Apaga colliders y rigidbodies del item sujeto.
        //
        // El prefab suele ser el mismo objeto que estaba en el suelo, con su fisica
        // puesta: sujetarlo tal cual haria que empuje al jugador, dispare
        // interacciones o se caiga al vacio.
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

        // Guarda un id por slot. Los slots vacios van como cadena vacia, para que
        // el array conserve las posiciones: importa en que slot estaba cada item.
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
                // Un guardado de una version con mas o menos slots no debe salirse
                // del array, y sin registro no hay forma de resolver ningun id.
                string id = (ids != null && i < ids.Length) ? ids[i] : string.Empty;
                slots[i] = registro != null ? registro.Resolver(id) : null;

                // La UI de slots se pinta desde este evento, asi que hay que
                // avisar de todos, tambien de los que quedaron vacios.
                AlCambiarSlot?.Invoke(i);
            }

            // Se asigna directo en vez de por SeleccionarSlot: ese metodo ignora
            // el indice si coincide con el actual, y entonces la mano se quedaria
            // mostrando el item de antes de cargar.
            slotSeleccionado = EsIndiceValido(datos.slotSeleccionado) ? datos.slotSeleccionado : 0;

            ActualizarItemEnMano();
            AlCambiarSeleccion?.Invoke(slotSeleccionado);
        }
    }
}
