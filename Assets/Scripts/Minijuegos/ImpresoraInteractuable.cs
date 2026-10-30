using System.Collections;
using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using SoltaLaPala.Guardado;
using UnityEngine;

namespace SoltaLaPala.Minijuegos
{
    /// <summary>
    /// Interactuable para la Impresora física en la Sala de Impresión.
    /// 
    /// Flujo:
    /// 1. El jugador viene con el documento (Informes o Foto de Chiqui).
    /// 2. Presiona [E] para "Poner documento en la impresora". El documento se carga en la máquina y se consume del inventario.
    /// 3. El jugador va a la computadora e imprime.
    /// 4. Al imprimir, la impresora expulsa físicamente la Carpeta resultante con ObjetoRecogible listo para agarrar con [E].
    /// </summary>
    [RequireComponent(typeof(ResaltadoInteractuable))]
    public class ImpresoraInteractuable : MonoBehaviour, IInteractuable
    {
        public enum EstadoSwitch
        {
            Activado,
            Desactivado
        }

        public enum EstadoDocumentoImpresora
        {
            Vacio,
            Informe,
            FotoChiqui
        }

        [Header("Interruptor del interactuable")]
        public EstadoSwitch estado = EstadoSwitch.Activado;

        [Header("Estado del Escáner")]
        [Tooltip("Documento actualmente cargado en la bandeja de escaneo de la impresora")]
        public EstadoDocumentoImpresora documentoCargado = EstadoDocumentoImpresora.Vacio;

        [Header("IDs de Ítems requeridos")]
        public string idInforme = "Informe";
        public string idInformeAlternativo = "Documento";
        public string idFotoChiqui = "FotoChiqui";
        public string idFotoChiquiAlternativo = "fotoChiqui";
        public string idCarpeta = "Carpeta";

        [Header("Referencias de Expulsión")]
        [Tooltip("Punto físico desde donde saldrá la carpeta expulsada. Si se deja vacío, saldrá delante de la impresora.")]
        public Transform puntoSalidaCarpeta;
        [Tooltip("Prefab o modelo de la Carpeta a expulsar. Si se deja vacío, se cargará el modelo oficial de Resources.")]
        public GameObject prefabCarpeta;
        [Tooltip("Hojita o modelo visual en la tapa del escáner (opcional)")]
        public GameObject objetoVisualDocumentoEnEscaner;

        [Header("Sonido")]
        public AudioSource audioSource;
        public AudioClip clipPonerPapel;
        public AudioClip clipExpulsarCarpeta;

        public Transform Transform => transform;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (objetoVisualDocumentoEnEscaner != null)
            {
                objetoVisualDocumentoEnEscaner.SetActive(documentoCargado != EstadoDocumentoImpresora.Vacio);
            }
        }

        #region Comprobación de Inventario del Jugador

        private bool JugadorTieneInforme(InventarioJugador inv)
        {
            if (inv == null) return false;
            return inv.TieneItem(idInforme)
                || (!string.IsNullOrEmpty(idInformeAlternativo) && inv.TieneItem(idInformeAlternativo))
                || inv.TieneItem("Papeles");
        }

        private bool JugadorTieneFotoChiqui(InventarioJugador inv)
        {
            if (inv == null) return false;
            return inv.TieneItem(idFotoChiqui)
                || (!string.IsNullOrEmpty(idFotoChiquiAlternativo) && inv.TieneItem(idFotoChiquiAlternativo))
                || inv.TieneItem("Dibujo");
        }

        private bool EsItemFotoChiqui(DatosItem item)
        {
            if (item == null) return false;
            string id = item.Id;
            return id == idFotoChiqui || id == idFotoChiquiAlternativo || id == "Dibujo";
        }

        private bool EsItemInforme(DatosItem item)
        {
            if (item == null) return false;
            string id = item.Id;
            return id == idInforme || id == idInformeAlternativo || id == "Papeles";
        }

        #endregion

        #region IInteractuable

        public string ObtenerTextoInteraccion()
        {
            if (estado == EstadoSwitch.Desactivado)
                return string.Empty;

            var jugador = GameObject.FindGameObjectWithTag("Player");
            var inv = jugador != null ? jugador.GetComponent<InventarioJugador>() : null;

            if (documentoCargado == EstadoDocumentoImpresora.Vacio)
            {
                if (inv == null)
                    return "[E] Impresora (Requiere Informes o Foto de Chiqui)";

                // Si tiene el ítem seleccionado en mano
                if (EsItemFotoChiqui(inv.ItemSeleccionado))
                    return "[E] Poner Foto de Chiqui en la impresora";

                if (EsItemInforme(inv.ItemSeleccionado))
                    return "[E] Poner Informes en la impresora";

                // Si los tiene en el inventario aunque no estén en la mano activa
                if (JugadorTieneInforme(inv) && !JugadorTieneFotoChiqui(inv))
                    return "[E] Poner Informes en la impresora";

                if (JugadorTieneFotoChiqui(inv) && !JugadorTieneInforme(inv))
                    return "[E] Poner Foto de Chiqui en la impresora";

                if (JugadorTieneInforme(inv) && JugadorTieneFotoChiqui(inv))
                    return "[E] Poner documento en la impresora";

                return "[E] Impresora (Requiere Informes o Foto de Chiqui)";
            }
            else
            {
                string nombreDoc = (documentoCargado == EstadoDocumentoImpresora.FotoChiqui)
                    ? "Foto de Chiqui"
                    : "Informes";
                return $"[E] Impresora lista ({nombreDoc}) -> Usa la Computadora para Imprimir";
            }
        }

        public bool PuedeInteractuar()
        {
            if (estado != EstadoSwitch.Activado) return false;
            if (GestorMinijuego.Instancia != null && GestorMinijuego.Instancia.MinijuegoActivo) return false;

            return true;
        }

        public void Interactuar(GameObject quienInteractua)
        {
            if (estado == EstadoSwitch.Desactivado) return;

            var inv = quienInteractua != null ? quienInteractua.GetComponent<InventarioJugador>() : null;

            if (documentoCargado != EstadoDocumentoImpresora.Vacio)
            {
                Debug.Log("[Impresora] El documento ya está en la bandeja del escáner. Ve a la computadora para imprimir.");
                return;
            }

            if (inv == null) return;

            // Decidir qué documento colocar
            if (EsItemFotoChiqui(inv.ItemSeleccionado) || (JugadorTieneFotoChiqui(inv) && !JugadorTieneInforme(inv)))
            {
                ColocarDocumentoEnImpresora(EstadoDocumentoImpresora.FotoChiqui, inv);
            }
            else if (EsItemInforme(inv.ItemSeleccionado) || JugadorTieneInforme(inv))
            {
                ColocarDocumentoEnImpresora(EstadoDocumentoImpresora.Informe, inv);
            }
            else if (JugadorTieneFotoChiqui(inv))
            {
                ColocarDocumentoEnImpresora(EstadoDocumentoImpresora.FotoChiqui, inv);
            }
            else
            {
                Debug.Log("[Impresora] No tienes Informes ni Foto de Chiqui en tu inventario para poner.");
            }
        }

        #endregion

        #region Colocación y Expulsión Física

        private void ColocarDocumentoEnImpresora(EstadoDocumentoImpresora tipo, InventarioJugador inv)
        {
            documentoCargado = tipo;

            // Consumir del inventario
            if (tipo == EstadoDocumentoImpresora.FotoChiqui)
            {
                if (!inv.QuitarItem(idFotoChiqui))
                {
                    if (!inv.QuitarItem(idFotoChiquiAlternativo))
                        inv.QuitarItem("Dibujo");
                }
                Debug.Log("[Impresora] ✔ Has colocado la Foto de Chiqui Tapia en la impresora. ¡Ahora ve a la computadora a imprimir!");
            }
            else
            {
                if (!inv.QuitarItem(idInforme))
                {
                    if (!inv.QuitarItem(idInformeAlternativo))
                        inv.QuitarItem("Papeles");
                }
                Debug.Log("[Impresora] ✔ Has colocado los Informes del Jefe en la impresora. ¡Ahora ve a la computadora a imprimir!");
            }

            if (objetoVisualDocumentoEnEscaner != null)
            {
                objetoVisualDocumentoEnEscaner.SetActive(true);
            }

            if (audioSource != null && clipPonerPapel != null)
            {
                audioSource.PlayOneShot(clipPonerPapel);
            }

            // Notificar al minijuego si está abierto o en escena
            var mini = FindFirstObjectByType<MinijuegoFotocopiar>(FindObjectsInactive.Include);
            if (mini != null)
            {
                mini.ActualizarPantallaSegunEstadoImpresora();
            }
        }

        /// <summary>
        /// Lo llama MinijuegoFotocopiar al finalizar la impresión desde la computadora.
        /// Expulsa físicamente la Carpeta delante de la impresora.
        /// </summary>
        public void ExpulsarCarpeta(bool fueSabotaje)
        {
            if (objetoVisualDocumentoEnEscaner != null)
            {
                objetoVisualDocumentoEnEscaner.SetActive(false);
            }

            documentoCargado = EstadoDocumentoImpresora.Vacio;

            StartCoroutine(RutinaExpulsarCarpeta(fueSabotaje));
        }

        private IEnumerator RutinaExpulsarCarpeta(bool fueSabotaje)
        {
            yield return new WaitForSeconds(0.4f);

            // Determinar posición de salida (en la bandeja frontal de la impresora)
            Vector3 posSalida;
            Quaternion rotSalida;

            if (puntoSalidaCarpeta != null)
            {
                posSalida = puntoSalidaCarpeta.position;
                rotSalida = puntoSalidaCarpeta.rotation;
            }
            else
            {
                posSalida = transform.position + (transform.forward * 0.45f) + (transform.up * 0.25f);
                rotSalida = transform.rotation;
            }

            GameObject carpetaInstancia;

            if (prefabCarpeta != null)
            {
                carpetaInstancia = Instantiate(prefabCarpeta, posSalida, rotSalida);
            }
            else
            {
                // Fallback: cargar modelo oficial Carpeta.fbx
                GameObject modeloCarpeta = Resources.Load<GameObject>("Modelos/Carpeta")
                                          ?? Resources.Load<GameObject>("Carpeta");

                if (modeloCarpeta != null)
                {
                    carpetaInstancia = Instantiate(modeloCarpeta, posSalida, rotSalida);
                }
                else
                {
                    // Si no, crear objeto con BoxCollider y escala adecuada
                    carpetaInstancia = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    carpetaInstancia.transform.position = posSalida;
                    carpetaInstancia.transform.rotation = rotSalida;
                    carpetaInstancia.transform.localScale = new Vector3(0.35f, 0.05f, 0.45f);
                }
            }

            carpetaInstancia.name = fueSabotaje ? "Carpeta_Saboteada" : "Carpeta_Informes";

            // Asegurar Collider
            Collider col = carpetaInstancia.GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider box = carpetaInstancia.AddComponent<BoxCollider>();
                box.size = new Vector3(0.4f, 0.08f, 0.5f);
            }

            // Asegurar Rigidbody para pequeña caída física suave
            Rigidbody rb = carpetaInstancia.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = carpetaInstancia.AddComponent<Rigidbody>();
            }
            rb.mass = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            // Impulso suave hacia afuera de la impresora
            rb.AddForce(transform.forward * 1.2f + Vector3.up * 0.4f, ForceMode.Impulse);

            // Asegurar ResaltadoInteractuable
            if (carpetaInstancia.GetComponent<ResaltadoInteractuable>() == null)
            {
                carpetaInstancia.AddComponent<ResaltadoInteractuable>();
            }

            // Asegurar IdentificadorObjeto
            if (carpetaInstancia.GetComponent<IdentificadorObjeto>() == null)
            {
                var idComp = carpetaInstancia.AddComponent<IdentificadorObjeto>();
                idComp.id = "Carpeta_Impresa_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            }

            // Asegurar ObjetoRecogible con DatosItem "Carpeta"
            ObjetoRecogible recogible = carpetaInstancia.GetComponent<ObjetoRecogible>();
            if (recogible == null)
            {
                recogible = carpetaInstancia.AddComponent<ObjetoRecogible>();
            }

            DatosItem itemCarpeta = Resources.Load<DatosItem>("Items/" + idCarpeta)
                                   ?? Resources.Load<DatosItem>(idCarpeta);

            recogible.item = itemCarpeta;

            if (audioSource != null && clipExpulsarCarpeta != null)
            {
                audioSource.PlayOneShot(clipExpulsarCarpeta);
            }

            Debug.Log($"[Impresora] ➔ ¡Ha salido la {carpetaInstancia.name} de la impresora! Puedes recogerla con [E].");
        }

        #endregion
    }
}
