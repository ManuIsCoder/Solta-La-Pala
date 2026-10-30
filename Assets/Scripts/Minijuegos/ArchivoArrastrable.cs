using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoltaLaPala.Minijuegos
{
    /// <summary>
    /// Componente para cada archivo virtual en el escritorio del minijuego.
    /// Permite hacer click y arrastrar el icono hacia la papelera de reciclaje.
    /// Incluye animaciones de elevación, retorno elástico si se cancela,
    /// y efecto de reducción al ser tragado por la papelera.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class ArchivoArrastrable : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public MinijuegoBorrarDatos.DatosArchivo Datos { get; private set; }
        public string NombreArchivo => Datos != null ? Datos.nombreArchivo : string.Empty;
        public bool EsImportante => Datos != null && Datos.esImportante;

        [Header("Referencias visuales internas")]
        public Image imagenIcono;
        public Text textoEtiqueta;
        public Image resaltadoHover;

        private MinijuegoBorrarDatos minijuego;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvasRaiz;

        private Transform padreOriginal;
        private Vector2 posicionInicial;
        private Coroutine corrutinaAnimacion;
        private bool estaArrastrando;
        private bool yaBorrado;

        public void Configurar(MinijuegoBorrarDatos.DatosArchivo datosArchivo, MinijuegoBorrarDatos gestor)
        {
            Datos = datosArchivo;
            minijuego = gestor;

            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvasRaiz = GetComponentInParent<Canvas>();

            padreOriginal = transform.parent;
            posicionInicial = rectTransform.anchoredPosition;

            if (imagenIcono != null && datosArchivo.icono != null)
            {
                imagenIcono.sprite = datosArchivo.icono;
            }

            if (textoEtiqueta != null)
            {
                textoEtiqueta.text = datosArchivo.nombreArchivo;
            }

            if (resaltadoHover != null)
            {
                resaltadoHover.gameObject.SetActive(false);
            }
        }

        public void GuardarPosicionActualComoOriginal()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            posicionInicial = rectTransform.anchoredPosition;
            padreOriginal = transform.parent;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (estaArrastrando || yaBorrado) return;
            if (resaltadoHover != null)
            {
                resaltadoHover.gameObject.SetActive(true);
            }
            transform.localScale = Vector3.one * 1.05f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (estaArrastrando || yaBorrado) return;
            if (resaltadoHover != null)
            {
                resaltadoHover.gameObject.SetActive(false);
            }
            transform.localScale = Vector3.one;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (yaBorrado) return;

            estaArrastrando = true;
            if (corrutinaAnimacion != null) StopCoroutine(corrutinaAnimacion);

            canvasGroup.alpha = 0.8f;
            canvasGroup.blocksRaycasts = false;

            if (resaltadoHover != null)
                resaltadoHover.gameObject.SetActive(false);

            if (canvasRaiz == null) canvasRaiz = GetComponentInParent<Canvas>();
            if (canvasRaiz != null)
            {
                transform.SetParent(canvasRaiz.transform, true);
                transform.SetAsLastSibling();
            }

            transform.localScale = Vector3.one * 1.15f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!estaArrastrando || yaBorrado) return;

            if (canvasRaiz != null)
            {
                rectTransform.anchoredPosition += eventData.delta / canvasRaiz.scaleFactor;
            }
            else
            {
                rectTransform.position = eventData.position;
            }

            // Notificar al minijuego si estamos sobrevolando la papelera
            if (minijuego != null)
            {
                bool sobrePapelera = minijuego.EstaSobrePapelera(eventData.position, eventData.pressEventCamera);
                minijuego.NotificarHoverPapelera(sobrePapelera);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!estaArrastrando || yaBorrado) return;
            estaArrastrando = false;

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            transform.localScale = Vector3.one;

            if (minijuego != null)
            {
                minijuego.NotificarHoverPapelera(false);

                if (minijuego.EstaSobrePapelera(eventData.position, eventData.pressEventCamera))
                {
                    yaBorrado = true;
                    corrutinaAnimacion = StartCoroutine(AnimarBorradoHaciaPapelera(minijuego.zonaPapelera.position));
                    return;
                }
            }

            // Si no se soltó en la papelera, regresa elásticamente a su posición original
            corrutinaAnimacion = StartCoroutine(AnimarRegreso());
        }

        private IEnumerator AnimarRegreso()
        {
            transform.SetParent(padreOriginal, true);

            Vector2 inicio = rectTransform.anchoredPosition;
            float duracion = 0.22f;
            float tiempo = 0f;

            while (tiempo < duracion)
            {
                tiempo += Time.deltaTime;
                float t = Mathf.Clamp01(tiempo / duracion);
                // Curva de rebote suave
                float curvado = Mathf.Sin(t * Mathf.PI * 0.5f);
                rectTransform.anchoredPosition = Vector2.Lerp(inicio, posicionInicial, curvado);
                yield return null;
            }

            rectTransform.anchoredPosition = posicionInicial;
            corrutinaAnimacion = null;
        }

        private IEnumerator AnimarBorradoHaciaPapelera(Vector3 destinoMundo)
        {
            canvasGroup.blocksRaycasts = false;
            Vector3 escalaInicial = transform.localScale;
            Vector3 posInicial = transform.position;
            float duracion = 0.25f;
            float tiempo = 0f;

            while (tiempo < duracion)
            {
                tiempo += Time.deltaTime;
                float t = Mathf.Clamp01(tiempo / duracion);
                transform.position = Vector3.Lerp(posInicial, destinoMundo, t);
                transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, t * t);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            if (minijuego != null)
            {
                minijuego.ProcesarArchivoEliminado(this);
            }

            Destroy(gameObject);
        }
    }
}
