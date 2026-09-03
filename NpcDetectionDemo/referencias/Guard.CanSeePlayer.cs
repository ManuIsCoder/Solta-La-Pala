// HenrySpartGlobal/Unity_Stealth_Game — Stealth_Game/Assets/Scripts/Guard.cs

bool CanSeePlayer(){
    if (Vector3.Distance(transform.position, player.position) < viewDistance) {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angleBetweenGuardAndPlayer = Vector3.Angle(transform.forward, dirToPlayer);
        if (angleBetweenGuardAndPlayer < viewAngle / 2f) {
            if (!Physics.Linecast(transform.position, player.position, viewMask)) {
                return true;
            }
        }
    }
    return false;
}
