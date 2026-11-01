using System.Collections.Generic;
using SoltaLaPala.Inventory;
using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
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

        [Tooltip("Lo que dice al recibir un sustituto.")]
        [TextArea] public string respuestaSaboteada = "Esto no es lo que te pedi...";

        [Header("Entrega")]
        [Tooltip("Si esta marcado, el objeto tiene que estar en la mano. Si no, basta con llevarlo encima.")]
        public bool debeEstarEnLaMano = false;

        public bool Completada { get; private set; }

        public bool SeSaboteo { get; private set; }

        public struct Opcion
        {
            public DatosItem item;
            public bool esSabotaje;
            public bool disponible;
            public int slot;
        }

        public bool HayEntregaPosible()
        {
            return !Completada && BuscarEntrega(out _, out _);
        }

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

        public bool EntregarOpcion(Opcion opcion)
        {
            if (Completada || opcion.item == null)
            {
                return false;
            }

            InventarioJugador inventario = InventarioJugador.DelJugador();

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

            DialogoNPC dialogo = GetComponent<DialogoNPC>();
            GetComponent<TareaNivel>()?.Completar();
            dialogo?.AlCompletarEncargo();

            MostrarRespuesta(dialogo, opcion.esSabotaje);

            AplicarRecompensa(opcion.esSabotaje, dialogo != null ? dialogo.nombre : name);

            return true;
        }

        public void MarcarComoCompletada()
        {
            Completada = true;
        }

        public void ReiniciarEncargo()
        {
            Completada = false;
            SeSaboteo = false;
        }

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

        private void AplicarRecompensa(bool esSabotaje, string nombreNpc)
        {
            EstadoPartida estado = EstadoPartida.Instancia;

            if (estado == null)
            {
                return;
            }

            if (esSabotaje)
            {
                estado.RegistrarSabotaje(
                    impactoPorSabotaje,
                    sospechaPorSabotaje,
                    $"Encargo de {nombreNpc}");

                Debug.Log($"[MisionEntrega] {name}: cambiazo colado. " +
                          $"Impacto +{impactoPorSabotaje}, sospecha +{sospechaPorSabotaje}.");
                return;
            }

            estado.ReducirSospecha(sospechaQueQuitaCumplir);

            Debug.Log($"[MisionEntrega] {name}: encargo cumplido. " +
                      $"Sospecha -{sospechaQueQuitaCumplir}.");
        }

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
