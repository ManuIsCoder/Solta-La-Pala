using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    // Boton de los menus: se aclara con el mouse encima y mas todavia al pulsarlo.
    //
    // El aclarado lo pinta un hijo "Brillo" (Image blanca) que es el targetGraphic
    // del boton: los colores del ColorBlock solo cambian su alpha. El Mask del boton
    // recorta el brillo a la forma del sprite, asi no se ve un rectangulo blanco
    // en las esquinas transparentes.
    //
    // El tinte normal de Unity multiplica el color del sprite y no puede pasar de
    // blanco, por eso no sirve para aclarar.
    public class BotonMenu : Button
    {
        // Cuanto dura el destello de pulsado. La accion del boton espera a que
        // termine: si no, el menu se cierra antes de que se llegue a ver.
        public const float DuracionPulsado = 0.12f;

        private float finPulsado;
        private Coroutine transicionPendiente;
        private Coroutine clickPendiente;

#if UNITY_EDITOR
        // Al agregar el componente en el editor ya queda con los colores del Brillo.
        // Falta crear el hijo "Brillo" (Image blanca estirada, sin raycast, ultimo
        // hijo), asignarlo como Target Graphic y poner un Mask en el boton.
        protected override void Reset()
        {
            base.Reset();
            colors = ColoresBrillo();
        }
#endif

        // Colores del Brillo: transparente en reposo, cada vez mas blanco al
        // pasar por encima y al pulsar. Apagado oscurece en vez de aclarar.
        private static ColorBlock ColoresBrillo()
        {
            ColorBlock colores = ColorBlock.defaultColorBlock;
            colores.normalColor = new Color(1f, 1f, 1f, 0f);
            colores.highlightedColor = new Color(1f, 1f, 1f, 0.2f);
            colores.pressedColor = new Color(1f, 1f, 1f, 0.4f);
            // Sin esto el boton se queda aclarado despues del click aunque el mouse se vaya.
            colores.selectedColor = new Color(1f, 1f, 1f, 0f);
            colores.disabledColor = new Color(0f, 0f, 0f, 0.45f);
            colores.colorMultiplier = 1f;
            colores.fadeDuration = 0.06f;
            return colores;
        }

        // Retiene el estado de pulsado hasta que pase DuracionPulsado, aunque el
        // click haya sido instantaneo.
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (transicionPendiente != null)
            {
                StopCoroutine(transicionPendiente);
                transicionPendiente = null;
            }

            if (state == SelectionState.Pressed)
            {
                finPulsado = Time.unscaledTime + DuracionPulsado;
            }
            else if (!instant && isActiveAndEnabled && Time.unscaledTime < finPulsado)
            {
                transicionPendiente = StartCoroutine(TransicionDiferida());
                return;
            }

            base.DoStateTransition(state, instant);
        }

        // Tiempo real: los menus abren con Time.timeScale en 0.
        private IEnumerator TransicionDiferida()
        {
            yield return new WaitForSecondsRealtime(finPulsado - Time.unscaledTime);
            transicionPendiente = null;
            AplicarEstado(currentSelectionState);
        }

        // base no se puede usar dentro de un iterador.
        private void AplicarEstado(SelectionState state)
        {
            base.DoStateTransition(state, false);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left
                || !IsActive() || !IsInteractable() || clickPendiente != null)
            {
                return;
            }

            clickPendiente = StartCoroutine(ClickDiferido());
        }

        private IEnumerator ClickDiferido()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, finPulsado - Time.unscaledTime));
            clickPendiente = null;
            onClick.Invoke();
        }

        // Las corrutinas mueren solas al apagarse el objeto; aca solo se limpian
        // las referencias para que el proximo click no quede bloqueado.
        protected override void OnDisable()
        {
            transicionPendiente = null;
            clickPendiente = null;
            finPulsado = 0f;
            base.OnDisable();
        }
    }
}
