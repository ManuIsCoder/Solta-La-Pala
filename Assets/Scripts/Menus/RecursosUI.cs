using UnityEngine;

namespace SoltaLaPala.Menus
{
    [CreateAssetMenu(fileName = "RecursosUI", menuName = "SoltaLaPala/Recursos UI")]
    public class RecursosUI : ScriptableObject
    {
        private static RecursosUI instancia;

        public Sprite fondoNiveles;
        public Sprite[] panelesNivel;
        public Sprite bloqueoNivel;
        [Header("Selector de niveles")]
        [Tooltip("Posicion del centro del boton del primer nivel, en el canvas de referencia.")]
        public Vector2 posicionPrimerBotonNivel = new Vector2(0f, -176f);
        [Tooltip("Distancia vertical entre los centros de los botones de nivel.")]
        public float separacionBotonesNivel = 137f;
        [Tooltip("Ancho y alto de la zona clicable de cada boton de nivel.")]
        public Vector2 tamanoBotonNivel = new Vector2(700f, 118f);
        public Sprite fondoResultados;
        public Sprite fondoVictoria;
        public Sprite fondoDerrota;
        public Sprite botonContinuar;
        public Sprite botonSiguienteNivel;
        public Sprite botonReintentarNivel;

        public static RecursosUI Instancia
        {
            get
            {
                if (instancia == null)
                {
                    instancia = Resources.Load<RecursosUI>("Sprites/UI/RecursosUI");
                    if (instancia == null)
                    {
                        Debug.LogError("[RecursosUI] No se encontró Resources/Sprites/UI/RecursosUI.asset.");
                    }
                }

                return instancia;
            }
        }
    }
}
