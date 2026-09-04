// HenrySpartGlobal/Unity_Stealth_Game — Guard.CanSeePlayer
// En la demo: CampoDeVision.PuedeVer

bool PuedeVer(Transform origen, Transform objetivo)
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
