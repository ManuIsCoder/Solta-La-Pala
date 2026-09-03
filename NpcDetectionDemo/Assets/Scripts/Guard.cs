using System.Collections;
using UnityEngine;

// HenrySpartGlobal/Unity_Stealth_Game — Stealth_Game/Assets/Scripts/Guard.cs
public class Guard : MonoBehaviour
{
    public static event System.Action OnGuardHasSpottedPlayer;

    public float speed = 5;
    public float waitTime = .3f;
    public float turnSpeed = 90;
    public float awarenessTimer = .5f;

    public Light spotlight;
    public float viewDistance;
    public LayerMask viewMask;

    float viewAngle;
    float playerVisibleTimer;

    public Transform pathHolder;
    Transform player;
    Color MainSpotLightColor;

    public bool IsSeeingPlayer { get; private set; }

    void Start()
    {
        // El Guard original mueve con transform; CharacterController lo bloquea.
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
            player = playerGo.transform;

        if (spotlight != null)
        {
            viewAngle = spotlight.spotAngle;
            MainSpotLightColor = spotlight.color;
        }
        else
        {
            viewAngle = 70f;
            Debug.LogWarning("Guard: falta Spot Light en Spotlight.");
        }

        if (pathHolder == null || pathHolder.childCount < 2)
        {
            Debug.LogError("Guard: Path Holder necesita al menos 2 hijos (waypoints).");
            return;
        }

        Vector3[] waypoints = new Vector3[pathHolder.childCount];
        for (int i = 0; i < waypoints.Length; i++)
        {
            waypoints[i] = pathHolder.GetChild(i).position;
            waypoints[i] = new Vector3(waypoints[i].x, transform.position.y, waypoints[i].z);
        }
        StartCoroutine(FollowPath(waypoints));
    }

    void Update()
    {
        if (player == null)
            return;

        IsSeeingPlayer = CanSeePlayer();

        if (IsSeeingPlayer)
            playerVisibleTimer += Time.deltaTime;
        else
            playerVisibleTimer -= Time.deltaTime;

        playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, awarenessTimer);

        if (spotlight != null)
            spotlight.color = Color.Lerp(MainSpotLightColor, Color.red, playerVisibleTimer / awarenessTimer);

        if (playerVisibleTimer >= awarenessTimer)
        {
            if (OnGuardHasSpottedPlayer != null)
                OnGuardHasSpottedPlayer();
        }
    }

    bool CanSeePlayer()
    {
        if (Vector3.Distance(transform.position, player.position) < viewDistance)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angleBetweenGuardAndPlayer = Vector3.Angle(transform.forward, dirToPlayer);
            if (angleBetweenGuardAndPlayer < viewAngle / 2f)
            {
                if (!Physics.Linecast(transform.position, player.position, viewMask))
                    return true;
            }
        }
        return false;
    }

    IEnumerator FollowPath(Vector3[] waypoints)
    {
        transform.position = waypoints[0];

        int targetWaypointIndex = 1;
        Vector3 targetWaypoint = waypoints[targetWaypointIndex];
        transform.LookAt(targetWaypoint);

        while (true)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWaypoint, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetWaypoint) < 0.05f)
            {
                transform.position = targetWaypoint;
                targetWaypointIndex = (targetWaypointIndex + 1) % waypoints.Length;
                targetWaypoint = waypoints[targetWaypointIndex];
                yield return new WaitForSeconds(waitTime);
                yield return StartCoroutine(TurnToFace(targetWaypoint));
            }
            yield return null;
        }
    }

    IEnumerator TurnToFace(Vector3 lookTarget)
    {
        Vector3 dirToLookTarget = (lookTarget - transform.position).normalized;
        float targetAngle = 90 - Mathf.Atan2(dirToLookTarget.z, dirToLookTarget.x) * Mathf.Rad2Deg;

        while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetAngle)) > 0.05f)
        {
            float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, turnSpeed * Time.deltaTime);
            transform.eulerAngles = Vector3.up * angle;
            yield return null;
        }
    }

    void OnDrawGizmos()
    {
        if (pathHolder != null)
        {
            Vector3 startPosition = pathHolder.GetChild(0).position;
            Vector3 previousPosition = startPosition;

            foreach (Transform waypoint in pathHolder)
            {
                Gizmos.DrawSphere(waypoint.position, .3f);
                Gizmos.DrawLine(previousPosition, waypoint.position);
                previousPosition = waypoint.position;
            }
            Gizmos.DrawLine(previousPosition, startPosition);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * viewDistance);
    }
}
