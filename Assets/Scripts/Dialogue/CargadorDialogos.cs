using System.Collections.Generic;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    public class OpcionDialogo
    {
        public string texto;
        public int sospecha;
        public string reaccion;
    }

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

    public static class CargadorDialogos
    {
        public const string CarpetaRaiz = "Nivel/Dialogos";

        private const string PrefijoComentario = "#";
        private const string PrefijoOpcion = ">";
        private const char SeparadorOpcion = '|';

        public static string ConstruirRuta(string idNpc, TipoDialogo tipo)
        {
            return $"{CarpetaRaiz}/{idNpc}/{tipo}";
        }

        public static bool TieneDialogo(string idNpc, TipoDialogo tipo)
        {
            string ruta = ConstruirRuta(idNpc, tipo);
            return Resources.Load<TextAsset>(ruta) != null;
        }

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
