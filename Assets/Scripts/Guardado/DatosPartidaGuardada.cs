using System;
using UnityEngine;

namespace SoltaLaPala.Guardado
{
    // Foto de una partida en curso, tal como se escribe a disco.
    //
    // Es deliberadamente plano y con campos publicos: JsonUtility no serializa
    // propiedades, diccionarios ni tipos anidados con herencia. Cada sistema
    // vuelca aqui lo suyo desde IGuardable.Capturar y lo lee en Restaurar.
    [Serializable]
    public class DatosPartidaGuardada
    {
        // Formato actual. Si algun dia cambia la estructura, subir este numero
        // hace que GuardadoPartida descarte los guardados viejos en vez de
        // cargar basura a medias.
        public const int VersionActual = 1;

        public int version = VersionActual;

        // --- Progreso ---
        public int nivel = 1;
        public int impacto;
        public int sospecha;

        // Segundos que quedaban de nivel. Se guarda para que recargar la partida
        // no regale tiempo.
        public float tiempoRestante;

        // --- Jugador ---
        public Vector3 posicionJugador;
        public float rotacionJugadorY;

        // --- Camara ---
        public float rotacionCamaraX;
        public float rotacionCamaraY;

        // --- Inventario ---
        // Un id de DatosItem por slot. Cadena vacia = slot vacio.
        public string[] slotsInventario = new string[0];
        public int slotSeleccionado;

        // --- Mundo ---
        // Ids de IdentificadorObjeto ya gastados: recogidos o saboteados.
        public string[] objetosConsumidos = new string[0];

        // Progreso de dialogo de cada NPC, como "idNpc:TipoDialogo".
        //
        // Lo que avanza al hablar es el tipoActual del NPC (Mision -> Charla ->
        // Perpetuo), asi que guardar ese tipo basta: no hace falta la lista de
        // lineas ya leidas. Formato plano porque JsonUtility no serializa
        // diccionarios.
        public string[] progresoDialogos = new string[0];
    }
}
