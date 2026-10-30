using System;
using System.IO;
using UnityEngine;

namespace SoltaLaPala.Guardado
{
    public static class GuardadoPartida
    {
        private const string NombreArchivo = "partida.json";

        private static string Ruta => Path.Combine(Application.persistentDataPath, NombreArchivo);

        private static bool? existeCache;

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

                existeCache = null;
                return false;
            }
        }

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

                Borrar();
                return null;
            }
        }

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
                existeCache = null;
            }
        }
    }
}
