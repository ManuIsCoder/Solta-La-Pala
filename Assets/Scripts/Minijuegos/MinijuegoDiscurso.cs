using System.Collections;
using SoltaLaPala.Dialogue;
using UnityEngine;
using UnityEngine.UI;
using SoltaLaPala.Menus;
using SoltaLaPala.Inventory;

namespace SoltaLaPala.Minijuegos
{
    public class MinijuegoDiscurso : MinijuegoBase
    {
        [Header("IDs de Ítems")]
        public string idTarjetas = "TarjetasDiscurso";
        public string idTarjetaSabotaje = "DiscursoFalso";

        [Header("Fondo UI")]
        public Image imagenFondo;

        [Header("Tarjetas de Discurso (Botones / Arrastrables)")]
        public Button tarjetaTrabajo1;
        public Button tarjetaTrabajo2;
        public Button tarjetaDisparate; // Tarjeta mal escrita (sabotaje)

        [Header("UI General")]
        public Text textoTemaSeleccionado;
        public Button botonDarDiscurso;
        public Button botonSalir;

        [Header("Ajustes")]
        public bool modoPrueba = false;
        public string nombreProtagonista = "Protagonista";
        public VozNPC vozProtagonista = VozNPC.Agudo;

        private int tarjetaElegida = -1; // 0 o 1 = Trabajo, 2 = Sabotaje
        private bool discursoEnCurso;
        private Coroutine rutinaDiscurso;
        private InterfazDialogo interfazDiscurso;
        private AudioSource fuenteVoz;
        private AudioClip clipVoz;

        protected override void AlIniciar()
        {
            bool tieneTarjetas = modoPrueba || (Inventario != null && Inventario.TieneItem(idTarjetas));
            bool tieneSabotaje = modoPrueba || (Inventario != null && Inventario.TieneItem(idTarjetaSabotaje));
            tarjetaElegida = -1;
            discursoEnCurso = false;

            if (textoTemaSeleccionado != null)
                textoTemaSeleccionado.text = "Selecciona una tarjeta para el discurso...";

            if (botonDarDiscurso != null)
            {
                botonDarDiscurso.interactable = false;
                botonDarDiscurso.onClick.RemoveAllListeners();
                botonDarDiscurso.onClick.AddListener(DarDiscurso);
            }

            if (botonSalir != null)
            {
                botonSalir.onClick.RemoveAllListeners();
                botonSalir.onClick.AddListener(Salir);
            }

            ConfigurarBotonTarjeta(tarjetaTrabajo1, 0, tieneTarjetas, "Estrategia Financiera Q4");
            ConfigurarBotonTarjeta(tarjetaTrabajo2, 1, tieneTarjetas, "Optimización de Recursos");
            ConfigurarBotonTarjeta(tarjetaDisparate, 2, tieneSabotaje, "Poema a la Milanesa (Disparate)");
        }

        private void ConfigurarBotonTarjeta(Button boton, int indice, bool interactuable, string titulo)
        {
            if (boton == null) return;
            boton.interactable = interactuable;
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(() => SeleccionarTarjeta(indice, titulo));
        }

        public void SeleccionarTarjeta(int indice, string titulo)
        {
            tarjetaElegida = indice;
            if (textoTemaSeleccionado != null)
                textoTemaSeleccionado.text = $"Discurso seleccionado: {titulo}";

            if (botonDarDiscurso != null)
                botonDarDiscurso.interactable = true;
        }

        public void DarDiscurso()
        {
            if (tarjetaElegida == -1 || discursoEnCurso) return;

            discursoEnCurso = true;
            if (botonDarDiscurso != null) botonDarDiscurso.interactable = false;
            if (tarjetaTrabajo1 != null) tarjetaTrabajo1.interactable = false;
            if (tarjetaTrabajo2 != null) tarjetaTrabajo2.interactable = false;
            if (tarjetaDisparate != null) tarjetaDisparate.interactable = false;
            rutinaDiscurso = StartCoroutine(RepresentarDiscurso(tarjetaElegida));
        }

        private IEnumerator RepresentarDiscurso(int indiceTarjeta)
        {
            string[] lineas = ObtenerLineasDiscurso(indiceTarjeta);
            interfazDiscurso = FindFirstObjectByType<InterfazDialogo>(FindObjectsInactive.Include);

            if (interfazDiscurso != null)
            {
                interfazDiscurso.AlEmpezarLinea += AlEmpezarLinea;
                interfazDiscurso.AlTerminarLinea += AlTerminarLinea;
                PrepararVoz();
                interfazDiscurso.Abrir(nombreProtagonista);
                foreach (string linea in lineas)
                {
                    interfazDiscurso.MostrarLinea(linea);
                    yield return null;

                    while (true)
                    {
                        if (Input.GetMouseButtonDown(0)
                            || Input.GetKeyDown(KeyCode.Return)
                            || Input.GetKeyDown(KeyCode.KeypadEnter)
                            || Input.GetKeyDown(KeyCode.Space))
                        {
                            if (interfazDiscurso.Escribiendo)
                            {
                                interfazDiscurso.CompletarLinea();
                            }
                            else
                            {
                                break;
                            }
                        }

                        yield return null;
                    }
                }

                interfazDiscurso.Cerrar();
                interfazDiscurso.AlEmpezarLinea -= AlEmpezarLinea;
                interfazDiscurso.AlTerminarLinea -= AlTerminarLinea;
                interfazDiscurso = null;
            }
            else
            {
                Debug.LogWarning("[MinijuegoDiscurso] No se encontró una InterfazDialogo en la escena.", this);
            }

            bool esSabotaje = indiceTarjeta == 2;
            int impacto = esSabotaje ? 3 : 1;

            if (EstadoPartida.Instancia != null)
            {
                if (esSabotaje)
                {
                    int mitadTramoAmarillo = Mathf.RoundToInt(EstadoPartida.Instancia.SospechaMaxima * 0.65f);
                    EstadoPartida.Instancia.EstablecerSospecha(mitadTramoAmarillo);
                }
                EstadoPartida.Instancia.RegistrarSabotaje(impacto, 0);
            }

            if (esSabotaje)
                Inventario?.QuitarItem(idTarjetaSabotaje);
            else
                Inventario?.QuitarItem(idTarjetas);

            EstadoPartida.Instancia?.TerminarPartida(
                esSabotaje,
                esSabotaje
                    ? "El poema a la milanesa conquistó al público. ¡Ganaste el nivel!"
                    : "El discurso no convenció al público. Perdiste el nivel.");
            Salir();
        }

        private void PrepararVoz()
        {
            if (vozProtagonista == VozNPC.Ninguna) return;

            clipVoz = Resources.Load<AudioClip>($"Audio/Voces/Balbuceo{vozProtagonista}");
            if (fuenteVoz == null)
            {
                GameObject objetoVoz = new GameObject("VozProtagonista");
                objetoVoz.transform.SetParent(transform, false);
                fuenteVoz = objetoVoz.AddComponent<AudioSource>();
                fuenteVoz.playOnAwake = false;
                fuenteVoz.loop = true;
                fuenteVoz.spatialBlend = 0f;
            }
        }

        private void AlEmpezarLinea(float duracion)
        {
            if (fuenteVoz == null || clipVoz == null) return;
            fuenteVoz.clip = clipVoz;
            fuenteVoz.Play();
        }

        private void AlTerminarLinea()
        {
            if (fuenteVoz != null && fuenteVoz.isPlaying)
                fuenteVoz.Stop();
        }

        private static string[] ObtenerLineasDiscurso(int indiceTarjeta)
        {
            return indiceTarjeta switch
            {
                0 => new[]
                {
                    "Buenos días. Para el próximo trimestre propongo enfocar la inversión en los proyectos con mejores resultados.",
                    "Vamos a revisar los gastos, medir cada objetivo y corregir el rumbo con datos concretos.",
                    "Con un plan claro y el trabajo de todos, podemos crecer sin perder de vista nuestras prioridades. Gracias."
                },
                1 => new[]
                {
                    "Optimizar recursos no significa hacer menos, sino aprovechar mejor lo que ya tenemos.",
                    "Coordinemos los equipos, simplifiquemos las tareas repetidas y compartamos la información a tiempo.",
                    "Así podremos trabajar mejor, cuidar el presupuesto y alcanzar los objetivos en conjunto. Muchas gracias."
                },
                _ => new[]
                {
                    "Oh, noble milanesa, dorada estrella de mi plato, crujiente sol que alegra la mesa.",
                    "Entre pan rallado y limón, tu aroma convoca hasta al más serio de los oficinistas.",
                    "Que nunca falte tu presencia junto a las papas. ¡Viva la milanesa!"
                }
            };
        }

        private void Update()
        {
            if (!discursoEnCurso && Input.GetKeyDown(KeyCode.Escape)) Salir();
        }

        protected override void AlSalir()
        {
            if (rutinaDiscurso != null)
            {
                StopCoroutine(rutinaDiscurso);
                rutinaDiscurso = null;
            }

            if (interfazDiscurso != null)
            {
                interfazDiscurso.AlEmpezarLinea -= AlEmpezarLinea;
                interfazDiscurso.AlTerminarLinea -= AlTerminarLinea;
                interfazDiscurso.Cerrar();
                interfazDiscurso = null;
            }
            AlTerminarLinea();
            discursoEnCurso = false;
        }
    }
}