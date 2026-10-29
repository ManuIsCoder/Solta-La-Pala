using System.Collections.Generic;
using UnityEngine;

namespace SoltaLaPala.NPC
{
    public class RutaNPC : MonoBehaviour
    {
        public enum ModoRecorrido
        {
            Bucle,
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

        private readonly List<Transform> resueltos = new List<Transform>();

        public int CantidadPuntos
        {
            get
            {
                ResolverPuntos();
                return resueltos.Count;
            }
        }

        public Vector3 ObtenerPosicion(int indice)
        {
            ResolverPuntos();

            if (resueltos.Count == 0)
            {
                return transform.position;
            }

            return resueltos[Mathf.Clamp(indice, 0, resueltos.Count - 1)].position;
        }

        public int SiguienteIndice(int actual, ref int sentido)
        {
            ResolverPuntos();

            if (resueltos.Count <= 1)
            {
                return 0;
            }

            if (modo == ModoRecorrido.Bucle)
            {
                return (actual + 1) % resueltos.Count;
            }

            int siguiente = actual + sentido;

            if (siguiente >= resueltos.Count || siguiente < 0)
            {
                sentido = -sentido;
                siguiente = actual + sentido;
            }

            return Mathf.Clamp(siguiente, 0, resueltos.Count - 1);
        }

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

            foreach (Transform hijo in transform)
            {
                resueltos.Add(hijo);
            }
        }

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
                    Gizmos.color = new Color(colorRuta.r, colorRuta.g, colorRuta.b, 0.35f);
                    Gizmos.DrawLine(posicion, resueltos[0].position);
                }
            }
        }
    }
}
