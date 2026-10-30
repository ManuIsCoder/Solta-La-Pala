using System.Collections.Generic;
using UnityEngine;

namespace SoltaLaPala.Guardado
{
    public class RegistroObjetosConsumidos : MonoBehaviour, IGuardable
    {
        public static RegistroObjetosConsumidos Instancia { get; private set; }

        private readonly HashSet<string> consumidos = new HashSet<string>();

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

        public void Marcar(GameObject objeto)
        {
            if (objeto == null)
            {
                return;
            }

            IdentificadorObjeto identificador = objeto.GetComponent<IdentificadorObjeto>();

            if (identificador == null || !identificador.TieneId)
            {
                Debug.LogWarning($"[RegistroObjetosConsumidos] '{objeto.name}' no tiene " +
                                 "IdentificadorObjeto: al cargar la partida volvera a aparecer.", objeto);
                return;
            }

            consumidos.Add(identificador.Id);
        }

        public bool EstaConsumido(string id)
        {
            return !string.IsNullOrEmpty(id) && consumidos.Contains(id);
        }

        public void Capturar(DatosPartidaGuardada datos)
        {
            datos.objetosConsumidos = new List<string>(consumidos).ToArray();
        }

        public void Restaurar(DatosPartidaGuardada datos)
        {
            consumidos.Clear();

            if (datos.objetosConsumidos != null)
            {
                foreach (string id in datos.objetosConsumidos)
                {
                    consumidos.Add(id);
                }
            }

            AplicarALaEscena();
        }

        private void AplicarALaEscena()
        {
            foreach (IdentificadorObjeto identificador in
                     FindObjectsByType<IdentificadorObjeto>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!identificador.TieneId)
                {
                    continue;
                }

                identificador.gameObject.SetActive(!EstaConsumido(identificador.Id));
            }
        }

        public void Limpiar()
        {
            consumidos.Clear();
            AplicarALaEscena();
        }
    }
}
