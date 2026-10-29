using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoltaLaPala.Menus
{
    public class BotonMenu : Button
    {
        public const float DuracionPulsado = 0.12f;

        private float finPulsado;
        private Coroutine transicionPendiente;
        private Coroutine clickPendiente;

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            colors = ColoresBrillo();
        }
#endif

        public static ColorBlock ColoresBrillo()
        {
            ColorBlock colores = ColorBlock.defaultColorBlock;
            colores.normalColor = new Color(1f, 1f, 1f, 0f);
            colores.highlightedColor = new Color(1f, 1f, 1f, 0.2f);
            colores.pressedColor = new Color(1f, 1f, 1f, 0.4f);
            colores.selectedColor = new Color(1f, 1f, 1f, 0f);
            colores.disabledColor = new Color(0f, 0f, 0f, 0.45f);
            colores.colorMultiplier = 1f;
            colores.fadeDuration = 0.06f;
            return colores;
        }

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

        private IEnumerator TransicionDiferida()
        {
            yield return new WaitForSecondsRealtime(finPulsado - Time.unscaledTime);
            transicionPendiente = null;
            AplicarEstado(currentSelectionState);
        }

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

        protected override void OnDisable()
        {
            transicionPendiente = null;
            clickPendiente = null;
            finPulsado = 0f;
            base.OnDisable();
        }
    }
}
