using UnityEngine;

[System.Serializable]
public class CampoDeVision
{
    public float angulo = 70f;
    public float radio = 12f;
    public LayerMask mascaraObstaculos;

    // Misma logica que Guard.CanSeePlayer (Unity_Stealth_Game)
    public bool PuedeVer(Transform origen, Transform objetivo)
    {
        if (Vector3.Distance(origen.position, objetivo.position) < radio)
        {
            Vector3 direccion = (objetivo.position - origen.position).normalized;
            float anguloEntre = Vector3.Angle(origen.forward, direccion);
            if (anguloEntre < angulo / 2f)
            {
                if (!Physics.Linecast(origen.position, objetivo.position, mascaraObstaculos))
                    return true;
            }
        }
        return false;
    }
}
