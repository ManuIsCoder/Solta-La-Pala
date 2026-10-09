using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoltaLaPala.Interaction
{
    // Pinta una silueta blanca (outline) cuando el jugador esta apuntando a este objeto.
    //
    // Usa un shader Inverted Hull sin alterar los materiales ni colores originales
    // del objeto, evitando que se vuelva completamente blanco solido.
    public class ResaltadoInteractuable : MonoBehaviour
    {
        [Header("Silueta")]
        [Tooltip("Color del contorno.")]
        public Color colorSilueta = Color.white;

        [Tooltip("Grosor del contorno en metros.")]
        [Range(0.005f, 0.08f)]
        public float grosor = 0.025f;

        [Tooltip("Si se deja vacio se toman todos los MeshRenderer hijos al arrancar.")]
        public Renderer[] renderers;

        private static Material materialSiluetaCompartido;
        private readonly List<GameObject> siluetasCreadas = new List<GameObject>();
        private bool resaltado;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }

            ConstruirSiluetas();
        }

        private static Material ObtenerMaterialSilueta()
        {
            if (materialSiluetaCompartido == null)
            {
                Shader shader = Shader.Find("Custom/OutlineSilueta");
                if (shader == null)
                {
                    Debug.LogWarning("[ResaltadoInteractuable] No se encontro el shader 'Custom/OutlineSilueta'.");
                    return null;
                }
                materialSiluetaCompartido = new Material(shader)
                {
                    name = "Material_Silueta_Compartido"
                };
            }
            return materialSiluetaCompartido;
        }

        // Crea un objeto hijo 'Silueta' por cada MeshRenderer existente
        private void ConstruirSiluetas()
        {
            Material mat = ObtenerMaterialSilueta();
            if (mat == null || renderers == null) return;

            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            propBlock.SetColor("_Color", colorSilueta);
            propBlock.SetFloat("_Grosor", grosor);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (rend == null || rend.name == "Silueta_Outline") continue;

                MeshFilter mfOriginal = rend.GetComponent<MeshFilter>();
                if (mfOriginal == null || mfOriginal.sharedMesh == null) continue;

                GameObject objSilueta = new GameObject("Silueta_Outline");
                objSilueta.transform.SetParent(rend.transform, false);
                objSilueta.transform.localPosition = Vector3.zero;
                objSilueta.transform.localRotation = Quaternion.identity;
                objSilueta.transform.localScale = Vector3.one;

                MeshFilter mf = objSilueta.AddComponent<MeshFilter>();
                mf.sharedMesh = mfOriginal.sharedMesh;

                MeshRenderer mr = objSilueta.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.SetPropertyBlock(propBlock);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;

                objSilueta.SetActive(false);
                siluetasCreadas.Add(objSilueta);
            }
        }

        // Enciende o apaga la silueta blanca
        public void Resaltar(bool activo)
        {
            if (resaltado == activo) return;
            resaltado = activo;

            for (int i = 0; i < siluetasCreadas.Count; i++)
            {
                GameObject silueta = siluetasCreadas[i];
                if (silueta != null)
                {
                    silueta.SetActive(activo);
                }
            }
        }

        // Si el objeto se desactiva (ej: al recogerlo), apaga el resaltado
        private void OnDisable()
        {
            if (resaltado)
            {
                Resaltar(false);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < siluetasCreadas.Count; i++)
            {
                if (siluetasCreadas[i] != null)
                {
                    Destroy(siluetasCreadas[i]);
                }
            }
            siluetasCreadas.Clear();
        }
    }
}
