using System.Collections.Generic;
using SoltaLaPala.Guardado;
using SoltaLaPala.Interaction;
using SoltaLaPala.Menus;
using SoltaLaPala.Player;
using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    // Controla la conversacion activa: carga las lineas, las va pasando
    // y bloquea al jugador mientras dura el dialogo.
    // Con ESPACIO se completa la linea que se esta escribiendo o se pasa a la siguiente.
    //
    // Es tambien el que guarda el progreso de dialogo de todos los NPC: el estado
    // vive repartido en cada DialogoNPC, pero si cada uno escribiera en el DTO por
    // su cuenta se pisarian el mismo array.
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

        // True si lo que se esta mostrando es una frase suelta y no un dialogo
        // de archivo: al cerrarse no avisa al NPC ni avanza su estado.
        private bool fraseSuelta;
        private List<LineaDialogo> lineasActuales = new List<LineaDialogo>();
        private int indiceLinea;

        // True mientras se esperan las respuestas de la linea actual: ESPACIO no
        // avanza, hay que elegir una con el mouse o con los numeros.
        private bool esperandoRespuesta;

        private LineaDialogo LineaActual =>
            indiceLinea >= 0 && indiceLinea < lineasActuales.Count ? lineasActuales[indiceLinea] : null;

        // True mientras hay una conversacion en marcha.
        public bool DialogoActivo { get; private set; }

        // NPC con el que se esta hablando, o null si no hay dialogo. Lo consultan
        // los NPC para saber si la conversacion es suya: los demas siguen a lo suyo.
        public DialogoNPC NpcActual => npcActual;

        // Registra el singleton y busca referencias si no fueron asignadas.
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

        // Si hay dialogo activo, lee la tecla de avance para saltar/avanzar.
        private void Update()
        {
            if (!DialogoActivo) return;

            // Con un menu abierto (pausa, configuracion) no se avanza el dialogo:
            // la misma tecla podria estar usandose para navegar la UI.
            if (GestorMenus.Instancia != null && GestorMenus.Instancia.HayMenuAbierto) return;

            if (esperandoRespuesta)
            {
                LeerRespuestaPorTeclado();
                return;
            }

            // Las respuestas aparecen recien cuando la frase termino de escribirse,
            // para que el jugador lea antes de contestar.
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

        // 1..9 de la fila de numeros o del teclado numerico. Se leen fijas y no
        // por ControlesJuego: son "elegir la opcion N", no una accion reasignable.
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

        // Aplica la sospecha de la respuesta y sigue: con la reaccion del NPC si
        // la tiene, o con la frase siguiente del archivo.
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

            // La reaccion se mete como una frase mas, justo despues de esta, asi
            // se escribe y se avanza con ESPACIO igual que el resto.
            if (!string.IsNullOrWhiteSpace(opcion.reaccion))
            {
                lineasActuales.Insert(indiceLinea + 1, new LineaDialogo(opcion.reaccion));
            }

            PasarALineaSiguiente();

            // Al final: si la sospecha llega al tope, la pantalla de derrota tiene
            // que quedar encima del dialogo y no al reves.
            EstadoPartida.Instancia?.CambiarSospecha(opcion.sospecha);
        }

        // Empieza una conversacion: carga las lineas del NPC para ese tipo,
        // bloquea movimiento e interaccion, abre el cuadro y muestra la primera linea.
        public void EmpezarDialogo(DialogoNPC npc, TipoDialogo tipo)
        {
            if (DialogoActivo || npc == null) return;

            npcActual = npc;
            tipoActual = tipo;
            fraseSuelta = false;

            lineasActuales = CargadorDialogos.CargarLineas(npc.id, tipo);
            if (lineasActuales == null || lineasActuales.Count == 0)
            {
                // Si no hay líneas para ese tipo, intentar caer a Perpetuo si no es Perpetuo
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

        // Suelta una frase concreta, sin leerla de un .txt.
        //
        // Lo usan las respuestas puntuales que dependen de lo que acaba de pasar
        // (que le entregaste, por ejemplo) y que por eso no pueden vivir en un
        // archivo de dialogo fijo.
        //
        // No toca el tipoActual del NPC: es un comentario, no un paso de la
        // conversacion.
        public void MostrarFraseSuelta(DialogoNPC npc, string frase)
        {
            if (DialogoActivo || npc == null || string.IsNullOrWhiteSpace(frase))
            {
                return;
            }

            npcActual = npc;
            tipoActual = TipoDialogo.Perpetuo;

            // Al cerrarse no se avisa al NPC: una frase suelta no debe mover su
            // estado. Sin esto, el Perpetuo de arriba pisaria el tipo al que el
            // NPC acaba de pasar (Charla, tras completar el encargo).
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

        // Avanza a la siguiente linea. Si la linea actual todavia se esta escribiendo,
        // la completa de golpe en vez de pasar. Si no quedan lineas, termina la conversacion.
        private void AvanzarDialogo()
        {
            if (interfaz != null && interfaz.Escribiendo)
            {
                interfaz.CompletarLinea();
                return;
            }

            // Con respuestas pendientes no se puede saltar: hay que elegir una.
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

        // Manda la linea indicada a la interfaz para que la escriba.
        private void MostrarLinea(int indice)
        {
            if (indice >= 0 && indice < lineasActuales.Count && interfaz != null)
            {
                interfaz.MostrarLinea(lineasActuales[indice].texto);
            }
        }

        // Corta el dialogo en marcha sin avisar al NPC, como si fuera una frase
        // suelta: ni avanza su estado ni lo marca como escuchado.
        //
        // Lo usa el reinicio de nivel: un dialogo de captura puede quedar abierto
        // detras de la pantalla de derrota, y con el tiempo congelado por el
        // dialogo el nivel nuevo arrancaria parado.
        public void CancelarDialogo()
        {
            if (!DialogoActivo)
            {
                return;
            }

            fraseSuelta = true;
            TerminarDialogo();
        }

        // Cierra el cuadro, devuelve el control al jugador y avisa al NPC
        // para que aplique la transicion de tipo de dialogo.
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

        // Bloquea o desbloquea movimiento, camara e interaccion del jugador,
        // y libera el cursor del raton mientras dura el dialogo.
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

            // La camara tiene que congelarse ademas del movimiento: si no, el raton
            // que usas para elegir opciones del dialogo sigue girando la camara.
            // BloquearCamara ya se encarga de liberar y recapturar el cursor.
            if (camaraJugador != null)
            {
                camaraJugador.BloquearCamara(bloqueado);
            }

            // Ademas del bloqueo directo se avisa por el contador estatico, que es
            // lo que consulta la camara aunque este dialogo no tenga referencia a
            // ella (o aparezca una camara nueva a mitad de conversacion).
            CamaraTerceraPersona.RegistrarUiAbierta(bloqueado);
        }

        // Guarda el tipo de dialogo en el que quedo cada NPC, como "id:tipo".
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
            // Si habia un dialogo abierto al guardar, cargar no debe dejar al
            // jugador bloqueado hablando con nadie.
            if (DialogoActivo)
            {
                TerminarDialogo();
            }

            if (datos.progresoDialogos == null)
            {
                return;
            }

            // Se indexa el guardado y no los NPC: hay pocos de cada, pero asi un
            // id que ya no exista en la escena simplemente se ignora.
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

                // Un tipo que ya no existe en el enum no tumba la carga: ese NPC
                // se queda con el que tenga puesto en la escena.
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

                // Un NPC con encargo que ya no esta en Mision es uno al que ya le
                // entregaste: sin esto podrias volver a cobrar la recompensa.
                if (tipo != TipoDialogo.Mision)
                {
                    npc.GetComponent<MisionEntrega>()?.MarcarComoCompletada();
                }
            }
        }

        // Incluye los inactivos: un NPC puede estar desactivado al guardar y su
        // progreso sigue importando.
        private static DialogoNPC[] BuscarNPCs()
        {
            return FindObjectsByType<DialogoNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
    }
}
