using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Pinta una silueta blanca cuando el jugador esta apuntando a este objeto.
    //
    // Solo se enciende mientras el InteractorJugador lo tiene como objetivo, y ese
    // solo elige objetos con los que se puede interactuar de verdad: si algo brilla,
    // es que sirve.
    public class ResaltadoInteractuable : MonoBehaviour
    {
        [Header("Borde")]
        public Color colorBorde = Color.white;
        public float intensidad = 1.2f;
        [Tooltip("Si se deja vacio se cogen todos los Renderer hijos al arrancar.")]
        public Renderer[] renderers;

        private MaterialPropertyBlock propBlock;
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

        // Emision que tenia cada renderer antes de resaltarlo, para devolversela
        // al apagar en vez de dejarlo todo en negro: un objeto que ya brillaba
        // por su material se quedaria apagado para siempre.
        private Color[] emisionOriginal;

        private bool resaltado;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            propBlock = new MaterialPropertyBlock();

            GuardarEmisionOriginal();
        }

        // Lee la emision de partida de cada material. Se hace una sola vez, antes
        // de que nadie la haya tocado.
        private void GuardarEmisionOriginal()
        {
            emisionOriginal = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];

                emisionOriginal[i] = rend != null && rend.sharedMaterial != null
                    && rend.sharedMaterial.HasProperty(EmissionColorProp)
                        ? rend.sharedMaterial.GetColor(EmissionColorProp)
                        : Color.black;
            }
        }

        // Enciende o apaga la silueta blanca.
        public void Resaltar(bool activo)
        {
            if (renderers == null || resaltado == activo)
            {
                return;
            }

            resaltado = activo;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];

                if (rend == null)
                {
                    continue;
                }

                rend.GetPropertyBlock(propBlock);
                propBlock.SetColor(EmissionColorProp, activo
                    ? colorBorde * intensidad
                    : emisionOriginal[i]);
                rend.SetPropertyBlock(propBlock);

                // La keyword se toca sobre el material de instancia y no se puede
                // meter en el PropertyBlock. Se apaga al desresaltar salvo que el
                // material ya emitiera de por si.
                AjustarKeywordEmision(rend, activo || emisionOriginal[i] != Color.black);
            }
        }

        private static void AjustarKeywordEmision(Renderer rend, bool encendida)
        {
            foreach (Material mat in rend.materials)
            {
                if (encendida)
                {
                    mat.EnableKeyword("_EMISSION");
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }

        // Si el objeto se desactiva estando resaltado (al recogerlo, por ejemplo)
        // hay que dejarlo limpio: al reaparecer seguiria brillando.
        private void OnDisable()
        {
            if (resaltado)
            {
                Resaltar(false);
            }
        }
    }
}
