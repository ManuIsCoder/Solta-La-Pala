using System.Collections.Generic;
using SoltaLaPala.Menus;
using UnityEngine;

namespace SoltaLaPala.Guardado
{
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

        public bool GuardarAhora()
        {
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
