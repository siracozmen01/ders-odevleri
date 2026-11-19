// File: Assets/Scripts/Guard/Guard.cs  (week 7 section 8.4 + homework "Sesi Duyan Bekçi")
// The guard only holds data and helper methods. It does not decide anything; the states do.
// Lines added for the homework are marked [ÖDEV].
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Guard : MonoBehaviour
{
    [Header("Sahne")]
    public Transform player;
    public Transform[] patrolPoints;
    public SlidingDoor door;

    [Header("Hız (m/sn)")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float investigateSpeed = 3f;      // [ÖDEV] a bit faster than patrol: the guard hurries to the noise

    [Header("Görüş (3. hafta)")]
    public float viewDistance = 10f;
    public float viewAngle = 120f;

    [Header("Süreler ve mesafeler")]
    public float loseTime = 1f;              // not seen for this long -> Search
    public float searchTime = 5f;            // look-around time at the last seen position
    public float doorStopDistance = 2.5f;    // wait when this close to a closed door

    [Header("Ses (ödev)")]
    public float hearingDistance = 8f;       // [ÖDEV] hears a noise closer than this; walls do not block sound
    public float investigateTime = 3f;       // [ÖDEV] look-around time at the noise position

    // State objects: every guard has its own copies (6.5)
    public readonly GuardPatrol Patrol = new();
    public readonly GuardChase Chase = new();
    public readonly GuardSearch Search = new();
    public readonly GuardWaitAtDoor WaitAtDoor = new();
    public readonly GuardInvestigate Investigate = new();   // [ÖDEV] fifth state

    public StateMachine<Guard> FSM { get; private set; }
    public NavMeshAgent Agent { get; private set; }

    [HideInInspector] public int patrolIndex;
    [HideInInspector] public Vector3 lastSeenPosition;
    [HideInInspector] public float lastSeenTime;
    [HideInInspector] public Vector3 noisePosition;          // [ÖDEV] where the last heard noise came from
    [HideInInspector] public string lastHearing = "-";       // [ÖDEV] last HearNoise result, shown by GuardDebugView

    Renderer rend;

    void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        // The agent is saved disabled and switched on here, after the NavMeshSurface has loaded its data;
        // otherwise Unity logs "Failed to create agent" on the first frame (same fix as week 6).
        Agent.enabled = true;
        Agent.speed = patrolSpeed;
        rend = GetComponent<Renderer>();
        FSM = new StateMachine<Guard>(this);
        FSM.Global = new GuardGlobal();
    }

    void Start()
    {
        if (player == null || patrolPoints.Length == 0)
        {
            Debug.LogError("Guard: Player ve en az bir devriye noktası atanmalı.");
            enabled = false;
            return;
        }
        FSM.ChangeState(Patrol);
    }

    void Update()
    {
        FSM.Update();
    }

    // Sight: distance, angle, obstacle (week 3)
    public bool CanSeePlayer()
    {
        Vector3 eye = transform.position;
        Vector3 toPlayer = player.position + Vector3.up * 0.5f - eye;
        if (toPlayer.magnitude > viewDistance) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > viewAngle / 2f) return false;
        return Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, viewDistance)
               && hit.transform.IsChildOf(player);
    }

    // Is the door closed and the guard close to it? Height difference is ignored.
    public bool DoorBlocksMe()
    {
        if (door == null || door.IsOpen) return false;
        Vector3 d = door.transform.position - transform.position;
        d.y = 0f;
        return d.magnitude < doorStopDistance;
    }

    // Has the NavMeshAgent reached its destination?
    public bool Arrived()
    {
        return !Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance + 0.1f;
    }

    public void SetColor(Color c)
    {
        rend.material.color = c;
    }

    // [ÖDEV] Called by PlayerNoise when the player presses N.
    // This method checks the distance and the current state; the transition itself is a normal ChangeState.
    public void HearNoise(Vector3 position)
    {
        Vector3 d = position - transform.position;
        d.y = 0f;                                     // height difference is ignored, as in DoorBlocksMe
        float dist = d.magnitude;
        string now = $"[{Time.time:0.0} sn]";

        // 1. Too far: not heard. Walls are not checked because they do not block sound.
        if (dist > hearingDistance)
        {
            lastHearing = $"{dist:0.0} m: duyulmadı";
            Debug.Log($"{now} Ses {dist:0.0} m uzakta, {hearingDistance:0} m sınırının dışında: duyulmadı ({FSM.Current.Name})");
            return;
        }

        // 2. Already investigating: not a transition, only a new target (like reaching a patrol point, 3.2)
        if (FSM.IsIn(Investigate))
        {
            noisePosition = position;
            Investigate.Retarget(this);
            lastHearing = $"{dist:0.0} m: yeni hedef";
            Debug.Log($"{now} Ses duyuldu ({dist:0.0} m), zaten Araştır'da: hedef ve bakınma süresi yenilendi");
            return;
        }

        // 3. Only Patrol and Search react. Chase ignores it (the guard already sees the player),
        //    and WaitAtDoor stays at the closed door.
        if (!FSM.IsIn(Patrol) && !FSM.IsIn(Search))
        {
            lastHearing = $"{dist:0.0} m: görmezden gelindi";
            Debug.Log($"{now} Ses duyuldu ({dist:0.0} m) ama {FSM.Current.Name} durumunda: görmezden gelindi");
            return;
        }

        noisePosition = position;
        lastHearing = $"{dist:0.0} m: duyuldu";
        Debug.Log($"{now} Ses duyuldu ({dist:0.0} m, {FSM.Current.Name} durumunda)");
        FSM.ChangeState(Investigate);
    }

    // Show the last seen point and the noise point in the Scene window
    void OnDrawGizmos()
    {
        if (FSM == null) return;
        if (FSM.IsIn(Chase) || FSM.IsIn(Search))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastSeenPosition, 0.4f);
        }
        if (FSM.IsIn(Investigate))                     // [ÖDEV]
        {
            Gizmos.color = GuardInvestigate.Purple;
            Gizmos.DrawWireSphere(noisePosition, 0.4f);
        }
    }

    // [ÖDEV] Hearing range when the guard is selected
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.65f, 0.2f, 0.95f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, hearingDistance);
    }
}
