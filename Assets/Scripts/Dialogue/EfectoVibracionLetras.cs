using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Dialogue
{
    public class EfectoVibracionLetras : BaseMeshEffect
    {
        private float amplitud = 6f;
        private float duracion = 0.3f;
        private float ciclos = 2f;
        private float segundosPorLetra = 0.03f;

        private float instanteUltimaLetra = float.NegativeInfinity;

        public void Configurar(float nuevaAmplitud, float nuevaDuracion, float nuevosCiclos, float nuevosSegundosPorLetra)
        {
            amplitud = nuevaAmplitud;
            duracion = Mathf.Max(0.01f, nuevaDuracion);
            ciclos = nuevosCiclos;
            segundosPorLetra = nuevosSegundosPorLetra;
        }

        public void LetraNueva()
        {
            instanteUltimaLetra = Time.unscaledTime;
            Refrescar();
        }

        public void Detener()
        {
            instanteUltimaLetra = float.NegativeInfinity;
            Refrescar();
        }

        private void Update()
        {
            if (Vibrando())
            {
                Refrescar();
            }
        }

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

            int letras = vh.currentVertCount / 4;
            float desdeUltima = Time.unscaledTime - instanteUltimaLetra;
            UIVertex vertice = new UIVertex();

            for (int letra = 0; letra < letras; letra++)
            {
                float edad = desdeUltima + (letras - 1 - letra) * segundosPorLetra;

                if (edad >= duracion)
                {
                    continue;
                }

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
