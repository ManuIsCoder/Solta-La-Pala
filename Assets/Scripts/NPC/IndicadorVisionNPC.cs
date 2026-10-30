using UnityEngine;

namespace SoltaLaPala.NPC
{
    [RequireComponent(typeof(DetectorVisionNPC))]
    public class IndicadorVisionNPC : MonoBehaviour
    {
        [Header("Sprites")]
        [Tooltip("Ojo cerrado: el NPC no te ve.")]
        public Sprite spriteSinVer;
        [Tooltip("Ojo abierto: te ve de lejos.")]
        public Sprite spriteVeLejos;
        [Tooltip("Ojo abierto de cerca. Se pinta en rojo.")]
        public Sprite spriteVeCerca;

        [Header("Colocacion")]
        [Tooltip("Altura sobre el origen del NPC.")]
        public float altura = 2.2f;
        [Tooltip("Lado del icono en metros.")]
        public float tamano = 0.8f;

        [Header("Colores")]
        public Color colorNormal = Color.white;
        public Color colorAlerta = new Color(0.9f, 0.15f, 0.15f, 1f);

        [Header("Visibilidad")]
        [Tooltip("Si esta marcado, el ojo cerrado no se dibuja y el NPC solo " +
                 "muestra icono cuando te ve.")]
        public bool ocultarSiNoTeVe = false;

        private DetectorVisionNPC detector;
        private SpriteRenderer render;
        private Transform camara;

        private DetectorVisionNPC.EstadoVision ultimoEstado = (DetectorVisionNPC.EstadoVision)(-1);

        private void Awake()
        {
            detector = GetComponent<DetectorVisionNPC>();
        }

        private void LateUpdate()
        {
            if (render == null)
            {
                return;
            }

            Pintar(detector.Estado);
            Orientar();

#if UNITY_EDITOR
            render.transform.localPosition = Vector3.up * altura;
            AplicarTamano();
#endif
        }

        private void Pintar(DetectorVisionNPC.EstadoVision estado)
        {
            if (estado == ultimoEstado)
            {
                return;
            }

            ultimoEstado = estado;

            switch (estado)
            {
                case DetectorVisionNPC.EstadoVision.VeCerca:
                    render.sprite = spriteVeCerca;
                    render.color = colorAlerta;
                    break;

                case DetectorVisionNPC.EstadoVision.VeLejos:
                    render.sprite = spriteVeLejos;
                    render.color = colorNormal;
                    break;

                default:
                    render.sprite = spriteSinVer;
                    render.color = colorNormal;
                    break;
            }

            bool visible = render.sprite != null
                && !(ocultarSiNoTeVe && estado == DetectorVisionNPC.EstadoVision.SinVer);

            render.enabled = visible;
        }

        private void Orientar()
        {
            if (camara == null)
            {
                if (Camera.main == null)
                {
                    return;
                }

                camara = Camera.main.transform;
            }

            render.transform.rotation = Quaternion.LookRotation(camara.forward, camara.up);
        }

        private void CrearIcono()
        {
            GameObject objeto = new GameObject("IndicadorVision");
            objeto.transform.SetParent(transform, false);
            objeto.transform.localPosition = Vector3.up * altura;

            render = objeto.AddComponent<SpriteRenderer>();
            render.sprite = spriteSinVer;
            render.color = colorNormal;

            render.sortingOrder = 100;

            AplicarTamano();
        }

        private void AplicarTamano()
        {
            float lado = TamanoBaseSprite();

            render.transform.localScale = lado > 0f
                ? Vector3.one * (tamano / lado)
                : Vector3.one * tamano;
        }

        private float TamanoBaseSprite()
        {
            Sprite referencia = PrimerSpriteAsignado();

            if (referencia == null)
            {
                return 0f;
            }

            Vector2 medidas = referencia.bounds.size;
            return Mathf.Max(medidas.x, medidas.y);
        }

        private Sprite PrimerSpriteAsignado()
        {
            if (spriteSinVer != null) return spriteSinVer;
            if (spriteVeLejos != null) return spriteVeLejos;
            return spriteVeCerca;
        }
    }
}
