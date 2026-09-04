using System.Collections;
using UnityEngine;

// Patrulla y deteccion basada en Guard.cs (HenrySpartGlobal/Unity_Stealth_Game)
public class Npc : MonoBehaviour
{
    public string nombre;
    public int reputacionDelJugador;
    public CampoDeVision vision = new CampoDeVision();

    public float velocidad = 5f;
    public float tiempoEspera = 0.3f;
    public float velocidadGiro = 90f;

    public Light luzCono;
    public Transform rutaPatrulla;

    Transform jugador;
    Color colorLuzOriginal;

    public bool EstaViendoJugador { get; private set; }

    void Start()
    {
        jugador = GameObject.FindGameObjectWithTag("Player").transform;

        vision.angulo = luzCono.spotAngle;
        colorLuzOriginal = luzCono.color;

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
        EstaViendoJugador = vision.PuedeVer(transform, jugador);

        if (EstaViendoJugador)
            luzCono.color = Color.red;
        else
            luzCono.color = colorLuzOriginal;
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

            // Llego al punto de la ruta → espero, giro y voy al siguiente
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
        if (rutaPatrulla == null)
            return;

        Vector3 anterior = rutaPatrulla.GetChild(0).position;
        foreach (Transform punto in rutaPatrulla)
        {
            Gizmos.DrawSphere(punto.position, 0.3f);
            Gizmos.DrawLine(anterior, punto.position);
            anterior = punto.position;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * vision.radio);
    }
}
