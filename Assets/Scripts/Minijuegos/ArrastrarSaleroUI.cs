using UnityEngine;
using UnityEngine.EventSystems;

namespace SoltaLaPala.Minijuegos
{
    public class ArrastrarSaleroUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform rectTransform;
        private Canvas canvas;
        private MinijuegoCafetera minijuego;
        private Vector2 posicionInicial;
        private bool arrastrando;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
        }

        public void Configurar(MinijuegoCafetera cafetera)
        {
            minijuego = cafetera;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (minijuego == null) return;

            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            posicionInicial = rectTransform.anchoredPosition;
            arrastrando = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!arrastrando) return;

            float escala = canvas != null ? Mathf.Max(canvas.scaleFactor, 0.01f) : 1f;
            rectTransform.anchoredPosition += eventData.delta / escala;
            if (minijuego.IntentarAgregarSal(eventData.position, eventData.pressEventCamera))
            {
                arrastrando = false;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = posicionInicial;
            }
            arrastrando = false;
        }
    }
}
