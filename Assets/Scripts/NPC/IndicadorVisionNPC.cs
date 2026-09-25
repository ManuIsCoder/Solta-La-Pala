using UnityEngine;

namespace SoltaLaPala.NPC
{
    // Ojo que flota sobre la cabeza del NPC y avisa de si te esta viendo:
    //
    //   cerrado        -> no te ve
    //   abierto        -> te ve, pero de lejos
    //   abierto rojo   -> te ve y lo tenes encima
    //
    // Va en el mismo objeto que el DetectorVisionNPC, que es quien decide el
    // estado; este componente solo lo pinta.
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
        [Tooltip("Lado del icono en metros. Es el tamano real: no depende de " +
                 "cuantos pixeles tenga el sprite ni de su Pixels Per Unit.")]
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

        // Ultimo estado pintado, para no reasignar sprite y color cada frame.
        private DetectorVisionNPC.EstadoVision ultimoEstado = (DetectorVisionNPC.EstadoVision)(-1);

        private void Awake()
        {
            detector = GetComponent<DetectorVisionNPC>();
            CrearIcono();
        }

        // El icono se orienta en LateUpdate para que la camara ya se haya movido
        // este frame: si no, queda un frame por detras y tiembla al girar.
        private void LateUpdate()
        {
            if (render == null)
            {
                return;
            }

            Pintar(detector.Estado);
            Orientar();

#if UNITY_EDITOR
            // Para poder ajustar tamano y altura a ojo desde el inspector en Play.
            // Fuera del editor los valores no cambian, asi que no hace falta.
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

            // Un sprite sin asignar dejaria el icono en blanco: mejor no dibujar nada.
            bool visible = render.sprite != null
                && !(ocultarSiNoTeVe && estado == DetectorVisionNPC.EstadoVision.SinVer);

            render.enabled = visible;
        }

        // Billboard: el icono mira siempre a la camara, para que se lea igual
        // desde cualquier angulo.
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

            // Se alinea con el 'forward' de la camara en vez de mirar a su posicion:
            // asi todos los iconos quedan paralelos entre si y no se inclinan de
            // forma distinta segun donde este cada NPC.
            render.transform.rotation = Quaternion.LookRotation(camara.forward, camara.up);
        }

        // Crea el sprite como hijo, para poder moverlo y escalarlo sin tocar al NPC.
        private void CrearIcono()
        {
            GameObject objeto = new GameObject("IndicadorVision");
            objeto.transform.SetParent(transform, false);
            objeto.transform.localPosition = Vector3.up * altura;

            render = objeto.AddComponent<SpriteRenderer>();
            render.sprite = spriteSinVer;
            render.color = colorNormal;

            // Por encima de la geometria del mundo, para que no lo tape la cabeza
            // del propio NPC ni una pared cercana.
            render.sortingOrder = 100;

            AplicarTamano();
        }

        // Escala el icono para que mida 'tamano' metros de lado.
        //
        // Un SpriteRenderer con escala 1 mide pixeles/PixelsPerUnit metros: un PNG
        // de 32px a 100 PPU son 32 cm. Dividir por ese tamano hace que el campo
        // signifique metros de verdad y que cambiar de sprite no cambie el tamano.
        private void AplicarTamano()
        {
            float lado = TamanoBaseSprite();

            render.transform.localScale = lado > 0f
                ? Vector3.one * (tamano / lado)
                : Vector3.one * tamano;
        }

        // Lado mayor del sprite en metros, con escala 1.
        //
        // Se mide siempre el mismo sprite (el primero asignado) y no el que se
        // esta mostrando: si los tres PNG no fueran del mismo tamano, el icono
        // pegaria un salto de escala al cambiar de estado.
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

        // El sprite del estado inicial puede no estar asignado; sirve cualquiera
        // de los tres para saber a que escala trabajan.
        private Sprite PrimerSpriteAsignado()
        {
            if (spriteSinVer != null) return spriteSinVer;
            if (spriteVeLejos != null) return spriteVeLejos;
            return spriteVeCerca;
        }
    }
}
