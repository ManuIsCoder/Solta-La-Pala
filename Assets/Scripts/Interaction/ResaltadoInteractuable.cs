using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Pinta un contorno o resaltado blanco cuando el jugador lo esta mirando.
    public class ResaltadoInteractuable : MonoBehaviour
    {
        [Header("Borde")]
        public Color colorBorde = Color.white;
        public float intensidad = 1.2f;
        [Tooltip("Si se deja vacio se cogen todos los Renderer hijos al arrancar.")]
        public Renderer[] renderers;

        private MaterialPropertyBlock propBlock;
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }
            propBlock = new MaterialPropertyBlock();
        }

        // Enciende o apaga el borde blanco / resaltado.
        public void Resaltar(bool activo)
        {
            if (renderers == null) return;

            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propBlock);
                if (activo)
                {
                    propBlock.SetColor(EmissionColorProp, colorBorde * intensidad);
                    // Habilitar keyword de emisión en URP para que brille
                    foreach (var mat in rend.materials)
                    {
                        mat.EnableKeyword("_EMISSION");
                    }
                }
                else
                {
                    propBlock.SetColor(EmissionColorProp, Color.black);
                }
                rend.SetPropertyBlock(propBlock);
            }
        }
    }
}
