using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SoltaLaPala.Minijuegos
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class ArrastrableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Identificador de Tipo")]
        [Tooltip("Para saber qué objeto es (ej: 'Taza', 'Sandwich', 'Tenedor', 'Salero', 'BotellaAgua', 'Destapador', etc)")]
        public string idTipo;

        [Header("Comportamiento")]
        [Tooltip("Si es true, vuelve a su posición original si no se soltó sobre un destino válido")]
        public bool volverAlOrigenAlSoltar = true;

        public bool Bloqueado { get; set; } = false;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvas;
        private Vector2 posicionInicial;
        private Transform padreOriginal;

        public event Action<ArrastrableUI> AlComenzarArrastre;
        public event Action<ArrastrableUI> AlSoltar;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvas = GetComponentInParent<Canvas>();
        }

        private void OnEnable()
        {
            posicionInicial = rectTransform.anchoredPosition;
            padreOriginal = transform.parent;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Bloqueado) return;

            posicionInicial = rectTransform.anchoredPosition;
            padreOriginal = transform.parent;

            // Bloquear raycasts para que la zona de drop debajo pueda recibir el evento
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = 0.8f;
            }

            // Poner al frente mientras se arrastra
            transform.SetAsLastSibling();

            AlComenzarArrastre?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Bloqueado) return;

            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            rectTransform.anchoredPosition += eventData.delta / scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Bloqueado) return;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            AlSoltar?.Invoke(this);

            if (volverAlOrigenAlSoltar)
            {
                RestablecerPosicion();
            }
        }

        public void RestablecerPosicion()
        {
            rectTransform.anchoredPosition = posicionInicial;
            if (padreOriginal != null) transform.SetParent(padreOriginal);
        }

        public void FijarEnDestino(Transform nuevoPadre, Vector2 nuevaPosicionAnchored)
        {
            volverAlOrigenAlSoltar = false;
            transform.SetParent(nuevoPadre);
            rectTransform.anchoredPosition = nuevaPosicionAnchored;
        }
    }
}