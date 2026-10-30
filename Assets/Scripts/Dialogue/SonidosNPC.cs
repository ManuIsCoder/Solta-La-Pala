using UnityEngine;

namespace SoltaLaPala.Dialogue
{
    [RequireComponent(typeof(DialogoNPC))]
    public class SonidosNPC : MonoBehaviour
    {
        private DialogoNPC npc;
        private AudioSource fuenteVoz;
        private AudioSource fuenteTos;
        private InterfazDialogo interfaz;

        private float proximaTos;

        private VozNPC vozCargada = VozNPC.Ninguna;
        private AudioClip clipVoz;
        private TosNPC tosCargada = TosNPC.Ninguna;
        private AudioClip clipTos;

        private void Awake()
        {
            npc = GetComponent<DialogoNPC>();

            fuenteVoz = CrearFuente("Voz", loop: true);
            fuenteTos = CrearFuente("Tos", loop: false);

            ProgramarProximaTos(primeraVez: true);
        }

        private AudioSource CrearFuente(string nombre, bool loop)
        {
            GameObject objeto = new GameObject("Audio" + nombre);
            objeto.transform.SetParent(transform, false);

            AudioSource fuente = objeto.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.loop = loop;
            fuente.spatialBlend = 1f;
            fuente.minDistance = 3f;
            fuente.maxDistance = 25f;
            fuente.rolloffMode = AudioRolloffMode.Linear;

            return fuente;
        }

        private void OnDisable()
        {
            Desuscribir();
            if (fuenteVoz != null) fuenteVoz.Stop();
        }

        private void Update()
        {
            if (interfaz == null)
            {
                Suscribir();
            }

            ActualizarTos();
        }

        private void Suscribir()
        {
            if (GestorDialogos.Instancia == null || GestorDialogos.Instancia.interfaz == null)
            {
                return;
            }

            interfaz = GestorDialogos.Instancia.interfaz;
            interfaz.AlEmpezarLinea += AlEmpezarLinea;
            interfaz.AlTerminarLinea += AlTerminarLinea;
        }

        private void Desuscribir()
        {
            if (interfaz == null)
            {
                return;
            }

            interfaz.AlEmpezarLinea -= AlEmpezarLinea;
            interfaz.AlTerminarLinea -= AlTerminarLinea;
            interfaz = null;
        }

        private bool HablandoConmigo()
        {
            GestorDialogos gestor = GestorDialogos.Instancia;

            return gestor != null && gestor.DialogoActivo && gestor.NpcActual == npc;
        }

        private void AlEmpezarLinea(float duracion)
        {
            if (!HablandoConmigo())
            {
                return;
            }

            AudioClip clip = ObtenerVoz();
            if (clip == null)
            {
                return;
            }

            fuenteVoz.clip = clip;
            fuenteVoz.Play();
        }

        private void AlTerminarLinea()
        {
            if (fuenteVoz.isPlaying)
            {
                fuenteVoz.Stop();
            }

            ProgramarProximaTos();
        }

        private void ActualizarTos()
        {
            if (Time.timeScale <= 0f || HablandoConmigo())
            {
                return;
            }

            if (Time.time < proximaTos)
            {
                return;
            }

            AudioClip clip = ObtenerTos();
            if (clip != null && !fuenteTos.isPlaying)
            {
                fuenteTos.PlayOneShot(clip);
            }

            ProgramarProximaTos();
        }

        private void ProgramarProximaTos(bool primeraVez = false)
        {
            if (primeraVez)
            {
                proximaTos = Time.time + Random.Range(4f, 10f);
            }
            else
            {
                proximaTos = Time.time + Random.Range(npc.tosCadaMinimo, Mathf.Max(npc.tosCadaMinimo, npc.tosCadaMaximo));
            }
        }

        private AudioClip ObtenerVoz()
        {
            if (npc.voz == VozNPC.Ninguna)
            {
                return null;
            }

            if (clipVoz == null || vozCargada != npc.voz)
            {
                clipVoz = Resources.Load<AudioClip>("Audio/Voces/Balbuceo" + npc.voz);
                vozCargada = npc.voz;

                if (clipVoz == null)
                {
                    Debug.LogWarning($"[SonidosNPC] No se encontro el audio de la voz '{npc.voz}' " +
                                     "en Resources/Audio/Voces.", this);
                }
            }

            return clipVoz;
        }

        private AudioClip ObtenerTos()
        {
            if (npc.sonidoToz == TosNPC.Ninguna)
            {
                return null;
            }

            if (clipTos == null || tosCargada != npc.sonidoToz)
            {
                clipTos = Resources.Load<AudioClip>("Audio/Tos/" + npc.sonidoToz);
                tosCargada = npc.sonidoToz;

                if (clipTos == null)
                {
                    Debug.LogWarning($"[SonidosNPC] No se encontro el audio de tos '{npc.sonidoToz}' " +
                                     "en Resources/Audio/Tos.", this);
                }
            }

            return clipTos;
        }
    }
}
