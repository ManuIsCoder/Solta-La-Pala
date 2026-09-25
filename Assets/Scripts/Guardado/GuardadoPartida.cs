using System;
using System.IO;
using UnityEngine;

namespace SoltaLaPala.Guardado
{
    // Acceso al archivo de la partida guardada. Una sola ranura: un archivo.
    //
    // Nadie mas toca el disco. Toda operacion esta envuelta en try/catch porque
    // un guardado corrupto, un disco lleno o un permiso denegado no pueden tirar
    // el juego: en el peor caso se comporta como si no hubiera partida guardada.
    public static class GuardadoPartida
    {
        private const string NombreArchivo = "partida.json";

        // persistentDataPath es la carpeta que Unity garantiza escribible en
        // todas las plataformas, y que no se borra al actualizar el juego.
        private static string Ruta => Path.Combine(Application.persistentDataPath, NombreArchivo);

        // Cache del resultado de Existe(), para que el menu principal no consulte
        // el disco en cada frame al refrescar su boton.
        private static bool? existeCache;

        // True si hay una partida guardada que se pueda cargar.
        public static bool Existe()
        {
            if (existeCache.HasValue)
            {
                return existeCache.Value;
            }

            try
            {
                existeCache = File.Exists(Ruta);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GuardadoPartida] No se pudo comprobar el guardado: {e.Message}");
                existeCache = false;
            }

            return existeCache.Value;
        }

        // Escribe la partida a disco. Devuelve true si se pudo guardar.
        public static bool Guardar(DatosPartidaGuardada datos)
        {
            if (datos == null)
            {
                return false;
            }

            datos.version = DatosPartidaGuardada.VersionActual;

            try
            {
                string json = JsonUtility.ToJson(datos, true);
                File.WriteAllText(Ruta, json);
                existeCache = true;

                Debug.Log($"[GuardadoPartida] Partida guardada en {Ruta}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GuardadoPartida] No se pudo guardar: {e.Message}");

                // El archivo puede haber quedado a medias: se invalida la cache
                // para que la proxima consulta vuelva a mirar el disco.
                existeCache = null;
                return false;
            }
        }

        // Lee la partida guardada, o null si no hay, esta corrupta o es de un
        // formato viejo. Quien llama solo tiene que comprobar null.
        public static DatosPartidaGuardada Cargar()
        {
            if (!Existe())
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(Ruta);
                DatosPartidaGuardada datos = JsonUtility.FromJson<DatosPartidaGuardada>(json);

                if (datos == null)
                {
                    Debug.LogWarning("[GuardadoPartida] El archivo de guardado esta vacio o corrupto.");
                    Borrar();
                    return null;
                }

                // Un guardado de otra version tiene campos que ya no significan lo
                // mismo. Mejor empezar de cero que restaurar un estado incoherente.
                if (datos.version != DatosPartidaGuardada.VersionActual)
                {
                    Debug.LogWarning($"[GuardadoPartida] Guardado de version {datos.version}, " +
                                     $"se esperaba {DatosPartidaGuardada.VersionActual}. Se descarta.");
                    Borrar();
                    return null;
                }

                return datos;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GuardadoPartida] No se pudo leer el guardado: {e.Message}");

                // Un archivo que no se puede parsear no va a arreglarse solo: se
                // borra para que el menu deje de ofrecer "Continuar" en falso.
                Borrar();
                return null;
            }
        }

        // Borra la partida guardada. Se llama al empezar un nivel a mano y al
        // terminar uno: en los dos casos la partida vieja ya no tiene sentido.
        public static void Borrar()
        {
            try
            {
                if (File.Exists(Ruta))
                {
                    File.Delete(Ruta);
                    Debug.Log("[GuardadoPartida] Partida guardada borrada.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GuardadoPartida] No se pudo borrar el guardado: {e.Message}");
            }
            finally
            {
                // Pase lo que pase con el archivo, la cache no debe quedar
                // afirmando que existe algo que quizas ya no esta.
                existeCache = null;
            }
        }
    }
}
