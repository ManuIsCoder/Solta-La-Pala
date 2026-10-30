using UnityEngine;
using UnityEngine.UI;
using SoltaLaPala.NPC;

namespace SoltaLaPala.Menus{



    public class IndicadorVisionHUD : MonoBehaviour
    {

        public static IndicadorVisionHUD Instancia { get; private set; }
        public DetectorVisionNPC.EstadoVision EstadoActual { get; private set; } = DetectorVisionNPC.EstadoVision.SinVer;
        [Header("Sprites")]
        [Tooltip("Ojo cerrado: ningún NPC te ve.")]
        public Sprite spriteSinVer;
        [Tooltip("Ojo abierto: te ven de lejos.")]
        public Sprite spriteVeLejos;
        [Tooltip("Ojo abierto alerta: te ven de cerca.")]
        public Sprite spriteVeCerca;
        [Header("Posición y Tamaño")]
        public Vector2 tamano = new Vector2(72f, 72f);
        [Tooltip("Margen desde el borde inferior de la pantalla.")]
        public float margenInferior = 28f;
        [Header("Colores")]
        public Color colorNormal = Color.white;
        public Color colorAlerta = new Color(0.9f, 0.15f, 0.15f, 1f);
        public Color colorApagado = new Color(1f, 1f, 1f, 0.4f);
        [Header("Visibilidad")]
        [Tooltip("Si es true, se oculta completamente cuando nadie te ve en vez de mostrar el ojo cerrado.")]
        public bool ocultarSiNoTeVe = false;
        private const int OrdenCanvas = 50;
        private Image imagenOjo;
        private CanvasGroup grupo;
        private DetectorVisionNPC[] detectoresCache;
        private float proximaActualizacionDetectores = 0f;
        private DetectorVisionNPC.EstadoVision ultimoEstado = (DetectorVisionNPC.EstadoVision)(-1);

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            Instancia = this;

            ObtenerSpritesFallback();
            ConstruirUI();
        }

        // Si no se asignaron en el inspector, toma los sprites configurados en los NPCs de la escena
        private void ObtenerSpritesFallback()
        {
            if (spriteSinVer != null && spriteVeLejos != null && spriteVeCerca != null) return;

            IndicadorVisionNPC indicador = FindFirstObjectByType<IndicadorVisionNPC>();
            if (indicador != null)
            {
                if (spriteSinVer == null) spriteSinVer = indicador.spriteSinVer;
                if (spriteVeCerca == null) spriteVeCerca = indicador.spriteVeCerca;
                if (spriteVeLejos == null) spriteVeLejos = indicador.spriteVeLejos;
            } 
        }

        private void Update()
        {
             ActualizarVisibilidadGeneral();
            if (grupo != null && grupo.alpha <= 0.01f)
            {
                return;
            }
            EstadoActual = ObtenerEstadoMaximo();
            Pintar(EstadoActual);
        }

        private DetectorVisionNPC.EstadoVision ObtenerEstadoMaximo()
        {
            if (Time.time >= proximaActualizacionDetectores || detectoresCache == null)
            {
                detectoresCache = FindObjectsByType<DetectorVisionNPC>(FindObjectsSortMode.None);
                proximaActualizacionDetectores = Time.time + 1f;
            }
            DetectorVisionNPC.EstadoVision maximo = DetectorVisionNPC.EstadoVision.SinVer;
            for (int i = 0; i < detectoresCache.Length; i++)
            {
                DetectorVisionNPC det = detectoresCache[i];
                if (det == null || !det.gameObject.activeInHierarchy || !det.enabled) continue;
                if (det.Estado > maximo)
                {
                    maximo = det.Estado;
                    if (maximo == DetectorVisionNPC.EstadoVision.VeCerca) break;
                }
            }
            return maximo;
        }
        private void Pintar(DetectorVisionNPC.EstadoVision estado)
        {
            if (imagenOjo == null) return;

            if (spriteSinVer == null || spriteVeLejos == null || spriteVeCerca == null)
            {
                ObtenerSpritesFallback();
            }

            if (estado == ultimoEstado) return;
            ultimoEstado = estado;
            switch (estado)
            {
                case DetectorVisionNPC.EstadoVision.VeCerca:
                    imagenOjo.sprite = spriteVeCerca != null ? spriteVeCerca : spriteVeLejos;
                    imagenOjo.color = colorAlerta;
                    imagenOjo.enabled = true;
                    break;
                case DetectorVisionNPC.EstadoVision.VeLejos:
                    imagenOjo.sprite = spriteVeLejos;
                    imagenOjo.color = colorNormal;
                    imagenOjo.enabled = true;
                    break;
                default: // SinVer
                    if (ocultarSiNoTeVe)
                    {
                        imagenOjo.enabled = false;
                    }
                    else
                    {
                        imagenOjo.sprite = spriteSinVer;
                        imagenOjo.color = colorApagado;
                        imagenOjo.enabled = (spriteSinVer != null);
                    }
                    break;
            }
        }
        private void ActualizarVisibilidadGeneral()
        {
            if (grupo == null) return;
            bool jugando = EstadoPartida.Instancia != null
                && EstadoPartida.Instancia.PartidaEnCurso
                && (GestorMenus.Instancia == null || !GestorMenus.Instancia.HayMenuAbierto);
            float alfa = jugando ? 1f : 0f;
            if (!Mathf.Approximately(grupo.alpha, alfa))
            {
                grupo.alpha = alfa;
            }
        }
        private void ConstruirUI()
        {
            Canvas canvas = ConstructorUI.CrearCanvas("CanvasIndicadorVisionHUD", transform, OrdenCanvas);
            Destroy(canvas.GetComponent<GraphicRaycaster>());
            grupo = canvas.gameObject.AddComponent<CanvasGroup>();
            grupo.interactable = false;
            grupo.blocksRaycasts = false;
            GameObject contenedor = new GameObject("IconoOjo");
            contenedor.transform.SetParent(canvas.transform, false);
            RectTransform rect = contenedor.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, margenInferior);
            rect.sizeDelta = tamano;
            imagenOjo = contenedor.AddComponent<Image>();
            imagenOjo.preserveAspect = true;
            imagenOjo.color = colorApagado;
            if (spriteSinVer != null)
            {
                imagenOjo.sprite = spriteSinVer;
            }
            else
            {
                imagenOjo.enabled = false;
            }
        }
    }
}