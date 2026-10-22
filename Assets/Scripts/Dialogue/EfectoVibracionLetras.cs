using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Dialogue
{
    // Hace que cada letra del dialogo "vibre" de arriba para abajo cuando aparece.
    //
    // Es un efecto de malla sobre el Text: no cambia el texto, solo desplaza en
    // vertical los vertices de las letras recien aparecidas. Lo anade solo
    // InterfazDialogo y recibe sus valores de ahi, asi se ajusta desde el
    // Inspector de InterfazDialogo.
    //
    // Como las letras salen a un ritmo constante, la edad de cada una se deduce de
    // su posicion: la ultima acaba de aparecer, la anterior hace un turno, etc.
    // Asi no hace falta saber a que caracter corresponde cada cuadrado de la malla.
    public class EfectoVibracionLetras : BaseMeshEffect
    {
        private float amplitud = 6f;
        private float duracion = 0.3f;
        private float ciclos = 2f;
        private float segundosPorLetra = 0.03f;

        // Momento (en tiempo real) en que salio la ultima letra.
        private float instanteUltimaLetra = float.NegativeInfinity;

        public void Configurar(float nuevaAmplitud, float nuevaDuracion, float nuevosCiclos, float nuevosSegundosPorLetra)
        {
            amplitud = nuevaAmplitud;
            duracion = Mathf.Max(0.01f, nuevaDuracion);
            ciclos = nuevosCiclos;
            segundosPorLetra = nuevosSegundosPorLetra;
        }

        // Avisa de que acaba de salir una letra: la malla hay que recalcularla.
        public void LetraNueva()
        {
            instanteUltimaLetra = Time.unscaledTime;
            Refrescar();
        }

        // Corta la vibracion (la linea aparecio entera de golpe).
        public void Detener()
        {
            instanteUltimaLetra = float.NegativeInfinity;
            Refrescar();
        }

        // Mientras queden letras vibrando hay que recalcular la malla cada frame.
        private void Update()
        {
            if (Vibrando())
            {
                Refrescar();
            }
        }

        // True si alguna letra sigue dentro de su vibracion.
        private bool Vibrando()
        {
            return amplitud > 0f && Time.unscaledTime - instanteUltimaLetra < duracion;
        }

        private void Refrescar()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || amplitud <= 0f || vh.currentVertCount == 0)
            {
                return;
            }

            // Cada letra es un cuadrado de 4 vertices.
            int letras = vh.currentVertCount / 4;
            float desdeUltima = Time.unscaledTime - instanteUltimaLetra;
            UIVertex vertice = new UIVertex();

            for (int letra = 0; letra < letras; letra++)
            {
                // La ultima letra tiene la edad de desdeUltima; cada una anterior es un turno mas vieja.
                float edad = desdeUltima + (letras - 1 - letra) * segundosPorLetra;

                if (edad >= duracion)
                {
                    continue;
                }

                // Sube y baja, y se va calmando hasta quedarse quieta en su sitio.
                float progreso = edad / duracion;
                float desplazamiento = Mathf.Sin(progreso * ciclos * 2f * Mathf.PI) * amplitud * (1f - progreso);

                for (int k = 0; k < 4; k++)
                {
                    int indice = letra * 4 + k;
                    vh.PopulateUIVertex(ref vertice, indice);
                    vertice.position += new Vector3(0f, desplazamiento, 0f);
                    vh.SetUIVertex(vertice, indice);
                }
            }
        }
    }
}
