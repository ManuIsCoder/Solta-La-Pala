using SoltaLaPala.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoltaLaPala.Personajes
{
    public class ModeloPersonaje : MonoBehaviour
    {
        public const string HuesoManoDerecha = "Bip001 R Hand001";
        public const string HuesoManoIzquierda = "Bip001 L Hand001";
        public const string HuesoCabeza = "Bip001 Head001";

        [Tooltip("Prefab de Resources/Personajes (Jugador, NPCHombre1, NPCMujer1, ...).")]
        public GameObject prefabModelo;

        [Tooltip("Altura del modelo en metros. 0 = la del collider del personaje.")]
        public float altura = 0f;

        [Tooltip("Usa la animacion de sentado en vez del reposo.")]
        public bool sentado;

        [Tooltip("Oculta el MeshRenderer propio (la capsula de prueba).")]
        public bool ocultarCapsula = true;

        [Tooltip("En primera persona el cuerpo solo proyecta sombra, para no tapar la camara.")]
        public bool ocultarEnPrimeraPersona = true;

        public GameObject Instancia { get; private set; }
        public AnimadorPersonaje Animador { get; private set; }

        private Renderer[] renderersModelo;
        private CamaraTerceraPersona camara;
        private bool enPrimeraPersona;

        private void Awake()
        {
            if (prefabModelo != null)
            {
                Asignar(prefabModelo);
            }
        }

        public void Asignar(GameObject prefab)
        {
            if (Instancia != null)
            {
                Destroy(Instancia);
            }

            prefabModelo = prefab;
            if (prefab == null)
            {
                return;
            }

            Instancia = Instantiate(prefab, transform);
            Instancia.name = prefab.name;
            Instancia.transform.localPosition = Vector3.zero;

            renderersModelo = Instancia.GetComponentsInChildren<Renderer>(true);
            foreach (SkinnedMeshRenderer piel in Instancia.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                piel.updateWhenOffscreen = true;
            }
            Animador = Instancia.GetComponentInChildren<AnimadorPersonaje>();
            if (Animador != null)
            {
                Animador.estaSentado = sentado;
            }

            Acomodar();

            if (ocultarCapsula)
            {
                MeshRenderer capsula = GetComponent<MeshRenderer>();
                if (capsula != null)
                {
                    capsula.enabled = false;
                }
            }
        }

        public Transform BuscarHueso(string nombre)
        {
            return Instancia != null ? Buscar(Instancia.transform, nombre) : null;
        }

        private static Transform Buscar(Transform padre, string nombre)
        {
            if (padre.name == nombre)
            {
                return padre;
            }

            foreach (Transform hijo in padre)
            {
                Transform encontrado = Buscar(hijo, nombre);
                if (encontrado != null)
                {
                    return encontrado;
                }
            }

            return null;
        }

        private void Acomodar()
        {
            float pies = 0f;
            float alturaCollider = 0f;

            CharacterController controlador = GetComponent<CharacterController>();
            CapsuleCollider capsula = GetComponent<CapsuleCollider>();

            if (controlador != null)
            {
                pies = controlador.center.y - controlador.height * 0.5f;
                alturaCollider = controlador.height;
            }
            else if (capsula != null)
            {
                pies = capsula.center.y - capsula.height * 0.5f;
                alturaCollider = capsula.height;
            }

            float alturaDeseada = altura > 0f ? altura : alturaCollider;

            AnimationClip reposo = Animador != null ? Animador.Clip(AnimadorPersonaje.ClipReposo) : null;
            if (reposo != null)
            {
                reposo.SampleAnimation(Instancia, 0f);
            }

            Orientar();

            if (!LimitesDeHuesos(out float abajo, out float arriba))
            {
                Instancia.transform.localPosition = new Vector3(0f, pies, 0f);
                return;
            }

            float alturaModelo = (arriba - abajo) / transform.lossyScale.y;
            if (alturaDeseada > 0f && alturaModelo > 0.01f)
            {
                Instancia.transform.localScale *= alturaDeseada / alturaModelo;
                LimitesDeHuesos(out abajo, out arriba);
            }

            float piesModelo = transform.InverseTransformPoint(new Vector3(transform.position.x, abajo, transform.position.z)).y;
            Instancia.transform.localPosition += new Vector3(0f, pies - piesModelo, 0f);

            Transform pelvis = BuscarHuesoQueContenga("Pelvis");
            if (pelvis != null)
            {
                Vector3 desvio = transform.InverseTransformPoint(pelvis.position);
                Instancia.transform.localPosition -= new Vector3(desvio.x, 0f, desvio.z);
            }
        }

        private void Orientar()
        {
            Transform derecha = BuscarHueso(HuesoManoDerecha);
            Transform izquierda = BuscarHueso(HuesoManoIzquierda);
            if (derecha == null || izquierda == null)
            {
                return;
            }

            Vector3 lado = transform.InverseTransformDirection(derecha.position - izquierda.position);
            lado.y = 0f;
            if (lado.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float angulo = Vector3.SignedAngle(lado, Vector3.right, Vector3.up);
            Instancia.transform.localRotation = Quaternion.Euler(0f, angulo, 0f) * Instancia.transform.localRotation;
        }

        private Transform BuscarHuesoQueContenga(string parte)
        {
            foreach (Transform hueso in Instancia.GetComponentsInChildren<Transform>(true))
            {
                if (hueso.name.StartsWith("Bip") && hueso.name.Contains(parte))
                {
                    return hueso;
                }
            }

            return null;
        }

        private bool LimitesDeHuesos(out float abajo, out float arriba)
        {
            abajo = float.MaxValue;
            arriba = float.MinValue;

            foreach (Transform hueso in Instancia.GetComponentsInChildren<Transform>(true))
            {
                if (!hueso.name.StartsWith("Bip"))
                {
                    continue;
                }

                float y = hueso.position.y;
                abajo = Mathf.Min(abajo, y);
                arriba = Mathf.Max(arriba, y);
            }

            return arriba > abajo;
        }

        private void LateUpdate()
        {

            if (!ocultarEnPrimeraPersona || renderersModelo == null)
            {
                return;
            }

            if (camara == null)
            {
                camara = FindAnyObjectByType<CamaraTerceraPersona>();
                if (camara == null || !EsJugador())
                {
                    ocultarEnPrimeraPersona = false;
                    return;
                }
            }

            if (camara.EnPrimeraPersona == enPrimeraPersona)
            {
                return;
            }

            enPrimeraPersona = camara.EnPrimeraPersona;
            ShadowCastingMode modo = enPrimeraPersona ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;

            foreach (Renderer renderer in renderersModelo)
            {
                if (renderer != null)
                {
                    renderer.shadowCastingMode = modo;
                }
            }
        }

        private bool EsJugador()
        {
            return CompareTag("Player");
        }
    }
}
