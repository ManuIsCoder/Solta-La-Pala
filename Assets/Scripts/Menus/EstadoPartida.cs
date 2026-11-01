using System;
using System.Collections.Generic;
using SoltaLaPala.Dialogue;
using SoltaLaPala.Guardado;
using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using SoltaLaPala.Minijuegos;
using SoltaLaPala.NPC;
using SoltaLaPala.Player;
using SoltaLaPala.Sabotaje;
using UnityEngine;

namespace SoltaLaPala.Menus
{
    [Serializable]
    public class RegistroTareaSaboteada
    {
        public string tarea;
        public int impacto;
        public bool testigo;
    }

    public class EstadoPartida : MonoBehaviour, IGuardable
    {
        public static EstadoPartida Instancia { get; private set; }

        [Header("Objetivos del nivel")]
        [Tooltip("Impacto que hay que acumular para ganar el nivel.")]
        public int impactoObjetivo = 10;
        [Tooltip("Si la sospecha llega a este valor, te descubren y pierdes.")]
        public int sospechaMaxima = 100;
        [Tooltip("Segundos que dura el nivel. Si llega a 0 se pierde.")]
        public float duracionNivel = 300f;

        [Header("Ajustes de Percepción")]
        [Tooltip("Velocidad de subida de sospecha por segundo cuando te ven de lejos.")]
        public float velocidadSospechaLejos = 10f;
        [Tooltip("Velocidad de subida de sospecha por segundo cuando te ven de cerca (ojo rojo).")]
        public float velocidadSospechaCerca = 25f;
        public static bool AccionSospechosaEnCurso { get; set; }
        private float sospechaAcumulada;

        [Header("Estado actual")]
        [SerializeField] private int impacto;
        [SerializeField] private int sospecha;
        [SerializeField] private float tiempoRestante;
        private readonly List<RegistroTareaSaboteada> tareasSaboteadas =
            new List<RegistroTareaSaboteada>();
        private bool requiereMisionDiscurso;
        private bool misionDiscursoCompletada;

        public event Action<int, int> AlCambiarContadores;

        public event Action<float> AlCambiarTiempo;

        public event Action AlEmpezarNivel;

        public int Impacto => impacto;
        public int Sospecha => sospecha;
        public int ImpactoObjetivo => impactoObjetivo;
        public int SospechaMaxima => sospechaMaxima;
        public IReadOnlyList<RegistroTareaSaboteada> TareasSaboteadas => tareasSaboteadas;
        public bool HuboTestigo { get; private set; }

        public float TiempoRestante => tiempoRestante;

        public bool PartidaTerminada { get; private set; }

        public bool PartidaEnCurso { get; private set; }

        public int MisionesPendientes => ObtenerMisionesPendientes();
        public int MisionesEntregaPendientes => ObtenerMisionesEntregaPendientes();
        public bool RequiereMisionDiscurso => requiereMisionDiscurso;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            tiempoRestante = duracionNivel;
        }

        public void EmpezarNivel()
        {
            PartidaEnCurso = true;
            AlEmpezarNivel?.Invoke();
        }

        public void ConfigurarMisionDiscurso(bool requerida)
        {
            requiereMisionDiscurso = requerida;
            misionDiscursoCompletada = false;
        }

        public bool CompletarMisionDiscurso()
        {
            if (!requiereMisionDiscurso)
            {
                return true;
            }

            if (ObtenerMisionesEntregaPendientes() > 0)
            {
                return false;
            }

            misionDiscursoCompletada = true;
            return true;
        }

        public void ReiniciarNivel()
        {
            Reiniciar();

            GestorMinijuego.Instancia?.ForzarCierre();

            ObjetoSoltado.DestruirTodos();
            RegistroObjetosConsumidos.Instancia?.Limpiar();

            ReiniciarMisiones();
            ReiniciarDialogos();
            ReiniciarNpcs();
            ReiniciarJugador();
            ReiniciarMinijuegos();
            ReiniciarTareas();
        }

        private static void ReiniciarMinijuegos()
        {
            foreach (MinijuegoBase minijuego in
                     FindObjectsByType<MinijuegoBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                minijuego.Reiniciar();
            }
        }

        private static void ReiniciarNpcs()
        {
            foreach (PatrullaNPC patrulla in
                     FindObjectsByType<PatrullaNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                patrulla.Reiniciar();
            }
        }

        private static void ReiniciarMisiones()
        {
            foreach (MisionEntrega mision in
                     FindObjectsByType<MisionEntrega>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                mision.ReiniciarEncargo();
            }
        }

        private static void ReiniciarTareas()
        {
            foreach (TareaNivel tarea in
                     FindObjectsByType<TareaNivel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                tarea.Reiniciar();
            }
        }

        private static void ReiniciarDialogos()
        {
            GestorDialogos.Instancia?.CancelarDialogo();

            foreach (DialogoNPC npc in
                     FindObjectsByType<DialogoNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                npc.ReiniciarConversacion();
            }
        }

        private static void ReiniciarJugador()
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");

            if (jugador == null)
            {
                return;
            }

            jugador.GetComponent<InventarioJugador>()?.Vaciar();
            jugador.GetComponent<MovimientoJugador>()?.VolverAlInicio();

            FindFirstObjectByType<CamaraTerceraPersona>()?.VolverAlInicio();
        }

        private void Update()
        {
            if (!PartidaEnCurso || PartidaTerminada)
            {
                return;
            }

            tiempoRestante -= Time.deltaTime;

            if (tiempoRestante <= 0f)
            {
                tiempoRestante = 0f;
                AlCambiarTiempo?.Invoke(tiempoRestante);

                ResolverFinalPorTiempo();
                return;
            }

            AlCambiarTiempo?.Invoke(tiempoRestante);

            ActualizarSospechaContinua();
        }

        private void ActualizarSospechaContinua()
        {
            if (!EsJugadorSospechoso())
            {
                return;
            }

            bool alguienLoVe = HayNPCViendoAlJugador();
            HuboTestigo |= alguienLoVe;

            var visionHud = IndicadorVisionHUD.Instancia;
            if (visionHud == null || visionHud.EstadoActual == DetectorVisionNPC.EstadoVision.SinVer)
            {
                return;
            }

            float velocidad = (visionHud.EstadoActual == DetectorVisionNPC.EstadoVision.VeCerca)
                ? velocidadSospechaCerca
                : velocidadSospechaLejos;
            sospechaAcumulada += velocidad * Time.deltaTime;
            int nuevaSospecha = Mathf.Min(sospechaMaxima, Mathf.FloorToInt(sospechaAcumulada));
            if (nuevaSospecha != sospecha)
            {
                sospecha = nuevaSospecha;
                AlCambiarContadores?.Invoke(impacto, sospecha);
                ComprobarFinal();
            }
        }
        public bool EsJugadorSospechoso()
        {
            if (AccionSospechosaEnCurso)
            {
                return true;
            }
            if (MenuSabotajeTaza.Instancia != null && MenuSabotajeTaza.Instancia.Abierto)
            {
                return true;
            }
            var inv = InventarioJugador.DelJugador();
            if (inv != null && inv.ItemSeleccionado != null && inv.ItemSeleccionado.esSospechoso)
            {
                return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
            }
        }

        public void RegistrarSabotaje(int impactoGanado, int sospechaGanada, string tarea = null)
        {
            if (PartidaTerminada)
            {
                return;
            }

            impacto += impactoGanado;
            sospecha = Mathf.Max(0, sospecha + sospechaGanada);
            sospechaAcumulada = sospecha;

            bool testigo = HayNPCViendoAlJugador();
            HuboTestigo |= testigo;
            if (impactoGanado > 0 && !string.IsNullOrWhiteSpace(tarea))
            {
                tareasSaboteadas.Add(new RegistroTareaSaboteada
                {
                    tarea = tarea,
                    impacto = impactoGanado,
                    testigo = testigo
                });
            }

            AlCambiarContadores?.Invoke(impacto, sospecha);

            ComprobarFinal();
        }

        public void CambiarSospecha(int cantidad)
        {
            if (cantidad == 0)
            {
                return;
            }

            RegistrarSabotaje(0, cantidad);
        }

        public void EstablecerSospecha(int valor)
        {
            if (PartidaTerminada)
            {
                return;
            }

            sospecha = Mathf.Clamp(valor, 0, sospechaMaxima);
            sospechaAcumulada = sospecha;
            AlCambiarContadores?.Invoke(impacto, sospecha);
            ComprobarFinal();
        }

        public void ReducirSospecha(int cantidad)
        {
            if (PartidaTerminada || cantidad <= 0)
            {
                return;
            }

            sospecha = Mathf.Max(0, sospecha - cantidad);
            AlCambiarContadores?.Invoke(impacto, sospecha);
        }

        private void ComprobarFinal()
        {
            if (sospecha >= sospechaMaxima)
            {
                DispararDialogoDeCaptura();

                TerminarPartida(false, "Te atraparon con las manos en la masa.");
            }
        }

        private void ResolverFinalPorTiempo()
        {
            int misionesPendientes = ObtenerMisionesPendientes();
            if (impacto >= impactoObjetivo && misionesPendientes == 0)
            {
                TerminarPartida(true,
                    $"Se acabo la jornada. Impacto {impacto}/{impactoObjetivo}: saliste sin levantar sospechas.");
                return;
            }

            string detalle = misionesPendientes > 0
                ? $"Te faltan {misionesPendientes} misiones por completar."
                : "No has hecho suficiente sabotaje.";
            TerminarPartida(false, detalle);
        }

        private int ObtenerMisionesPendientes()
        {
            int pendientes = ObtenerMisionesEntregaPendientesSinTarea()
                + ObtenerTareasPendientes();
            bool discursoTieneTarea = TieneTarea(nameof(MinijuegoDiscurso));
            if (requiereMisionDiscurso && !misionDiscursoCompletada && !discursoTieneTarea)
            {
                pendientes++;
            }

            return pendientes;
        }

        private static int ObtenerMisionesEntregaPendientesSinTarea()
        {
            int pendientes = 0;
            MisionEntrega[] misiones = FindObjectsByType<MisionEntrega>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (MisionEntrega mision in misiones)
            {
                if (mision != null && !mision.Completada && mision.GetComponent<TareaNivel>() == null)
                {
                    pendientes++;
                }
            }

            return pendientes;
        }

        private static int ObtenerMisionesEntregaPendientes()
        {
            int pendientes = 0;
            MisionEntrega[] misiones = FindObjectsByType<MisionEntrega>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (MisionEntrega mision in misiones)
            {
                if (mision != null && !mision.Completada)
                {
                    pendientes++;
                }
            }

            return pendientes;
        }

        private static int ObtenerTareasPendientes()
        {
            int pendientes = 0;
            TareaNivel[] tareas = FindObjectsByType<TareaNivel>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (TareaNivel tarea in tareas)
            {
                if (tarea != null && !tarea.Completada)
                {
                    pendientes++;
                }
            }

            return pendientes;
        }

        private static bool TieneTarea(string id)
        {
            TareaNivel[] tareas = FindObjectsByType<TareaNivel>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (TareaNivel tarea in tareas)
            {
                if (tarea != null && tarea.Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CompletarTareaNivel(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            bool completada = false;
            TareaNivel[] tareas = FindObjectsByType<TareaNivel>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (TareaNivel tarea in tareas)
            {
                if (tarea != null && tarea.Id == id)
                {
                    completada |= tarea.Completar();
                }
            }

            return completada;
        }

        public bool EstaCompletaTarea(string id)
        {
            TareaNivel[] tareas = FindObjectsByType<TareaNivel>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (TareaNivel tarea in tareas)
            {
                if (tarea != null && tarea.Id == id)
                {
                    return tarea.Completada;
                }
            }

            return false;
        }

        private static bool HayNPCViendoAlJugador()
        {
            DetectorVisionNPC[] detectores =
                FindObjectsByType<DetectorVisionNPC>(FindObjectsSortMode.None);

            foreach (DetectorVisionNPC detector in detectores)
            {
                if (detector != null && detector.isActiveAndEnabled
                    && detector.Estado != DetectorVisionNPC.EstadoVision.SinVer)
                {
                    return true;
                }
            }

            return false;
        }

        private void DispararDialogoDeCaptura()
        {
            DetectorVisionNPC[] detectores =
                FindObjectsByType<DetectorVisionNPC>(FindObjectsSortMode.None);

            if (detectores.Length == 0)
            {
                return;
            }

            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            Vector3 posicionJugador = jugador != null ? jugador.transform.position : transform.position;

            DetectorVisionNPC elegido = null;
            float mejorDistancia = float.MaxValue;
            bool elegidoTeVe = false;

            foreach (DetectorVisionNPC detector in detectores)
            {
                bool teVe = detector.Estado != DetectorVisionNPC.EstadoVision.SinVer;
                float distancia = Vector3.Distance(detector.transform.position, posicionJugador);

                bool mejor = elegido == null
                    || (teVe && !elegidoTeVe)
                    || (teVe == elegidoTeVe && distancia < mejorDistancia);

                if (mejor)
                {
                    elegido = detector;
                    mejorDistancia = distancia;
                    elegidoTeVe = teVe;
                }
            }

            elegido?.GetComponent<DialogoNPC>()?.DispararDetectado();
        }

        public void TerminarPartida(bool victoria, string detalle = null)
        {
            IntentarTerminarPartida(victoria, detalle);
        }

        public bool IntentarTerminarPartida(bool victoria, string detalle = null)
        {
            if (PartidaTerminada)
            {
                return false;
            }

            if (victoria && ObtenerMisionesPendientes() > 0)
            {
                Debug.LogWarning(
                    $"[EstadoPartida] No se puede ganar todavía: quedan {ObtenerMisionesPendientes()} misiones pendientes.");
                return false;
            }

            PartidaTerminada = true;
            PartidaEnCurso = false;

            int nivel = ProgresoNiveles.NivelActual;
            ProgresoNiveles.GuardarResultado(nivel, impacto, sospecha);

            if (victoria)
            {
                ProgresoNiveles.CompletarNivel(nivel);
            }

            GuardadoPartida.Borrar();

            if (GestorMenus.Instancia == null)
            {
                Debug.LogWarning("[EstadoPartida] No hay GestorMenus en la escena, no se puede mostrar el final.");
                return true;
            }

            if (victoria)
            {
                GestorMenus.Instancia.MostrarVictoria(detalle);
            }
            else
            {
                GestorMenus.Instancia.MostrarDerrota(detalle);
            }

            return true;
        }

        public void Reiniciar()
        {
            impacto = 0;
            sospecha = 0;
            sospechaAcumulada = sospecha;
            tiempoRestante = duracionNivel;
            tareasSaboteadas.Clear();
            HuboTestigo = false;
            misionDiscursoCompletada = false;
            PartidaTerminada = false;

            AccionSospechosaEnCurso = false;

            PartidaEnCurso = false;
            AlCambiarContadores?.Invoke(impacto, sospecha);
            AlCambiarTiempo?.Invoke(tiempoRestante);
        }

        public void Capturar(DatosPartidaGuardada datos)
        {
            datos.impacto = impacto;
            datos.sospecha = sospecha;
            datos.tiempoRestante = tiempoRestante;
        }

        public void Restaurar(DatosPartidaGuardada datos)
        {
            impacto = datos.impacto;
            sospecha = datos.sospecha;
            sospechaAcumulada = sospecha;

            tiempoRestante = datos.tiempoRestante > 0f ? datos.tiempoRestante : duracionNivel;

            PartidaTerminada = false;

            AlCambiarContadores?.Invoke(impacto, sospecha);
            AlCambiarTiempo?.Invoke(tiempoRestante);
        }
    }
}
