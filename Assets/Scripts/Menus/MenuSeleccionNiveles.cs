using SoltaLaPala.Guardado;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    public class MenuSeleccionNiveles : MenuBase
    {
        protected override int OrdenPantalla => 110;

        private readonly Button[] botonesNivel = new Button[ProgresoNiveles.CantidadNiveles];
        private readonly GameObject[] bloqueosNivel = new GameObject[6];
        private GameObject panelLista;
        private Text textoAviso;

        protected override void Construir(Transform raiz)
        {
            RecursosUI recursos = RecursosUI.Instancia;
            panelLista = new GameObject("ListaNiveles", typeof(RectTransform));
            panelLista.transform.SetParent(raiz, false);
            Estirar((RectTransform)panelLista.transform);
            ConstructorUI.CrearImagenPantalla("FondoNiveles", panelLista.transform, recursos?.fondoNiveles);

            for (int i = 0; i < bloqueosNivel.Length; i++)
            {
                int nivel = i + 1;
                Vector2 posicion = new Vector2(
                    recursos != null ? recursos.posicionPrimerBotonNivel.x : 0f,
                    (recursos != null ? recursos.posicionPrimerBotonNivel.y : -176f)
                    - i * (recursos != null ? recursos.separacionBotonesNivel : 137f));

                if (nivel <= ProgresoNiveles.CantidadNiveles)
                {
                    Button boton = ConstructorUI.CrearBoton($"Nivel{nivel}", panelLista.transform, "",
                        posicion,
                        recursos != null ? recursos.tamanoBotonNivel : new Vector2(700f, 118f),
                        () => SeleccionarNivel(nivel));
                    CrearBordeBlanco(
                        panelLista.transform,
                        $"BordeNivel{nivel}",
                        posicion,
                        recursos != null ? recursos.tamanoBotonNivel : new Vector2(700f, 118f));

                    Image imagenBoton = boton.GetComponent<Image>();
                    imagenBoton.color = Color.clear;
                    imagenBoton.raycastTarget = true;

                    Text etiqueta = boton.GetComponentInChildren<Text>();
                    if (etiqueta != null)
                    {
                        etiqueta.gameObject.SetActive(false);
                    }

                    botonesNivel[i] = boton;
                }
            }

            for (int i = 0; i < bloqueosNivel.Length; i++)
            {
                int nivel = i + 1;
                Vector2 posicion = new Vector2(
                    recursos != null ? recursos.posicionPrimerBotonNivel.x : 0f,
                    (recursos != null ? recursos.posicionPrimerBotonNivel.y : -176f)
                    - i * (recursos != null ? recursos.separacionBotonesNivel : 137f));
                GameObject bloqueo = new GameObject($"BloqueoNivel{nivel}");
                bloqueo.transform.SetParent(panelLista.transform, false);
                RectTransform rectBloqueo = bloqueo.AddComponent<RectTransform>();
                rectBloqueo.anchorMin = new Vector2(0.5f, 1f);
                rectBloqueo.anchorMax = new Vector2(0.5f, 1f);
                rectBloqueo.pivot = new Vector2(0.5f, 1f);
                rectBloqueo.anchoredPosition = posicion;
                rectBloqueo.sizeDelta = new Vector2(1180f, 138f);

                Image imagenBloqueo = bloqueo.AddComponent<Image>();
                imagenBloqueo.sprite = recursos != null ? recursos.bloqueoNivel : null;
                imagenBloqueo.preserveAspect = false;
                imagenBloqueo.color = imagenBloqueo.sprite != null
                    ? Color.white
                    : new Color(0f, 0f, 0f, 0.75f);
                imagenBloqueo.raycastTarget = true;
                bloqueosNivel[i] = bloqueo;
            }

            textoAviso = ConstructorUI.CrearTexto("Aviso", panelLista.transform,
                "Completa un nivel para desbloquear el siguiente.",
                24, TextAnchor.MiddleCenter, new Vector2(0f, -1025f), new Vector2(780f, 34f));
            textoAviso.color = Color.white;

            ConstructorUI.CrearBoton("BotonVolver", panelLista.transform, "Volver",
                new Vector2(-780f, -1015f), new Vector2(220f, 56f), () => Gestor?.Volver());
        }

        private static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CrearBordeBlanco(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
        {
            const float grosor = 4f;
            GameObject borde = new GameObject(nombre, typeof(RectTransform));
            borde.transform.SetParent(padre, false);

            RectTransform rectBorde = (RectTransform)borde.transform;
            rectBorde.anchorMin = new Vector2(0.5f, 1f);
            rectBorde.anchorMax = new Vector2(0.5f, 1f);
            rectBorde.pivot = new Vector2(0.5f, 1f);
            rectBorde.anchoredPosition = posicion;
            rectBorde.sizeDelta = tamano;

            CrearSegmentoBorde(borde.transform, "Arriba",
                new Vector2(0f, 0f), new Vector2(tamano.x, grosor));
            CrearSegmentoBorde(borde.transform, "Abajo",
                new Vector2(0f, -tamano.y + grosor), new Vector2(tamano.x, grosor));
            CrearSegmentoBorde(borde.transform, "Izquierda",
                new Vector2(-tamano.x * 0.5f + grosor * 0.5f, -grosor),
                new Vector2(grosor, Mathf.Max(0f, tamano.y - grosor * 2f)));
            CrearSegmentoBorde(borde.transform, "Derecha",
                new Vector2(tamano.x * 0.5f - grosor * 0.5f, -grosor),
                new Vector2(grosor, Mathf.Max(0f, tamano.y - grosor * 2f)));
        }

        private static void CrearSegmentoBorde(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
        {
            GameObject segmento = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            segmento.transform.SetParent(padre, false);

            RectTransform rect = (RectTransform)segmento.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;

            Image imagen = segmento.GetComponent<Image>();
            imagen.color = Color.white;
            imagen.raycastTarget = false;
        }

        protected override void AlMostrar()
        {
            RefrescarBloqueos();
            RefrescarAviso();
        }

        private void RefrescarAviso()
        {
            if (textoAviso != null)
            {
                textoAviso.text = GuardadoPartida.Existe()
                    ? "Elegir un nivel borrara la partida guardada."
                    : "Completa un nivel para desbloquear el siguiente.";
            }
        }

        private void RefrescarBloqueos()
        {
            for (int i = 0; i < bloqueosNivel.Length; i++)
            {
                int nivel = i + 1;
                bool desbloqueado = nivel <= ProgresoNiveles.CantidadNiveles
                    && ProgresoNiveles.EstaDesbloqueado(nivel);

                if (i < botonesNivel.Length && botonesNivel[i] != null)
                {
                    botonesNivel[i].interactable = desbloqueado;
                }

                if (bloqueosNivel[i] != null)
                {
                    bloqueosNivel[i].SetActive(!desbloqueado);
                }
            }
        }

        private void SeleccionarNivel(int nivel)
        {
            if (!ProgresoNiveles.EstaDesbloqueado(nivel))
            {
                return;
            }

            ProgresoNiveles.NivelActual = nivel;
            Debug.Log($"[Menus] Empezando nivel {nivel}.");
            Gestor?.ReiniciarYJugar();
        }
    }
}
