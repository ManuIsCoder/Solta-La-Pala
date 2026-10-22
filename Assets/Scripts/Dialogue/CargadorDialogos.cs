using System.Collections.Generic;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    // Una opcion de respuesta del jugador.
    public class OpcionDialogo
    {
        public string texto;
        // Cuanto cambia la sospecha al elegirla: positivo sube, negativo baja.
        public int sospecha;
        // Lo que contesta el NPC a esta opcion. Vacio = sigue con la frase siguiente.
        public string reaccion;
    }

    // Una frase del NPC y, si las tiene, las respuestas que puede elegir el jugador.
    public class LineaDialogo
    {
        public string texto;
        public readonly List<OpcionDialogo> opciones = new List<OpcionDialogo>();

        public bool TieneOpciones => opciones.Count > 0;

        public LineaDialogo(string texto)
        {
            this.texto = texto;
        }
    }

    // Carga los .txt de dialogo desde Resources siguiendo la ruta
    // Nivel/Dialogos/{PJ}/{Tipo}.txt (ej: Nivel/Dialogos/Compañero/Charla.txt).
    //
    // Formato:
    //   - Cada linea no vacia es una frase del NPC.
    //   - Las que empiezan por # son comentarios.
    //   - Las que empiezan por > son respuestas del jugador a la frase de arriba:
    //       > Texto de la respuesta | sospecha | reaccion del NPC
    //     La sospecha es un entero (+10 sube, -5 baja, 0 no cambia) y la reaccion
    //     es opcional. Ejemplo:
    //       ¿Viste la maquina de cafe? Sabe horrible hoy.
    //       > Si, un asco. | 0
    //       > Capaz alguien le echo algo... | +10 | ¿Y vos como sabes eso?
    public static class CargadorDialogos
    {
        // Carpeta raiz dentro de Resources donde viven todos los dialogos.
        public const string CarpetaRaiz = "Nivel/Dialogos";

        private const string PrefijoComentario = "#";
        private const string PrefijoOpcion = ">";
        private const char SeparadorOpcion = '|';

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

        // Lee el .txt del tipo indicado para ese NPC y devuelve sus frases con
        // sus respuestas. Devuelve una lista vacia si el archivo no existe.
        public static List<LineaDialogo> CargarLineas(string idNpc, TipoDialogo tipo)
        {
            string ruta = ConstruirRuta(idNpc, tipo);
            TextAsset archivo = Resources.Load<TextAsset>(ruta);
            if (archivo == null)
            {
                Debug.LogWarning($"[CargadorDialogos] No se encontró el archivo de diálogo: Resources/{ruta}.txt");
                return new List<LineaDialogo>();
            }

            return Interpretar(archivo.text, ruta);
        }

        private static List<LineaDialogo> Interpretar(string textoCrudo, string ruta)
        {
            List<LineaDialogo> lineas = new List<LineaDialogo>();
            if (string.IsNullOrEmpty(textoCrudo)) return lineas;

            string[] renglones = textoCrudo.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string renglon in renglones)
            {
                string limpio = renglon.Trim();
                if (string.IsNullOrEmpty(limpio) || limpio.StartsWith(PrefijoComentario))
                {
                    continue;
                }

                if (!limpio.StartsWith(PrefijoOpcion))
                {
                    lineas.Add(new LineaDialogo(limpio));
                    continue;
                }

                if (lineas.Count == 0)
                {
                    Debug.LogWarning($"[CargadorDialogos] Resources/{ruta}.txt: respuesta '{limpio}' sin una frase arriba, se ignora.");
                    continue;
                }

                OpcionDialogo opcion = InterpretarOpcion(limpio.Substring(PrefijoOpcion.Length), ruta);
                if (opcion != null)
                {
                    lineas[lineas.Count - 1].opciones.Add(opcion);
                }
            }
            return lineas;
        }

        // "Texto | +10 | Reaccion" -> OpcionDialogo. La sospecha y la reaccion
        // son opcionales; una sospecha mal escrita cuenta como 0 y se avisa.
        private static OpcionDialogo InterpretarOpcion(string contenido, string ruta)
        {
            string[] partes = contenido.Split(SeparadorOpcion);

            string texto = partes[0].Trim();
            if (string.IsNullOrEmpty(texto))
            {
                return null;
            }

            int sospecha = 0;
            if (partes.Length > 1)
            {
                string valor = partes[1].Trim();
                if (valor.Length > 0 && !int.TryParse(valor, out sospecha))
                {
                    Debug.LogWarning($"[CargadorDialogos] Resources/{ruta}.txt: sospecha '{valor}' no es un numero en la respuesta '{texto}'. Se usa 0.");
                    sospecha = 0;
                }
            }

            string reaccion = partes.Length > 2 ? partes[2].Trim() : "";

            return new OpcionDialogo { texto = texto, sospecha = sospecha, reaccion = reaccion };
        }
    }
}
