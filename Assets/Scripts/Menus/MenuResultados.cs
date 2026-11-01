using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    public class MenuResultados : MenuBase
    {
        protected override int OrdenPantalla => 150;

        private readonly List<Text[]> filas = new List<Text[]>();
        private Text textoImpactoTotal;
        private Text textoSospechaTotal;
        private Text textoDetalle;
        private bool fueVictoria;
        private string detalleActual;

        public bool FueVictoria => fueVictoria;

        protected override void Construir(Transform raiz)
        {
            RecursosUI recursos = RecursosUI.Instancia;
            Image fondo = ConstructorUI.CrearImagenPantalla(
                "FondoResultados", raiz, recursos != null ? recursos.fondoResultados : null);
            if (fondo.sprite == null)
            {
                fondo.color = ConstructorUI.ColorPanel;
            }

            textoImpactoTotal = CrearTextoColumna("ImpactoTotal", raiz,
                new Vector2(-445f, -300f), new Vector2(360f, 64f), 38);
            textoSospechaTotal = CrearTextoColumna("SospechaTotal", raiz,
                new Vector2(445f, -300f), new Vector2(360f, 64f), 38);
            textoDetalle = CrearTextoColumna("DetalleFinal", raiz,
                new Vector2(0f, -160f), new Vector2(1050f, 38f), 18);

            for (int i = 0; i < 10; i++)
            {
                filas.Add(new[]
                {
                    CrearTextoColumna($"Tarea{i}", raiz, Vector2.zero, new Vector2(700f, 72f), 26),
                    CrearTextoColumna($"Testigo{i}", raiz, Vector2.zero, new Vector2(300f, 72f), 26),
                    CrearTextoColumna($"Impacto{i}", raiz, Vector2.zero, new Vector2(300f, 72f), 26),
                    CrearTextoColumna($"Sospecha{i}", raiz, Vector2.zero, new Vector2(300f, 72f), 26)
                });
            }

            Button botonContinuar = ConstructorUI.CrearBoton("BotonContinuar", raiz, "",
                new Vector2(0f, -1010f), new Vector2(520f, 68f),
                () => Gestor?.ContinuarDesdeResultados());
            ConfigurarBotonConSprite(botonContinuar,
                recursos != null ? recursos.botonContinuar : null);
        }

        public void ConfigurarFinal(bool victoria, string detalle)
        {
            AsegurarConstruida();
            fueVictoria = victoria;
            detalleActual = detalle;
            Refrescar();
        }

        protected override void AlMostrar()
        {
            Refrescar();
        }

        private void Refrescar()
        {
            EstadoPartida estado = EstadoPartida.Instancia;
            if (textoImpactoTotal == null || textoSospechaTotal == null)
            {
                return;
            }

            textoImpactoTotal.text = estado != null ? estado.Impacto.ToString() : "-";
            textoSospechaTotal.text = "-";

            List<RegistroTareaSaboteada> tareas = estado != null
                ? new List<RegistroTareaSaboteada>(estado.TareasSaboteadas)
                : new List<RegistroTareaSaboteada>();

            if (tareas.Count == 0)
            {
                tareas.Add(new RegistroTareaSaboteada
                {
                    tarea = "Ninguna tarea malograda",
                    impacto = 0,
                    testigo = estado != null && estado.HuboTestigo
                });
            }

            if (textoDetalle != null)
            {
                textoDetalle.text = detalleActual ?? "";
            }

            int filasVisibles = Mathf.Min(tareas.Count, filas.Count);
            float altoTabla = filasVisibles > 5 ? 420f : 480f;
            float altoFila = Mathf.Min(96f, altoTabla / filasVisibles);
            float primerCentroY = filasVisibles > 5 ? -570f : -565f;
            int impactoRestante = 0;
            bool huboTestigoEnRestantes = false;

            for (int i = filas.Count - 1; i < tareas.Count; i++)
            {
                impactoRestante += tareas[i].impacto;
                huboTestigoEnRestantes |= tareas[i].testigo;
            }

            for (int i = 0; i < filas.Count; i++)
            {
                Text[] fila = filas[i];
                bool visible = i < filasVisibles;
                foreach (Text texto in fila)
                {
                    texto.gameObject.SetActive(visible);
                }

                if (!visible)
                {
                    continue;
                }

                bool filaAdicional = i == filas.Count - 1 && tareas.Count > filas.Count;
                RegistroTareaSaboteada registro = tareas[i];
                float y = primerCentroY - i * altoFila;
                int tamanoFuente = Mathf.Clamp(Mathf.RoundToInt(altoFila * 0.28f), 16, 26);

                fila[0].rectTransform.anchoredPosition = new Vector2(-480f, y);
                fila[1].rectTransform.anchoredPosition = new Vector2(100f, y);
                fila[2].rectTransform.anchoredPosition = new Vector2(420f, y);
                fila[3].rectTransform.anchoredPosition = new Vector2(745f, y);

                foreach (Text texto in fila)
                {
                    texto.rectTransform.sizeDelta = new Vector2(
                        texto.rectTransform.sizeDelta.x, Mathf.Min(72f, altoFila));
                }

                if (filaAdicional)
                {
                    fila[0].text = $"Y {tareas.Count - filas.Count + 1} tareas mas";
                    fila[1].text = huboTestigoEnRestantes ? "Si" : "No";
                    fila[2].text = impactoRestante.ToString();
                }
                else
                {
                    fila[0].text = registro.tarea;
                    fila[1].text = registro.testigo ? "Si" : "No";
                    fila[2].text = registro.impacto.ToString();
                }

                fila[3].text = "-";
                foreach (Text texto in fila)
                {
                    texto.fontSize = tamanoFuente;
                }
            }
        }

        private static Text CrearTextoColumna(string nombre, Transform padre, Vector2 posicion,
            Vector2 tamano, int tamanoFuente)
        {
            Text texto = ConstructorUI.CrearTexto(nombre, padre, "", tamanoFuente,
                TextAnchor.MiddleCenter, posicion, tamano);
            texto.color = Color.black;
            texto.fontStyle = FontStyle.Bold;
            texto.raycastTarget = false;
            return texto;
        }

        private static void ConfigurarBotonConSprite(Button boton, Sprite sprite)
        {
            Image imagen = boton.GetComponent<Image>();
            imagen.sprite = sprite;
            imagen.color = sprite != null ? Color.white : ConstructorUI.ColorBoton;
            imagen.preserveAspect = sprite != null;

            Text etiqueta = boton.GetComponentInChildren<Text>();
            if (etiqueta != null)
            {
                etiqueta.text = sprite != null ? "" : "Continuar";
                etiqueta.gameObject.SetActive(sprite == null);
            }
        }
    }
}
