using SoltaLaPala.Guardado;
using SoltaLaPala.Inventory;
using UnityEngine;

namespace SoltaLaPala.Interaction
{
    // Objeto del mundo que se puede recoger y guardar en el inventario.
    // Necesita un IdentificadorObjeto para que el guardado recuerde que ya se recogio.
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

        // Si el item tiene un modelo 3D asignado (prefabEnMano) y este objeto en la escena
        // es solo un cubo prototipo primitivo, reemplaza la visual por el modelo 3D real.
        private void ActualizarVisualSiTieneModelo()
        {
            if (item == null || item.prefabEnMano == null)
            {
                return;
            }

            // Si es un cubo primitivo, reemplazar por el modelo 3D oficial
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null && (mf.sharedMesh == null || mf.sharedMesh.name == "Cube"))
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

        // Devuelve "[E] Recoger {nombre del item}".
        public string ObtenerTextoInteraccion()
        {
            return item != null ? $"[E] Recoger {item.nombre}" : "[E] Recoger objeto";
        }

        // Solo se puede recoger si el inventario del jugador tiene algun slot libre.
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

        // Mete el item en el inventario del jugador y destruye este objeto del mundo.
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

            // Se anota antes de que el jugador pueda guardar, para que al cargar
            // el objeto no reaparezca en el suelo.
            if (RegistroObjetosConsumidos.Instancia != null)
            {
                RegistroObjetosConsumidos.Instancia.Marcar(gameObject);
            }
        }
    }
}
