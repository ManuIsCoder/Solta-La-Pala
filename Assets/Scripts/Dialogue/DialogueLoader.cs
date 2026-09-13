using System.Collections.Generic;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    // Carga los .txt de dialogo desde Resources siguiendo la ruta
    // Nivel/Dialogos/{PJ}/{Tipo}.txt (ej: Nivel/Dialogos/Manolo/Charla.txt).
    // Cada linea no vacia del .txt es una frase del NPC.
    public static class DialogueLoader
    {
        // Carpeta raiz dentro de Resources donde viven todos los dialogos.
        public const string DialogueRootPath = "Nivel/Dialogos";

        // Lee el .txt del tipo indicado para ese NPC y devuelve sus lineas.
        // Devuelve una lista vacia si el archivo no existe.
        public static List<string> LoadLines(string npcId, DialogueType type)
        {
            return new List<string>();
        }

        // Construye la ruta de Resources para un NPC y un tipo de dialogo.
        // Ojo: sin extension, porque Resources.Load no la lleva.
        public static string BuildResourcePath(string npcId, DialogueType type)
        {
            return string.Empty;
        }

        // True si ese NPC tiene archivo para ese tipo de dialogo.
        // Sirve para decidir a que tipo se pasa cuando se acaba el actual.
        public static bool HasDialogue(string npcId, DialogueType type)
        {
            return false;
        }

        // Parte el texto crudo del .txt en frases, quitando lineas vacias
        // y lineas de comentario (las que empiezan por #).
        private static List<string> ParseLines(string rawText)
        {
            return new List<string>();
        }
    }
}
