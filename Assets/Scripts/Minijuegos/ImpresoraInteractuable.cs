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
    /// 1. El jugador viene con el documento (Informes/Documento o Foto de Chiqui) en la mano o inventario.
    /// 2. Presiona [E] para "Poner documento en la impresora".
    ///    - El documento se elimina inmediatamente de la mano / inventario.
    ///    - La impresora registra que tiene el documento cargado.
    /// 3. El jugador va a la computadora -> la computadora detecta el documento y deja IMPRIMIR.
    /// 4. Al imprimir, la impresora expulsa físicamente la Carpeta resultante lista para agarrar con [E].
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

        public static ImpresoraInteractuable Instancia { get; private set; }
        public static EstadoDocumentoImpresora DocumentoCargadoGlobal = EstadoDocumentoImpresora.Vacio;

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
        public string idCarpetaSaboteada = "CarpetaSaboteada";

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
            Instancia = this;

            // Eliminar componentes conflictivos que implementen IInteractuable en este mismo objeto (como InteractuableConSonido)
            // para garantizar que InteractorJugador siempre interactúe con ImpresoraInteractuable
            var otrosInteractuables = GetComponents<IInteractuable>();
            foreach (var inter in otrosInteractuables)
            {
                if (inter != (IInteractuable)this && inter is MonoBehaviour mb)
                {
                    Destroy(mb);
                }
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (clipPonerPapel == null)
            {
                clipPonerPapel = Resources.Load<AudioClip>("Audio/Objetos/Impresora2");
            }

            if (clipExpulsarCarpeta == null)
            {
                clipExpulsarCarpeta = Resources.Load<AudioClip>("Audio/Objetos/Impresora2");
            }

            if (objetoVisualDocumentoEnEscaner != null)
            {
                objetoVisualDocumentoEnEscaner.SetActive(documentoCargado != EstadoDocumentoImpresora.Vacio);
            }
        }

        private void OnEnable()
        {
            Instancia = this;
        }

        #region Comprobación de Ítems

        public static bool EsFotoChiqui(DatosItem item)
        {
            if (item == null) return false;
            string id = (item.Id ?? "").ToLower();
            string nom = (item.nombre ?? "").ToLower();
            string nomAsset = (item.name ?? "").ToLower();
            return id.Contains("chiqui") || nom.Contains("chiqui") || nomAsset.Contains("chiqui") 
                || id == "dibujo" || nom.Contains("dibujo") || nomAsset.Contains("dibujo");
        }

        public static bool EsDocumentoOInforme(DatosItem item)
        {
            if (item == null) return false;
            string id = (item.Id ?? "").ToLower();
            string nom = (item.nombre ?? "").ToLower();
            string nomAsset = (item.name ?? "").ToLower();
            return id.Contains("informe") || id.Contains("documento") || id.Contains("papel")
                || nom.Contains("informe") || nom.Contains("documento") || nom.Contains("papel")
                || nomAsset.Contains("informe") || nomAsset.Contains("documento") || nomAsset.Contains("papel");
        }

        public bool JugadorTieneCualquierDocumento(InventarioJugador inv)
        {
            if (inv == null) return false;
            if (EsFotoChiqui(inv.ItemSeleccionado) || EsDocumentoOInforme(inv.ItemSeleccionado)) return true;

            for (int i = 0; i < InventarioJugador.CantidadSlots; i++)
            {
                DatosItem it = inv.ObtenerItem(i);
                if (EsFotoChiqui(it) || EsDocumentoOInforme(it)) return true;
            }

            return false;
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

                // Si tiene el ítem directamente en mano
                if (EsFotoChiqui(inv.ItemSeleccionado))
                    return "[E] Poner Foto de Chiqui en la impresora";

                if (EsDocumentoOInforme(inv.ItemSeleccionado))
                    return "[E] Poner Informes en la impresora";

                // Si lo tiene en algún slot del inventario
                for (int i = 0; i < InventarioJugador.CantidadSlots; i++)
                {
                    DatosItem it = inv.ObtenerItem(i);
                    if (EsFotoChiqui(it)) return "[E] Poner Foto de Chiqui en la impresora";
                    if (EsDocumentoOInforme(it)) return "[E] Poner Informes en la impresora";
                }

                return "[E] Impresora (Requiere Informes o Foto de Chiqui en mano)";
            }
            else
            {
                string nombreDoc = (documentoCargado == EstadoDocumentoImpresora.FotoChiqui)
                    ? "Foto de Chiqui"
                    : "Informes";
                return $"[E] Impresora lista ({nombreDoc}) -> Ve a la Computadora a Imprimir";
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

            if (documentoCargado != EstadoDocumentoImpresora.Vacio)
            {
                Debug.Log("[Impresora] El documento ya está colocado en la impresora. Ve a la computadora para imprimir.");
                return;
            }

            var inv = quienInteractua != null ? quienInteractua.GetComponent<InventarioJugador>() : null;
            if (inv == null)
            {
                var jugador = GameObject.FindGameObjectWithTag("Player");
                if (jugador != null) inv = jugador.GetComponent<InventarioJugador>();
            }
            if (inv == null) return;

            // 1. Revisar primero si tiene el documento en la mano (slot seleccionado)
            DatosItem enMano = inv.ItemSeleccionado;
            if (EsFotoChiqui(enMano))
            {
                ColocarDocumentoEnImpresora(EstadoDocumentoImpresora.FotoChiqui, inv, inv.SlotSeleccionado);
                return;
            }

            if (EsDocumentoOInforme(enMano))
            {
                ColocarDocumentoEnImpresora(EstadoDocumentoImpresora.Informe, inv, inv.SlotSeleccionado);
                return;
            }

            // 2. Si no lo tiene en la mano activa, buscar en los otros slots del inventario
            for (int i = 0; i < InventarioJugador.CantidadSlots; i++)
            {
                DatosItem it = inv.ObtenerItem(i);
                if (EsFotoChiqui(it))
                {
                    ColocarDocumentoEnImpresora(EstadoDocumentoImpresora.FotoChiqui, inv, i);
                    return;
                }
                if (EsDocumentoOInforme(it))
                {
                    ColocarDocumentoEnImpresora(EstadoDocumentoImpresora.Informe, inv, i);
                    return;
                }
            }

            string infoItem = enMano != null ? $"'{enMano.nombre}' (id: {enMano.Id})" : "manos vacías";
            Debug.Log($"[Impresora] No se colocó nada. Tienes en la mano: {infoItem}. Se requiere Informes o Foto de Chiqui.");
        }

        #endregion

        #region Colocación y Expulsión Física

        private void ColocarDocumentoEnImpresora(EstadoDocumentoImpresora tipo, InventarioJugador inv, int slotAEliminar)
        {
            documentoCargado = tipo;
            DocumentoCargadoGlobal = tipo;

            // ELIMINAR EL DOCUMENTO DEL INVENTARIO / MANO
            if (inv != null && slotAEliminar >= 0)
            {
                inv.QuitarItem(slotAEliminar);
            }

            if (objetoVisualDocumentoEnEscaner != null)
            {
                objetoVisualDocumentoEnEscaner.SetActive(true);
            }

            if (audioSource != null && clipPonerPapel != null)
            {
                audioSource.PlayOneShot(clipPonerPapel);
            }

            // Actualizar el prompt de interacción en pantalla de inmediato
            var interactor = FindFirstObjectByType<InteractorJugador>();
            if (interactor != null && interactor.interfazTexto != null)
            {
                interactor.interfazTexto.Mostrar(ObtenerTextoInteraccion());
            }

            string nombreTipo = (tipo == EstadoDocumentoImpresora.FotoChiqui) ? "Foto de Chiqui Tapia" : "Informes del Jefe";
            Debug.Log($"[Impresora] ✔ Se colocó '{nombreTipo}' en la impresora y se eliminó de la mano. ¡Ahora ve a la computadora a imprimir!");

            // Notificar al minijuego si está abierto o en escena
            var mini = FindFirstObjectByType<MinijuegoFotocopiar>(FindObjectsInactive.Include);
            if (mini != null)
            {
                mini.impresora = this;
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
            DocumentoCargadoGlobal = EstadoDocumentoImpresora.Vacio;

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

            // Cargar DatosItem según sea legítimo (Carpeta) o sabotaje (CarpetaSaboteada)
            string idItemExpulsar = fueSabotaje ? idCarpetaSaboteada : idCarpeta;
            DatosItem itemCarpeta = Resources.Load<DatosItem>("Items/" + idItemExpulsar)
                                   ?? Resources.Load<DatosItem>(idItemExpulsar);

            if (itemCarpeta == null && fueSabotaje)
            {
                // Fallback por si la carpeta saboteada no estuviera cargada
                itemCarpeta = Resources.Load<DatosItem>("Items/" + idCarpeta) ?? Resources.Load<DatosItem>(idCarpeta);
            }

            GameObject carpetaInstancia;

            if (prefabCarpeta != null)
            {
                carpetaInstancia = Instantiate(prefabCarpeta, posSalida, rotSalida);
            }
            else if (itemCarpeta != null && itemCarpeta.prefabEnMano != null)
            {
                // Cargar modelo oficial desde el ScriptableObject DatosItem
                carpetaInstancia = Instantiate(itemCarpeta.prefabEnMano, posSalida, rotSalida);
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
            IdentificadorObjeto idComp = carpetaInstancia.GetComponent<IdentificadorObjeto>();
            if (idComp == null)
            {
                idComp = carpetaInstancia.AddComponent<IdentificadorObjeto>();
                idComp.AsignarId((fueSabotaje ? "Carpeta_Saboteada_" : "Carpeta_Informes_") + System.Guid.NewGuid().ToString("N").Substring(0, 8));
            }

            // Asegurar ObjetoRecogible con el DatosItem correspondiente (Carpeta o CarpetaSaboteada)
            ObjetoRecogible recogible = carpetaInstancia.GetComponent<ObjetoRecogible>();
            if (recogible == null)
            {
                recogible = carpetaInstancia.AddComponent<ObjetoRecogible>();
            }

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
