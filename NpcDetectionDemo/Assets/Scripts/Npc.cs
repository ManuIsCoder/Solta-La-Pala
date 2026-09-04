using System.Collections;
using UnityEngine;

// Logica de patrulla y deteccion basada en Guard.cs (HenrySpartGlobal/Unity_Stealth_Game)
public class Npc : MonoBehaviour
{
    public static event System.Action OnNpcDetectoJugador;

    public string nombre;
    public int reputacionDelJugador;
    public CampoDeVision vision = new CampoDeVision();

    public float velocidad = 5f;
    public float tiempoEspera = 0.3f;
    public float velocidadGiro = 90f;
    public float tiempoParaDetectar = 0.5f;

    public Light luzCono;
    public Transform rutaPatrulla;

    Transform jugador;
    float temporizadorVision;
    Color colorLuzOriginal;

    public bool EstaViendoJugador { get; private set; }

    void Start()
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        GameObject jugadorGo = GameObject.FindGameObjectWithTag("Player");
        if (jugadorGo != null)
            jugador = jugadorGo.transform;

        if (luzCono != null)
        {
            vision.angulo = luzCono.spotAngle;
            colorLuzOriginal = luzCono.color;
        }

        if (rutaPatrulla == null || rutaPatrulla.childCount < 2)
        {
            Debug.LogError("Npc: rutaPatrulla necesita al menos 2 puntos.");
            return;
        }

        Vector3[] puntos = new Vector3[rutaPatrulla.childCount];
        for (int i = 0; i < puntos.Length; i++)
        {
            puntos[i] = rutaPatrulla.GetChild(i).position;
            puntos[i] = new Vector3(puntos[i].x, transform.position.y, puntos[i].z);
        }
        StartCoroutine(SeguirRuta(puntos));
    }

    void Update()
    {
        if (jugador == null)
            return;

        EstaViendoJugador = Detectar();
        Reaccionar(EstaViendoJugador);
    }

    public bool Detectar()
    {
        return vision.PuedeVer(transform, jugador);
    }

    public void Reaccionar(bool visto)
    {
        if (visto)
            temporizadorVision += Time.deltaTime;
        else
            temporizadorVision -= Time.deltaTime;

        temporizadorVision = Mathf.Clamp(temporizadorVision, 0f, tiempoParaDetectar);

        if (luzCono != null)
            luzCono.color = Color.Lerp(colorLuzOriginal, Color.red, temporizadorVision / tiempoParaDetectar);

        if (temporizadorVision >= tiempoParaDetectar)
        {
            if (OnNpcDetectoJugador != null)
                OnNpcDetectoJugador();
        }
    }

    IEnumerator SeguirRuta(Vector3[] puntos)
    {
        transform.position = puntos[0];

        int indice = 1;
        Vector3 destino = puntos[indice];
        transform.LookAt(destino);

        while (true)
        {
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
            if (Vector3.Distance(transform.position, destino) < 0.05f)
            {
                transform.position = destino;
                indice = (indice + 1) % puntos.Length;
                destino = puntos[indice];
                yield return new WaitForSeconds(tiempoEspera);
                yield return StartCoroutine(GirarHacia(destino));
            }
            yield return null;
        }
    }

    IEnumerator GirarHacia(Vector3 destino)
    {
        Vector3 direccion = (destino - transform.position).normalized;
        float anguloObjetivo = 90f - Mathf.Atan2(direccion.z, direccion.x) * Mathf.Rad2Deg;

        while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, anguloObjetivo)) > 0.05f)
        {
            float angulo = Mathf.MoveTowardsAngle(transform.eulerAngles.y, anguloObjetivo, velocidadGiro * Time.deltaTime);
            transform.eulerAngles = Vector3.up * angulo;
            yield return null;
        }
    }

    void OnDrawGizmos()
    {
        if (rutaPatrulla != null && rutaPatrulla.childCount > 0)
        {
            Vector3 inicio = rutaPatrulla.GetChild(0).position;
            Vector3 anterior = inicio;

            foreach (Transform punto in rutaPatrulla)
            {
                Gizmos.DrawSphere(punto.position, 0.3f);
                Gizmos.DrawLine(anterior, punto.position);
                anterior = punto.position;
            }
            Gizmos.DrawLine(anterior, inicio);
        }

        Gizmos.color = Color.red;
        float radio = vision != null ? vision.radio : 12f;
        Gizmos.DrawRay(transform.position, transform.forward * radio);
    }
}
