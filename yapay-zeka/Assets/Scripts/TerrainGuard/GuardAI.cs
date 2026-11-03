using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Guard with a finite state machine:
///   Patrol  -> walks between waypoints on the NavMesh
///   Suspicious -> player glimpsed; "awareness" fills up while the player stays in the cone
///   Chase   -> awareness full: raises the alarm and chases the player over the terrain
///   Search  -> lost sight: goes to the last known position and looks around
///   (then back to Patrol)
/// Vision = distance check + angle check (cone) + raycast (line of sight, walls/hills block it).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class GuardAI : MonoBehaviour
{
    public enum State { Patrol, Suspicious, Chase, Search }

    [Header("Patrol")]
    public Transform[] waypoints;
    public float patrolSpeed = 2.5f;
    public float waitAtWaypoint = 1.5f;

    [Header("Vision cone")]
    public float viewDistance = 14f;
    [Range(10f, 180f)] public float viewAngle = 70f;
    public float eyeHeight = 1.6f;
    public LayerMask obstacleMask = ~0;
    [Tooltip("Crouching player is only seen within this fraction of viewDistance")]
    public float crouchDistanceFactor = 0.55f;

    [Header("Detection")]
    [Tooltip("Seconds the player must stay in the cone before the alarm goes off")]
    public float timeToDetect = 0.8f;
    public float chaseSpeed = 5.5f;
    public float catchDistance = 1.3f;
    public float searchDuration = 5f;

    public State CurrentState { get; private set; } = State.Patrol;
    /// <summary>0..1, how sure the guard is that it saw the player.</summary>
    public float Awareness { get; private set; }
    public bool CanSeePlayer { get; private set; }

    NavMeshAgent agent;
    Transform player;
    PlayerController playerController;
    int waypointIndex;
    float waitTimer;
    float searchTimer;
    Vector3 lastKnownPosition;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<PlayerController>();
        }
    }

    void OnEnable() { AlarmSystem.AlarmRaised += OnAlarm; }
    void OnDisable() { AlarmSystem.AlarmRaised -= OnAlarm; }

    void Start()
    {
        agent.speed = patrolSpeed;
        if (waypoints != null && waypoints.Length > 0)
            agent.SetDestination(waypoints[0].position);
    }

    void Update()
    {
        if (player == null) return;

        CanSeePlayer = CheckVision();

        switch (CurrentState)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Suspicious: UpdateSuspicious(); break;
            case State.Chase: UpdateChase(); break;
            case State.Search: UpdateSearch(); break;
        }
    }

    // ---------------- Vision ----------------

    public bool IsPointInCone(Vector3 point)
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 toTarget = point - eye;
        float range = viewDistance;
        if (playerController != null && playerController.IsCrouching) range *= crouchDistanceFactor;

        if (toTarget.magnitude > range) return false;                                   // 1) distance
        Vector3 flat = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        if (Vector3.Angle(transform.forward, flat) > viewAngle * 0.5f) return false;     // 2) angle (cone)
        return true;
    }

    bool CheckVision()
    {
        if (playerController != null && playerController.IsCaught) return false;
        Vector3 target = player.position + Vector3.up * 1f;
        if (!IsPointInCone(target)) return false;

        // 3) line of sight: anything between the eye and the player (hills, walls, rocks) blocks the view
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 dir = target - eye;
        if (Physics.Raycast(eye, dir.normalized, out RaycastHit hit, dir.magnitude, obstacleMask, QueryTriggerInteraction.Ignore))
            return hit.transform == player || hit.transform.IsChildOf(player);
        return true;
    }

    // ---------------- States ----------------

    void UpdatePatrol()
    {
        Awareness = Mathf.MoveTowards(Awareness, 0f, Time.deltaTime);
        if (CanSeePlayer) { SetState(State.Suspicious); return; }

        if (waypoints == null || waypoints.Length == 0) return;
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitAtWaypoint)
            {
                waitTimer = 0f;
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
                agent.SetDestination(waypoints[waypointIndex].position);
            }
        }
    }

    void UpdateSuspicious()
    {
        agent.isStopped = true;
        if (CanSeePlayer)
        {
            // turn towards the player while getting sure
            Vector3 look = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 5f * Time.deltaTime);

            Awareness += Time.deltaTime / Mathf.Max(0.01f, timeToDetect);
            if (Awareness >= 1f)
            {
                Awareness = 1f;
                lastKnownPosition = player.position;
                AlarmSystem.Raise(this, player.position);
                SetState(State.Chase);
            }
        }
        else
        {
            Awareness -= Time.deltaTime * 0.5f;
            if (Awareness <= 0f) { Awareness = 0f; SetState(State.Patrol); }
        }
    }

    void UpdateChase()
    {
        if (CanSeePlayer)
        {
            lastKnownPosition = player.position;
            AlarmSystem.ReportSighting(player.position);
        }

        agent.SetDestination(lastKnownPosition);

        if (Vector3.Distance(transform.position, player.position) <= catchDistance && CanSeePlayer)
        {
            if (playerController != null) playerController.IsCaught = true;
            GameUI.ShowCaught();
            agent.isStopped = true;
            return;
        }

        if (!CanSeePlayer && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            SetState(State.Search);
    }

    void UpdateSearch()
    {
        if (CanSeePlayer) { SetState(State.Chase); return; }

        // look around at the last known position
        transform.Rotate(0f, 90f * Time.deltaTime, 0f);
        searchTimer += Time.deltaTime;
        if (searchTimer >= searchDuration)
        {
            Awareness = 0f;
            SetState(State.Patrol);
        }
    }

    void SetState(State s)
    {
        CurrentState = s;
        agent.isStopped = false;
        switch (s)
        {
            case State.Patrol:
                agent.speed = patrolSpeed;
                agent.updateRotation = true;
                if (waypoints != null && waypoints.Length > 0)
                    agent.SetDestination(waypoints[waypointIndex].position);
                break;
            case State.Suspicious:
                agent.updateRotation = false;
                break;
            case State.Chase:
                agent.speed = chaseSpeed;
                agent.updateRotation = true;
                break;
            case State.Search:
                searchTimer = 0f;
                agent.isStopped = true;
                agent.updateRotation = false;
                break;
        }
    }

    /// <summary>Another guard raised the alarm: everybody runs to the reported position.</summary>
    void OnAlarm(GuardAI source, Vector3 position)
    {
        if (CurrentState == State.Chase) return;
        lastKnownPosition = position;
        Awareness = 1f;
        SetState(State.Chase);
    }

    public void HearReport(Vector3 position)
    {
        if (CurrentState == State.Chase && !CanSeePlayer) lastKnownPosition = position;
    }

    // ---------------- Debug drawing ----------------

    void OnDrawGizmos()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Gizmos.color = CanSeePlayer ? Color.red : Color.yellow;
        Vector3 left = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawLine(eye, eye + left * viewDistance);
        Gizmos.DrawLine(eye, eye + right * viewDistance);

        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.3f);
            var next = waypoints[(i + 1) % waypoints.Length];
            if (next != null) Gizmos.DrawLine(waypoints[i].position, next.position);
        }
    }
}
