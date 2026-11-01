using UnityEngine;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    public class MenuVictoriaDerrota : MenuBase
    {
        protected override int OrdenPantalla => 160;

        private Image fondo;
        private Button botonSiguiente;
        private Button botonReintentar;
        private Button botonMenu;
        private bool fueVictoria;

        protected override void Construir(Transform raiz)
        {
            fondo = ConstructorUI.CrearImagenPantalla("FondoFinal", raiz, null, true);

            botonSiguiente = ConstructorUI.CrearBoton("BotonSiguiente", raiz, "",
                new Vector2(0f, -988f), new Vector2(650f, 92f), IrAlSiguienteNivel);
            botonReintentar = ConstructorUI.CrearBoton("BotonReintentar", raiz, "",
                new Vector2(0f, -988f), new Vector2(650f, 92f), ReintentarNivel);
            botonMenu = ConstructorUI.CrearBoton("BotonMenu", raiz, "",
                new Vector2(0f, -1000f), new Vector2(520f, 78f), VolverAlMenu);

            RecursosUI recursos = RecursosUI.Instancia;
            ConfigurarBoton(botonSiguiente,
                recursos != null ? recursos.botonSiguienteNivel : null, "Siguiente nivel");
            ConfigurarBoton(botonReintentar,
                recursos != null ? recursos.botonReintentarNivel : null, "Reintentar nivel");
            ConfigurarBoton(botonMenu,
                recursos != null ? recursos.botonContinuar : null, "Volver al menu");
            botonMenu.gameObject.SetActive(false);
        }

        public void Configurar(bool victoria, string detalle)
        {
            AsegurarConstruida();
            fueVictoria = victoria;

            RecursosUI recursos = RecursosUI.Instancia;
            fondo.sprite = recursos == null
                ? null
                : victoria ? recursos.fondoVictoria : recursos.fondoDerrota;
            fondo.color = fondo.sprite != null ? Color.white : ConstructorUI.ColorFondoPantalla;

            if (botonSiguiente != null)
            {
                botonSiguiente.gameObject.SetActive(victoria
                    && ProgresoNiveles.NivelActual < ProgresoNiveles.CantidadNiveles);
            }

            if (botonReintentar != null)
            {
                botonReintentar.gameObject.SetActive(!victoria);
            }

            if (botonMenu != null)
            {
                botonMenu.gameObject.SetActive(victoria
                    && ProgresoNiveles.NivelActual >= ProgresoNiveles.CantidadNiveles);
            }
        }

        private void IrAlSiguienteNivel()
        {
            if (!fueVictoria || ProgresoNiveles.NivelActual >= ProgresoNiveles.CantidadNiveles)
            {
                return;
            }

            ProgresoNiveles.NivelActual++;
            Debug.Log($"[Menus] Empezando nivel {ProgresoNiveles.NivelActual}.");
            Gestor?.ReiniciarYJugar();
        }

        private void ReintentarNivel()
        {
            if (fueVictoria)
            {
                return;
            }

            Debug.Log($"[Menus] Reintentando nivel {ProgresoNiveles.NivelActual}.");
            Gestor?.ReiniciarYJugar();
        }

        private void VolverAlMenu()
        {
            if (Gestor == null)
            {
                return;
            }

            Gestor.CerrarTodo();
            Gestor.Abrir(PantallaMenu.Principal);
        }

        private static void ConfigurarBoton(Button boton, Sprite sprite, string etiqueta)
        {
            Image imagen = boton.GetComponent<Image>();
            imagen.sprite = sprite;
            imagen.color = sprite != null ? Color.white : ConstructorUI.ColorBoton;
            imagen.preserveAspect = sprite != null;

            Text texto = boton.GetComponentInChildren<Text>();
            if (texto != null)
            {
                texto.text = sprite != null ? "" : etiqueta;
                texto.gameObject.SetActive(sprite == null);
            }
        }
    }
}
