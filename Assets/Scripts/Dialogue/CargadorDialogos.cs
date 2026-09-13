using System.Collections.Generic;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    // Carga los .txt de dialogo desde Resources siguiendo la ruta
    // Nivel/Dialogos/{PJ}/{Tipo}.txt (ej: Nivel/Dialogos/Compañero/Charla.txt).
    // Cada linea no vacia del .txt es una frase del NPC.
    public static class CargadorDialogos
    {
        // Carpeta raiz dentro de Resources donde viven todos los dialogos.
        public const string CarpetaRaiz = "Nivel/Dialogos";

        // Construye la ruta de Resources para un NPC y un tipo de dialogo.
        // Ojo: sin extension, porque Resources.Load no la lleva
        public static string ConstruirRuta(string idNpc, TipoDialogo tipo)
        {
            return $"{CarpetaRaiz}/{idNpc}/{tipo}";
        }

        // True si ese NPC tiene archivo para ese tipo de dialogo.
        // Sirve para decidir a que tipo se pasa cuando se acaba el actual.
        public static bool TieneDialogo(string idNpc, TipoDialogo tipo)
        {
            string ruta = ConstruirRuta(idNpc, tipo);
            return Resources.Load<TextAsset>(ruta) != null;
        }

        // Lee el .txt del tipo indicado para ese NPC y devuelve sus lineas.
        // Devuelve una lista vacia si el archivo no existe.
        public static List<string> CargarLineas(string idNpc, TipoDialogo tipo)
        {
            string ruta = ConstruirRuta(idNpc, tipo);
            TextAsset archivo = Resources.Load<TextAsset>(ruta);
            if (archivo == null)
            {
                Debug.LogWarning($"[CargadorDialogos] No se encontró el archivo de diálogo: Resources/{ruta}.txt");
                return new List<string>();
            }

            return SepararLineas(archivo.text);
        }

        // Parte el texto crudo del .txt en frases, quitando lineas vacias
        // y lineas de comentario (las que empiezan por #).
        private static List<string> SepararLineas(string textoCrudo)
        {
            List<string> lineas = new List<string>();
            if (string.IsNullOrEmpty(textoCrudo)) return lineas;

            string[] renglones = textoCrudo.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string renglon in renglones)
            {
                string limpio = renglon.Trim();
                // Ignorar líneas vacías o comentarios (#)
                if (!string.IsNullOrEmpty(limpio) && !limpio.StartsWith("#"))
                {
                    lineas.Add(limpio);
                }
            }
            return lineas;
        }
    }
}
