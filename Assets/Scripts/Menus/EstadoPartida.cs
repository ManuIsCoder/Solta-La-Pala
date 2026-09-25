using System;
using SoltaLaPala.Guardado;
using UnityEngine;

namespace SoltaLaPala.Menus
{
    // Lleva la cuenta de Impacto y Sospecha de la partida en curso y decide
    // cuando se gana o se pierde.
    //
    // Los sabotajes llaman a RegistrarSabotaje() en vez de hacer solo un Debug.Log,
    // y esta clase avisa al GestorMenus cuando hay que mostrar victoria o derrota.
    public class EstadoPartida : MonoBehaviour, IGuardable
    {
        public static EstadoPartida Instancia { get; private set; }

        [Header("Objetivos del nivel")]
        [Tooltip("Impacto que hay que acumular para ganar el nivel.")]
        public int impactoObjetivo = 10;
        [Tooltip("Si la sospecha llega a este valor, te pillan y pierdes.")]
        public int sospechaMaxima = 100;
        [Tooltip("Segundos que dura el nivel. Si llega a 0 se pierde.")]
        public float duracionNivel = 300f;

        [Header("Estado actual")]
        [SerializeField] private int impacto;
        [SerializeField] private int sospecha;
        [SerializeField] private float tiempoRestante;

        // Avisa cuando cambian los contadores, para que un HUD pueda pintarlos.
        public event Action<int, int> AlCambiarContadores;

        // Avisa cada vez que corre el reloj, con los segundos que quedan.
        public event Action<float> AlCambiarTiempo;

        public int Impacto => impacto;
        public int Sospecha => sospecha;
        public int ImpactoObjetivo => impactoObjetivo;
        public int SospechaMaxima => sospechaMaxima;

        // Segundos que quedan para que se acabe el nivel.
        public float TiempoRestante => tiempoRestante;

        // True cuando la partida ya termino, para no disparar el final dos veces.
        public bool PartidaTerminada { get; private set; }

        // True mientras hay un nivel jugandose. El reloj solo corre si esto esta
        // encendido, para que no se gaste tiempo en el menu principal.
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

        // Arranca el reloj. Lo llama GestorMenus al entrar al juego, tanto en
        // partida nueva como al cargar una guardada.
        public void EmpezarNivel()
        {
            PartidaEnCurso = true;
        }

        // Corre el reloj del nivel y pierde la partida al llegar a 0.
        //
        // Usa Time.deltaTime y no unscaledDeltaTime a proposito: GestorMenus pone
        // timeScale a 0 con cualquier menu abierto, asi que el reloj se pausa solo
        // mientras el jugador esta en la pausa o la configuracion.
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
                TerminarPartida(false, "Se acabo el tiempo.");
                return;
            }

            AlCambiarTiempo?.Invoke(tiempoRestante);
        }

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
            }
        }

        // Suma el resultado de un sabotaje y comprueba si la partida termino.
        public void RegistrarSabotaje(int impactoGanado, int sospechaGanada)
        {
            if (PartidaTerminada)
            {
                return;
            }

            impacto += impactoGanado;
            sospecha = Mathf.Max(0, sospecha + sospechaGanada);

            AlCambiarContadores?.Invoke(impacto, sospecha);

            ComprobarFinal();
        }

        // Baja la sospecha (por ejemplo al disimular o esperar).
        public void ReducirSospecha(int cantidad)
        {
            if (PartidaTerminada || cantidad <= 0)
            {
                return;
            }

            sospecha = Mathf.Max(0, sospecha - cantidad);
            AlCambiarContadores?.Invoke(impacto, sospecha);
        }

        // Pierdes si te pillan; ganas si llegas al impacto objetivo sin que te pillen.
        // El orden importa: la derrota manda sobre la victoria en el mismo sabotaje.
        private void ComprobarFinal()
        {
            if (sospecha >= sospechaMaxima)
            {
                TerminarPartida(false, "Te pillaron con las manos en la masa.");
            }
            else if (impacto >= impactoObjetivo)
            {
                TerminarPartida(true, "Saliste de la oficina sin levantar sospechas.");
            }
        }

        // Cierra la partida, guarda el resultado y abre la pantalla de final.
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

            // Un nivel terminado no se retoma: dejar el guardado vivo haria que
            // "Continuar" devolviera al jugador a una partida ya resuelta.
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

        // Deja los contadores a cero para volver a jugar el nivel.
        public void Reiniciar()
        {
            impacto = 0;
            sospecha = 0;
            tiempoRestante = duracionNivel;
            PartidaTerminada = false;

            // Reiniciar deja el nivel listo, no lanzado: lo arranca EmpezarNivel
            // cuando el jugador vuelve al juego.
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

            // Un guardado viejo (sin tiempo escrito) traeria 0 y mataria la partida
            // en el primer frame. Si no hay tiempo valido, se arranca con el completo.
            tiempoRestante = datos.tiempoRestante > 0f ? datos.tiempoRestante : duracionNivel;

            // El guardado solo existe para partidas en curso, asi que cargar una
            // siempre deja la partida abierta.
            PartidaTerminada = false;

            AlCambiarContadores?.Invoke(impacto, sospecha);
            AlCambiarTiempo?.Invoke(tiempoRestante);
        }
    }
}
