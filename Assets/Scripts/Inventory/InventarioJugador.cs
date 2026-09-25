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
        [Tooltip("Punto delante del jugador donde flota el item sujeto (luego sera el hueso de la mano).")]
        public Transform anclaMano;

        private readonly DatosItem[] slots = new DatosItem[CantidadSlots];
        private int slotSeleccionado = 0;
        private GameObject instanciaEnMano;

        // Se dispara cuando cambia el contenido de un slot. Parametro: indice del slot.
        public event Action<int> AlCambiarSlot;

        // Se dispara cuando cambia el slot seleccionado. Parametro: nuevo indice.
        public event Action<int> AlCambiarSeleccion;

        public int SlotSeleccionado => slotSeleccionado;

        // Item del slot seleccionado, o null si ese slot esta vacio (manos vacias).
        public DatosItem ItemSeleccionado => slots[slotSeleccionado];

        // Lee las teclas de slot y cambia el slot seleccionado.
        private void Update()
        {
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
