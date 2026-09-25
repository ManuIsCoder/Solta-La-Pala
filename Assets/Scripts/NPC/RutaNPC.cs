using System.Collections.Generic;
using UnityEngine;

namespace SoltaLaPala.NPC
{
    // Recorrido que sigue un NPC, marcado con puntos en la escena.
    //
    // COMO SE USA:
    //   1. Crear un GameObject vacio (ej. "RutaRecepcion") y ponerle este componente.
    //   2. Crear GameObjects vacios como hijos suyos, uno por parada.
    //   3. Moverlos en la escena. Los gizmos dibujan el camino en amarillo.
    //   4. Arrastrar la ruta al campo 'ruta' del PatrullaNPC del NPC.
    //
    // Los puntos son hijos y no una lista de Vector3 para poder colocarlos a mano
    // en la vista de escena en vez de escribir coordenadas a ciegas.
    public class RutaNPC : MonoBehaviour
    {
        public enum ModoRecorrido
        {
            // Del ultimo punto vuelve al primero en linea recta.
            Bucle,
            // Al llegar al final se da la vuelta y desanda el camino.
            IdaYVuelta
        }

        [Header("Recorrido")]
        public ModoRecorrido modo = ModoRecorrido.Bucle;

        [Tooltip("Paradas del recorrido. Si se deja vacio se usan los hijos de " +
                 "este objeto, en orden de jerarquia.")]
        public List<Transform> puntos = new List<Transform>();

        [Header("Gizmos")]
        public Color colorRuta = new Color(1f, 0.85f, 0.2f, 1f);
        [Tooltip("Tamano de las esferas que marcan cada parada.")]
        public float tamanoPunto = 0.25f;

        // Paradas resueltas: las de la lista, o los hijos si la lista esta vacia.
        private readonly List<Transform> resueltos = new List<Transform>();

        public int CantidadPuntos
        {
            get
            {
                ResolverPuntos();
                return resueltos.Count;
            }
        }

        // Posicion de la parada indicada.
        public Vector3 ObtenerPosicion(int indice)
        {
            ResolverPuntos();

            if (resueltos.Count == 0)
            {
                return transform.position;
            }

            return resueltos[Mathf.Clamp(indice, 0, resueltos.Count - 1)].position;
        }

        // Calcula cual es la siguiente parada y, en ida y vuelta, si hay que
        // cambiar de sentido.
        //
        // 'sentido' entra y sale por referencia: vale 1 yendo hacia adelante y -1
        // al volver. En modo Bucle no se toca.
        public int SiguienteIndice(int actual, ref int sentido)
        {
            ResolverPuntos();

            if (resueltos.Count <= 1)
            {
                return 0;
            }

            if (modo == ModoRecorrido.Bucle)
            {
                // El modulo cierra el circulo: del ultimo se vuelve al primero.
                return (actual + 1) % resueltos.Count;
            }

            int siguiente = actual + sentido;

            // Al pasarse de cualquiera de los dos extremos se invierte el sentido
            // y se rebota al punto contiguo, que es el que toca ahora.
            if (siguiente >= resueltos.Count || siguiente < 0)
            {
                sentido = -sentido;
                siguiente = actual + sentido;
            }

            return Mathf.Clamp(siguiente, 0, resueltos.Count - 1);
        }

        // Llena la lista de trabajo. Se rehace cada vez porque en el editor se
        // añaden y quitan hijos constantemente.
        private void ResolverPuntos()
        {
            resueltos.Clear();

            if (puntos != null && puntos.Count > 0)
            {
                foreach (Transform punto in puntos)
                {
                    if (punto != null)
                    {
                        resueltos.Add(punto);
                    }
                }

                if (resueltos.Count > 0)
                {
                    return;
                }
            }

            // Sin lista explicita mandan los hijos, en el orden de la jerarquia.
            foreach (Transform hijo in transform)
            {
                resueltos.Add(hijo);
            }
        }

        // Dibuja el recorrido en la vista de escena para poder colocar los puntos
        // a ojo. Se ve siempre, no solo con el objeto seleccionado.
        private void OnDrawGizmos()
        {
            ResolverPuntos();

            if (resueltos.Count == 0)
            {
                return;
            }

            Gizmos.color = colorRuta;

            for (int i = 0; i < resueltos.Count; i++)
            {
                Vector3 posicion = resueltos[i].position;
                Gizmos.DrawSphere(posicion, tamanoPunto);

                bool esUltimo = i == resueltos.Count - 1;

                if (!esUltimo)
                {
                    Gizmos.DrawLine(posicion, resueltos[i + 1].position);
                }
                else if (modo == ModoRecorrido.Bucle && resueltos.Count > 1)
                {
                    // El tramo de cierre se dibuja mas apagado para distinguirlo
                    // del recorrido de ida.
                    Gizmos.color = new Color(colorRuta.r, colorRuta.g, colorRuta.b, 0.35f);
                    Gizmos.DrawLine(posicion, resueltos[0].position);
                }
            }
        }
    }
}
