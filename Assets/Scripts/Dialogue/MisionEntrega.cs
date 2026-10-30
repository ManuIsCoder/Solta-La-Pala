using System.Collections.Generic;
using SoltaLaPala.Inventory;
using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    // Encargo de un NPC: te pide un objeto y le llevas uno.
    //
    // Se puede cumplir de dos formas, y ahi esta la gracia:
    //   - el item correcto  -> baja la sospecha (quedas bien, sirve para disimular)
    //   - un item saboteado -> da impacto y sube la sospecha (le colaste el cambiazo)
    //
    // Va en el mismo objeto que el DialogoNPC. Mientras no lleves ninguno de los
    // dos, el NPC repite su dialogo de Mision en vez de pasar a Charla.
    [RequireComponent(typeof(DialogoNPC))]
    public class MisionEntrega : MonoBehaviour
    {
        [Header("Que pide")]
        [Tooltip("El objeto que el NPC te encarga de verdad.")]
        public DatosItem itemCorrecto;

        [Tooltip("Objetos que cuelan como sustituto: se los das, se los queda, y " +
                 "no son lo que pidio. Ej. una palita de juguete en vez de una pala.")]
        public DatosItem[] itemsSaboteados;

        [Header("Recompensa por cumplir")]
        [Tooltip("Cuanta sospecha te quita entregar lo correcto.")]
        public int sospechaQueQuitaCumplir = 10;

        [Header("Recompensa por sabotear")]
        [Tooltip("Impacto que da colarle el objeto equivocado.")]
        public int impactoPorSabotaje = 3;
        [Tooltip("Sospecha que levanta el cambiazo.")]
        public int sospechaPorSabotaje = 8;

        [Header("Respuesta del NPC")]
        [Tooltip("Lo que dice al recibir lo que pidio.")]
        [TextArea] public string respuestaCorrecta = "Gracias, justo lo que necesitaba.";

        [Tooltip("Lo que dice al recibir un sustituto. No sospecha todavia: solo " +
                 "se da cuenta de que no es lo que pidio.")]
        [TextArea] public string respuestaSaboteada = "Esto no es lo que te pedi...";

        [Header("Entrega")]
        [Tooltip("Si esta marcado, el objeto tiene que estar en la mano (slot " +
                 "seleccionado). Por defecto basta con llevarlo encima: al recoger " +
                 "varias cosas solo la primera cae en el slot activo, y exigir la " +
                 "mano hace que la entrega falle sin que se entienda por que.")]
        public bool debeEstarEnLaMano = false;

        // True cuando ya se entrego algo, para no poder cobrarla dos veces.
        public bool Completada { get; private set; }

        // Como se resolvio, para que el NPC sepa que decir despues.
        public bool SeSaboteo { get; private set; }

        // Una opcion del menu de entrega: un item que el NPC aceptaria, y si lo
        // llevas encima ahora mismo.
        public struct Opcion
        {
            public DatosItem item;
            public bool esSabotaje;
            // False cuando no lo llevas: el menu la pinta en gris y no deja pulsarla.
            public bool disponible;
            // Slot donde esta, o -1 si no lo llevas.
            public int slot;
        }

        // True si ahora mismo se puede entregar algo.
        public bool HayEntregaPosible()
        {
            return !Completada && BuscarEntrega(out _, out _);
        }

        // Todo lo que el NPC aceptaria, lleves o no cada cosa. El menu las muestra
        // todas y deshabilita las que faltan, para que se vea que hay que buscar.
        public List<Opcion> ObtenerOpciones()
        {
            List<Opcion> opciones = new List<Opcion>();
            InventarioJugador inventario = InventarioJugador.DelJugador();

            if (itemCorrecto != null)
            {
                opciones.Add(ConstruirOpcion(inventario, itemCorrecto, false));
            }

            if (itemsSaboteados != null)
            {
                foreach (DatosItem sustituto in itemsSaboteados)
                {
                    if (sustituto != null)
                    {
                        opciones.Add(ConstruirOpcion(inventario, sustituto, true));
                    }
                }
            }

            return opciones;
        }

        private Opcion ConstruirOpcion(InventarioJugador inventario, DatosItem item, bool esSabotaje)
        {
            int slot = inventario != null
                ? inventario.BuscarSlotCon(item, debeEstarEnLaMano)
                : -1;

            return new Opcion
            {
                item = item,
                esSabotaje = esSabotaje,
                disponible = slot != -1,
                slot = slot
            };
        }

        // Entrega el item elegido en el menu. Devuelve false si ya no lo llevas
        // (por ejemplo si lo soltaste con el menu abierto).
        public bool EntregarOpcion(Opcion opcion)
        {
            if (Completada || opcion.item == null)
            {
                return false;
            }

            InventarioJugador inventario = InventarioJugador.DelJugador();

            // Se vuelve a buscar el slot en vez de fiarse del que trae la opcion:
            // el inventario pudo cambiar desde que se abrio el menu.
            int slot = inventario != null
                ? inventario.BuscarSlotCon(opcion.item, debeEstarEnLaMano)
                : -1;

            if (slot == -1)
            {
                return false;
            }

            inventario.QuitarItem(slot);

            Completada = true;
            SeSaboteo = opcion.esSabotaje;

            // El NPC pasa a su siguiente dialogo: el encargo ya esta cerrado.
            DialogoNPC dialogo = GetComponent<DialogoNPC>();
            dialogo?.AlCompletarEncargo();

            // La respuesta va antes de la recompensa: si el cambiazo dispara el
            // tope de sospecha, la pantalla de derrota tiene que quedar encima
            // del cuadro de dialogo, no al reves.
            MostrarRespuesta(dialogo, opcion.esSabotaje);

            AplicarRecompensa(opcion.esSabotaje);

            return true;
        }

        // Marca la mision como ya resuelta sin dar recompensa.
        //
        // Lo usa la carga de partida: el guardado persiste el tipo de dialogo de
        // cada NPC, y uno que ya paso de Mision es uno al que ya le entregaste.
        // Sin esto podrias cobrar la misma mision dos veces recargando.
        public void MarcarComoCompletada()
        {
            Completada = true;
        }

        // Devuelve el encargo a pendiente. Lo usa el reinicio de nivel.
        public void ReiniciarEncargo()
        {
            Completada = false;
            SeSaboteo = false;
        }

        // El NPC comenta lo que acaba de recibir.
        private void MostrarRespuesta(DialogoNPC dialogo, bool esSabotaje)
        {
            string frase = esSabotaje ? respuestaSaboteada : respuestaCorrecta;

            if (dialogo == null || GestorDialogos.Instancia == null
                || string.IsNullOrWhiteSpace(frase))
            {
                return;
            }

            GestorDialogos.Instancia.MostrarFraseSuelta(dialogo, frase);
        }

        private void AplicarRecompensa(bool esSabotaje)
        {
            EstadoPartida estado = EstadoPartida.Instancia;

            if (estado == null)
            {
                return;
            }

            if (esSabotaje)
            {
                // RegistrarSabotaje ya comprueba si la partida termina con esto.
                estado.RegistrarSabotaje(impactoPorSabotaje, sospechaPorSabotaje);

                Debug.Log($"[MisionEntrega] {name}: cambiazo colado. " +
                          $"Impacto +{impactoPorSabotaje}, sospecha +{sospechaPorSabotaje}.");
                return;
            }

            estado.ReducirSospecha(sospechaQueQuitaCumplir);

            Debug.Log($"[MisionEntrega] {name}: encargo cumplido. " +
                      $"Sospecha -{sospechaQueQuitaCumplir}.");
        }

        // Busca en el inventario algo que sirva. El correcto tiene prioridad: si
        // llevas los dos, se entiende que quieres cumplir y no arriesgarte.
        private bool BuscarEntrega(out int slot, out bool esSabotaje)
        {
            slot = -1;
            esSabotaje = false;

            InventarioJugador inventario = InventarioJugador.DelJugador();

            if (inventario == null)
            {
                return false;
            }

            if (itemCorrecto != null)
            {
                slot = inventario.BuscarSlotCon(itemCorrecto, debeEstarEnLaMano);

                if (slot != -1)
                {
                    return true;
                }
            }

            if (itemsSaboteados == null)
            {
                return false;
            }

            foreach (DatosItem sustituto in itemsSaboteados)
            {
                slot = inventario.BuscarSlotCon(sustituto, debeEstarEnLaMano);

                if (slot != -1)
                {
                    esSabotaje = true;
                    return true;
                }
            }

            return false;
        }
    }
}
