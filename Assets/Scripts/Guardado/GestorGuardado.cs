using System.Collections.Generic;
using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.Guardado
{
    // Orquesta el guardado: junta el estado de todos los IGuardable de la escena
    // en un DTO y lo manda a disco, o al contrario.
    //
    // No sabe nada del inventario, del jugador ni de los dialogos: solo conoce
    // IGuardable. Lo crea ArranqueMenus, igual que al EstadoPartida.
    public class GestorGuardado : MonoBehaviour
    {
        public static GestorGuardado Instancia { get; private set; }

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
        }

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
            }
        }

        // Guarda la partida en curso. Devuelve true si se pudo escribir.
        public bool GuardarAhora()
        {
            // Una partida ya terminada no se guarda: el jugador vio victoria o
            // derrota y su guardado ya se borro en EstadoPartida.TerminarPartida.
            if (EstadoPartida.Instancia != null && EstadoPartida.Instancia.PartidaTerminada)
            {
                return false;
            }

            DatosPartidaGuardada datos = new DatosPartidaGuardada
            {
                nivel = ProgresoNiveles.NivelActual
            };

            foreach (IGuardable guardable in BuscarGuardables())
            {
                guardable.Capturar(datos);
            }

            return GuardadoPartida.Guardar(datos);
        }

        // Lee el guardado y pone a todos los sistemas en ese estado.
        // Devuelve false si no habia nada que cargar.
        public bool CargarAhora()
        {
            DatosPartidaGuardada datos = GuardadoPartida.Cargar();

            if (datos == null)
            {
                return false;
            }

            ProgresoNiveles.NivelActual = datos.nivel;

            foreach (IGuardable guardable in BuscarGuardables())
            {
                guardable.Restaurar(datos);
            }

            Debug.Log($"[GestorGuardado] Partida cargada: nivel {datos.nivel}, " +
                      $"impacto {datos.impacto}, sospecha {datos.sospecha}.");
            return true;
        }

        // Busca los IGuardable de la escena.
        //
        // Se hace en cada guardado/carga en vez de cachear: el jugador y los
        // menus aparecen y desaparecen, y una referencia vieja a un objeto
        // destruido guardaria estado equivocado en silencio.
        //
        // Incluye los inactivos porque la UI de inventario puede estar oculta
        // en el momento de guardar.
        private static List<IGuardable> BuscarGuardables()
        {
            List<IGuardable> encontrados = new List<IGuardable>();

            foreach (MonoBehaviour componente in
                     FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (componente is IGuardable guardable)
                {
                    encontrados.Add(guardable);
                }
            }

            return encontrados;
        }
    }
}
