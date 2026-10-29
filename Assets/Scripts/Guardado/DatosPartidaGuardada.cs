using System;
using UnityEngine;

namespace SoltaLaPala.Guardado
{
    [Serializable]
    public class DatosPartidaGuardada
    {
        public const int VersionActual = 1;

        public int version = VersionActual;

        public int nivel = 1;
        public int impacto;
        public int sospecha;

        public float tiempoRestante;

        public Vector3 posicionJugador;
        public float rotacionJugadorY;

        public float rotacionCamaraX;
        public float rotacionCamaraY;

        public string[] slotsInventario = new string[0];
        public int slotSeleccionado;

        public string[] objetosConsumidos = new string[0];

        public string[] progresoDialogos = new string[0];
    }
}
