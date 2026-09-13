namespace SoltaLaPala.Dialogue
{
    // Los 4 tipos de archivo de dialogo que puede tener un NPC.
    // El nombre del enum coincide con el nombre del .txt dentro de
    // Resources/Nivel/Dialogos/{PJ}/ (ej: Perpetuo.txt, Mision.txt).
    public enum DialogueType
    {
        // Bucle infinito: lo que dice el NPC cuando ya no le quedan dialogos nuevos.
        Perpetuo,

        // Habla sobre la mision. Al acabar pasa a Charla o a Perpetuo.
        Mision,

        // Charla que no se repite. Al acabar pasa a Perpetuo.
        Charla,

        // Salta cuando el NPC descubre al jugador.
        Detectado
    }
}
