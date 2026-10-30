using SoltaLaPala.Guardado;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Interaction
{
    [RequireComponent(typeof(ResaltadoInteractuable))]
    [RequireComponent(typeof(IdentificadorObjeto))]
    public class ObjetoRecogible : MonoBehaviour, IInteractuable
    {
        public DatosItem item;

        public Transform Transform => transform;

        private void Awake()
        {
            ActualizarVisualSiTieneModelo();
        }

        private void Start()
        {
            IgnorarColisionConJugador();
        }

        private void IgnorarColisionConJugador()
        {
            var jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador == null) return;

            var collidersJugador = jugador.GetComponentsInChildren<Collider>(true);
            foreach (var propio in GetComponentsInChildren<Collider>(true))
            {
                foreach (var delJugador in collidersJugador)
                {
                    Physics.IgnoreCollision(propio, delJugador, true);
                }
            }
        }

        // Si el item tiene un modelo 3D asignado (prefabEnMano) y este objeto en la escena
        // es una primitiva de prototipo (cubo, cápsula, esfera, cilindro), reemplaza la
        // visual por el modelo 3D real.
        private static readonly System.Collections.Generic.HashSet<string> MeshesPrototipo =
            new System.Collections.Generic.HashSet<string>
            {
                "Cube", "Capsule", "Sphere", "Cylinder", "Plane", "Quad"
            };

        private void ActualizarVisualSiTieneModelo()
        {
            if (item == null || item.prefabEnMano == null)
            {
                return;
            }

            // Si es una primitiva de prototipo, reemplazar por el modelo 3D oficial
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null && (mf.sharedMesh == null || MeshesPrototipo.Contains(mf.sharedMesh.name)))
            {
                MeshRenderer mr = GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.enabled = false;
                }

                GameObject visual = Instantiate(item.prefabEnMano, transform);
                visual.name = "ModeloVisual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;

                foreach (var c in visual.GetComponentsInChildren<Collider>(true))
                {
                    Destroy(c);
                }
                foreach (var r in visual.GetComponentsInChildren<Rigidbody>(true))
                {
                    Destroy(r);
                }

                ResaltadoInteractuable resaltado = GetComponent<ResaltadoInteractuable>();
                if (resaltado != null)
                {
                    resaltado.renderers = visual.GetComponentsInChildren<Renderer>(true);
                }
            }
        }

        public string ObtenerTextoInteraccion()
        {
            return item != null ? $"[E] Recoger {item.nombre}" : "[E] Recoger objeto";
        }

        public bool PuedeInteractuar()
        {
            var jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
            {
                var inv = jugador.GetComponent<InventarioJugador>();
                if (inv != null && inv.EstaLleno()) return false;
            }
            return true;
        }

        public void Interactuar(GameObject quienInteractua)
        {
            if (item == null)
            {
                Debug.LogWarning($"[ObjetoRecogible] '{name}' no tiene asignado ningún ScriptableObject en el campo 'Item' del Inspector.");
                return;
            }

            if (quienInteractua != null)
            {
                var inv = quienInteractua.GetComponent<InventarioJugador>();
                if (inv != null)
                {
                    bool agregado = inv.IntentarAgregarItem(item);
                    if (!agregado)
                    {
                        return;
                    }
                }
            }
            gameObject.SetActive(false);

            if (RegistroObjetosConsumidos.Instancia != null)
            {
                RegistroObjetosConsumidos.Instancia.Marcar(gameObject);
            }
        }
    }
}
