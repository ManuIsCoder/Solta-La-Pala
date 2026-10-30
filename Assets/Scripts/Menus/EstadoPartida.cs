using System;
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

        public event Action<int, int> AlCambiarContadores;

        public event Action<float> AlCambiarTiempo;

        public event Action AlEmpezarNivel;

        public int Impacto => impacto;
        public int Sospecha => sospecha;
        public int ImpactoObjetivo => impactoObjetivo;
        public int SospechaMaxima => sospechaMaxima;

        public float TiempoRestante => tiempoRestante;

        public bool PartidaTerminada { get; private set; }

        public bool PartidaEnCurso { get; private set; }

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
            var visionHud = IndicadorVisionHUD.Instancia;
            if (visionHud == null || visionHud.EstadoActual == DetectorVisionNPC.EstadoVision.SinVer)
            {
                return;
            }
            if (!EsJugadorSospechoso())
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

        public void RegistrarSabotaje(int impactoGanado, int sospechaGanada)
        {
            if (PartidaTerminada)
            {
                return;
            }

            impacto += impactoGanado;
            sospecha = Mathf.Max(0, sospecha + sospechaGanada);
            sospechaAcumulada = sospecha;

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
            if (impacto >= impactoObjetivo)
            {
                TerminarPartida(true,
                    $"Se acabo la jornada. Impacto {impacto}/{impactoObjetivo}: saliste sin levantar sospechas.");
                return;
            }

            TerminarPartida(false,
                $"Se acabo el tiempo con {impacto}/{impactoObjetivo} de impacto. No hiciste suficiente.");
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
            if (PartidaTerminada)
            {
                return;
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
                return;
            }

            if (victoria)
            {
                GestorMenus.Instancia.MostrarVictoria(detalle);
            }
            else
            {
                GestorMenus.Instancia.MostrarDerrota(detalle);
            }
        }

        public void Reiniciar()
        {
            impacto = 0;
            sospecha = 0;
            sospechaAcumulada = sospecha;
            tiempoRestante = duracionNivel;
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
