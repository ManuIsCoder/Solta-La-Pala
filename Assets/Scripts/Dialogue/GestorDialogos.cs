using System.Collections.Generic;
using SoltaLaPala.Guardado;
using SoltaLaPala.Interaction;
using SoltaLaPala.Menus;
using SoltaLaPala.Player;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    public class GestorDialogos : MonoBehaviour, IGuardable
    {
        public static GestorDialogos Instancia { get; private set; }

        [Header("Referencias")]
        public InterfazDialogo interfaz;
        public MovimientoJugador movimientoJugador;
        public CamaraTerceraPersona camaraJugador;
        public InteractorJugador interactorJugador;

        private DialogoNPC npcActual;
        private TipoDialogo tipoActual;

        private bool fraseSuelta;
        private List<LineaDialogo> lineasActuales = new List<LineaDialogo>();
        private int indiceLinea;

        private bool esperandoRespuesta;

        private LineaDialogo LineaActual =>
            indiceLinea >= 0 && indiceLinea < lineasActuales.Count ? lineasActuales[indiceLinea] : null;

        public bool DialogoActivo { get; private set; }

        public DialogoNPC NpcActual => npcActual;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            if (interfaz == null)
                interfaz = FindFirstObjectByType<InterfazDialogo>();
            if (movimientoJugador == null)
                movimientoJugador = FindFirstObjectByType<MovimientoJugador>();
            if (camaraJugador == null)
                camaraJugador = FindFirstObjectByType<CamaraTerceraPersona>();
            if (interactorJugador == null)
                interactorJugador = FindFirstObjectByType<InteractorJugador>();
        }

        private void Update()
        {
            if (!DialogoActivo) return;

            if (GestorMenus.Instancia != null && GestorMenus.Instancia.HayMenuAbierto) return;

            if (esperandoRespuesta)
            {
                LeerRespuestaPorTeclado();
                return;
            }

            LineaDialogo linea = LineaActual;
            if (linea != null && linea.TieneOpciones && interfaz != null && !interfaz.Escribiendo)
            {
                MostrarOpciones(linea);
                return;
            }

            if (ControlesJuego.Pulsada(AccionJuego.AvanzarDialogo))
            {
                AvanzarDialogo();
            }
        }

        private void MostrarOpciones(LineaDialogo linea)
        {
            esperandoRespuesta = true;

            List<string> textos = new List<string>(linea.opciones.Count);
            foreach (OpcionDialogo opcion in linea.opciones)
            {
                textos.Add(opcion.texto);
            }

            interfaz.MostrarOpciones(textos, ElegirRespuesta);
        }

        private void LeerRespuestaPorTeclado()
        {
            LineaDialogo linea = LineaActual;
            if (linea == null)
            {
                return;
            }

            int cantidad = Mathf.Min(linea.opciones.Count, 9);
            for (int i = 0; i < cantidad; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    ElegirRespuesta(i);
                    return;
                }
            }
        }

        private void ElegirRespuesta(int indice)
        {
            LineaDialogo linea = LineaActual;
            if (!esperandoRespuesta || linea == null || indice < 0 || indice >= linea.opciones.Count)
            {
                return;
            }

            OpcionDialogo opcion = linea.opciones[indice];
            esperandoRespuesta = false;
            interfaz?.OcultarOpciones();

            if (!string.IsNullOrWhiteSpace(opcion.reaccion))
            {
                lineasActuales.Insert(indiceLinea + 1, new LineaDialogo(opcion.reaccion));
            }

            PasarALineaSiguiente();

            EstadoPartida.Instancia?.CambiarSospecha(opcion.sospecha);
        }

        public void EmpezarDialogo(DialogoNPC npc, TipoDialogo tipo)
        {
            if (DialogoActivo || npc == null) return;

            npcActual = npc;
            tipoActual = tipo;
            fraseSuelta = false;

            lineasActuales = CargadorDialogos.CargarLineas(npc.id, tipo);
            if (lineasActuales == null || lineasActuales.Count == 0)
            {
                if (tipo != TipoDialogo.Perpetuo)
                {
                    tipoActual = TipoDialogo.Perpetuo;
                    lineasActuales = CargadorDialogos.CargarLineas(npc.id, TipoDialogo.Perpetuo);
                }

                if (lineasActuales == null || lineasActuales.Count == 0)
                {
                    Debug.LogWarning($"[GestorDialogos] El NPC {npc.nombre} ({npc.id}) no tiene líneas de diálogo disponibles.");
                    return;
                }
            }

            DialogoActivo = true;
            indiceLinea = 0;
            esperandoRespuesta = false;

            BloquearJugador(true);
            GestorMenus.Instancia?.RefrescarTiempo();

            if (interfaz != null)
            {
                interfaz.Abrir(npc.nombre);
            }

            MostrarLinea(indiceLinea);
        }

        public void MostrarFraseSuelta(DialogoNPC npc, string frase)
        {
            if (DialogoActivo || npc == null || string.IsNullOrWhiteSpace(frase))
            {
                return;
            }

            npcActual = npc;
            tipoActual = TipoDialogo.Perpetuo;

            fraseSuelta = true;

            lineasActuales = new List<LineaDialogo> { new LineaDialogo(frase) };
            indiceLinea = 0;
            esperandoRespuesta = false;
            DialogoActivo = true;

            BloquearJugador(true);
            GestorMenus.Instancia?.RefrescarTiempo();

            if (interfaz != null)
            {
                interfaz.Abrir(npc.nombre);
            }

            MostrarLinea(indiceLinea);
        }

        private void AvanzarDialogo()
        {
            if (interfaz != null && interfaz.Escribiendo)
            {
                interfaz.CompletarLinea();
                return;
            }

            if (esperandoRespuesta)
            {
                return;
            }

            PasarALineaSiguiente();
        }

        private void PasarALineaSiguiente()
        {
            indiceLinea++;
            if (indiceLinea < lineasActuales.Count)
            {
                MostrarLinea(indiceLinea);
            }
            else
            {
                TerminarDialogo();
            }
        }

        private void MostrarLinea(int indice)
        {
            if (indice >= 0 && indice < lineasActuales.Count && interfaz != null)
            {
                interfaz.MostrarLinea(lineasActuales[indice].texto);
            }
        }

        public void CancelarDialogo()
        {
            if (!DialogoActivo)
            {
                return;
            }

            fraseSuelta = true;
            TerminarDialogo();
        }

        private void TerminarDialogo()
        {
            DialogoActivo = false;
            esperandoRespuesta = false;

            if (interfaz != null)
            {
                interfaz.Cerrar();
            }

            BloquearJugador(false);
            GestorMenus.Instancia?.RefrescarTiempo();

            if (npcActual != null)
            {
                if (!fraseSuelta)
                {
                    npcActual.AlTerminarDialogo(tipoActual);
                }

                npcActual = null;
            }

            fraseSuelta = false;
        }

        private void BloquearJugador(bool bloqueado)
        {
            if (movimientoJugador != null)
            {
                movimientoJugador.BloquearMovimiento(bloqueado);
            }

            if (interactorJugador != null)
            {
                interactorJugador.BloquearInteraccion(bloqueado);
            }

            if (camaraJugador != null)
            {
                camaraJugador.BloquearCamara(bloqueado);
            }

            CamaraTerceraPersona.RegistrarUiAbierta(bloqueado);
        }

        public void Capturar(DatosPartidaGuardada datos)
        {
            List<string> progreso = new List<string>();

            foreach (DialogoNPC npc in BuscarNPCs())
            {
                if (!string.IsNullOrEmpty(npc.id))
                {
                    progreso.Add($"{npc.id}:{npc.tipoActual}");
                }
            }

            datos.progresoDialogos = progreso.ToArray();
        }

        public void Restaurar(DatosPartidaGuardada datos)
        {
            if (DialogoActivo)
            {
                TerminarDialogo();
            }

            if (datos.progresoDialogos == null)
            {
                return;
            }

            Dictionary<string, TipoDialogo> porId = new Dictionary<string, TipoDialogo>();

            foreach (string entrada in datos.progresoDialogos)
            {
                int separador = entrada.LastIndexOf(':');
                if (separador <= 0)
                {
                    continue;
                }

                string id = entrada.Substring(0, separador);
                string tipoTexto = entrada.Substring(separador + 1);

                if (System.Enum.TryParse(tipoTexto, out TipoDialogo tipo))
                {
                    porId[id] = tipo;
                }
            }

            foreach (DialogoNPC npc in BuscarNPCs())
            {
                if (npc.id == null || !porId.TryGetValue(npc.id, out TipoDialogo tipo))
                {
                    continue;
                }

                npc.tipoActual = tipo;

                if (tipo != TipoDialogo.Mision)
                {
                    npc.GetComponent<MisionEntrega>()?.MarcarComoCompletada();
                }
            }
        }

        private static DialogoNPC[] BuscarNPCs()
        {
            return FindObjectsByType<DialogoNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
    }
}
