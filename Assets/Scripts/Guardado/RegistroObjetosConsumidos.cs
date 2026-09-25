using System.Collections.Generic;
using UnityEngine;

namespace SoltaLaPala.Guardado
{
    // Lleva la cuenta de los objetos del mundo ya gastados: items recogidos y
    // sabotajes hechos.
    //
    // ObjetoRecogible y MenuSabotajeTaza desactivan el objeto con SetActive(false)
    // en vez de destruirlo, asi que al cargar basta con volver a desactivar los que
    // esten en esta lista.
    //
    // Es un IGuardable como los demas: se registra a si mismo en el DTO.
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

        // Marca un objeto como gastado. Lo llaman los recogibles y los sabotajes
        // justo despues de desactivar el objeto.
        public void Marcar(GameObject objeto)
        {
            if (objeto == null)
            {
                return;
            }

            IdentificadorObjeto identificador = objeto.GetComponent<IdentificadorObjeto>();

            if (identificador == null || !identificador.TieneId)
            {
                // Sin id no se puede recordar. Se avisa una vez, al usarlo, que es
                // cuando el aviso sirve para arreglar la escena.
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

        // Desactiva los objetos de la escena que ya estaban gastados, y reactiva
        // los que no. Lo segundo importa al cargar una partida encima de otra ya
        // empezada: si no, seguirian escondidos objetos que en este guardado
        // todavia no se habian tocado.
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

        // Al empezar un nivel nuevo no hay nada gastado todavia.
        public void Limpiar()
        {
            consumidos.Clear();
            AplicarALaEscena();
        }
    }
}
