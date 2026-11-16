// File: Assets/Scripts/NpcWaypointGuard.cs  (week 5 lecture notes, section 8.5)
// Homework additions are marked with [HOMEWORK] (task 3: ping-pong patrol) and [BONUS] (section 7.2 path smoothing).
using System.Collections.Generic;
using UnityEngine;

public class NpcWaypointGuard : MonoBehaviour
{
    enum State { Patrol, Chase, Return }

    public WaypointGraph graph;
    public Transform player;
    public Waypoint[] patrolRoute;                       // patrol order: W0 ... W7
    public float patrolSpeed = 2f, chaseSpeed = 4f, arriveRadius = 0.2f;
    public float viewDistance = 10f, viewAngle = 120f, loseTime = 3f;

    // [HOMEWORK] Task 3: ping-pong mode
    public bool pingPong = false;   // true: 0 1 2 ... 7 6 5 ... 0 1 ...
    int patrolDir = 1;              // +1 forward, -1 backward

    // [BONUS] Section 7.2: path smoothing at the end of PlanTo
    public bool smoothPath = true;

    State state = State.Patrol;
    int patrolIndex;
    Waypoint heading;                                    // node it is walking to right now (or null)

    readonly GraphAStar astar = new();
    readonly List<Vector3> path = new();                 // points to follow
    readonly List<Waypoint> pathNodes = new();           // nodes in the same order; null for the last point
    int pathIndex;
    float lastSeenTime = -999f, replanTimer;

    // Read-only views used by the runtime visualizer (GraphVisualizer) and the editor experiments.
    public IReadOnlyList<Vector3> CurrentPath => path;
    public int CurrentPathIndex => pathIndex;
    public string CurrentState => state.ToString();

    void Update()
    {
        bool sees = CanSeePlayer();
        if (sees) lastSeenTime = Time.time;

        if (state == State.Patrol)
        {
            if (sees) { state = State.Chase; replanTimer = 0f; return; }
            heading = patrolRoute[patrolIndex];
            if (MoveTo(heading.transform.position, patrolSpeed))
            {
                if (pingPong && patrolRoute.Length > 1)
                {
                    // [HOMEWORK] If the next index (patrolIndex + patrolDir) leaves the array, flip the direction,
                    // then add patrolDir to patrolIndex. (A one-element route has nowhere to turn, so it is skipped.)
                    int next = patrolIndex + patrolDir;
                    if (next < 0 || next >= patrolRoute.Length) patrolDir = -patrolDir;
                    patrolIndex += patrolDir;                                // ping-pong mode
                }
                else
                {
                    patrolIndex = (patrolIndex + 1) % patrolRoute.Length;   // loop mode (unchanged)
                }
            }
        }
        else if (state == State.Chase)
        {
            if (Time.time - lastSeenTime > loseTime) { StartReturn(); return; }
            replanTimer -= Time.deltaTime;
            if (replanTimer <= 0f) { PlanTo(player.position); replanTimer = 0.5f; }
            FollowPath(chaseSpeed);
        }
        else // State.Return
        {
            if (sees) { state = State.Chase; replanTimer = 0f; return; }
            if (FollowPath(patrolSpeed)) state = State.Patrol;
        }
    }

    // Sight: distance, angle, obstacle (week 3)
    bool CanSeePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        if (toPlayer.magnitude > viewDistance) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > viewAngle / 2f) return false;
        return graph.CanSee(transform.position, player.position);
    }

    // Build a path to the target: walk straight if you can see it, otherwise A* over the graph
    void PlanTo(Vector3 target)
    {
        path.Clear();
        pathNodes.Clear();
        pathIndex = 0;

        if (!graph.CanSee(transform.position, target))
        {
            Waypoint start = heading != null ? heading : graph.Nearest(transform.position);
            Waypoint goal = graph.Nearest(target);
            List<Waypoint> nodes = (start != null && goal != null) ? astar.FindPath(start, goal) : null;
            if (nodes != null)
            {
                foreach (Waypoint w in nodes) { path.Add(w.transform.position); pathNodes.Add(w); }
                Debug.Log($"A*: {start.name} → {goal.name}, yol {nodes.Count} düğüm, {astar.Expanded} genişletildi");
            }
        }
        path.Add(target);                                // last stop: the target itself
        pathNodes.Add(null);

        if (smoothPath) SmoothPath();                    // [BONUS] section 7.2

        heading = pathNodes[0];
    }

    // [BONUS] Section 7.2 path smoothing:
    //   i <- NPC position; while i is not the end of the path:
    //       walking back from the end, find the first j that i can see; go from i to j; i <- j
    // The points between i and j are removed from path (and pathNodes, which stays in step with it).
    void SmoothPath()
    {
        var keptPoints = new List<Vector3>();
        var keptNodes = new List<Waypoint>();
        Vector3 from = transform.position;               // i = NPC position (not stored in path)
        int i = -1;
        while (i < path.Count - 1)
        {
            int j = path.Count - 1;
            while (j > i + 1 && !graph.CanSee(from, path[j])) j--;   // i+1 is always reachable: it is an edge
            keptPoints.Add(path[j]);
            keptNodes.Add(pathNodes[j]);
            from = path[j];
            i = j;
        }

        if (keptPoints.Count < path.Count)
            Debug.Log($"Yumuşatma: {path.Count} nokta → {keptPoints.Count} nokta");

        path.Clear();
        path.AddRange(keptPoints);
        pathNodes.Clear();
        pathNodes.AddRange(keptNodes);
    }

    // Lost sight: return to the nearest patrol point with A*
    void StartReturn()
    {
        int best = 0;
        for (int i = 1; i < patrolRoute.Length; i++)
            if (Dist(patrolRoute[i]) < Dist(patrolRoute[best])) best = i;
        patrolIndex = best;
        PlanTo(patrolRoute[best].transform.position);
        state = State.Return;
    }

    float Dist(Waypoint w) => Vector3.Distance(transform.position, w.transform.position);

    // Follow the path in order; true when finished
    bool FollowPath(float speed)
    {
        if (pathIndex >= path.Count) return true;
        if (MoveTo(path[pathIndex], speed))
        {
            pathIndex++;
            heading = pathIndex < pathNodes.Count ? pathNodes[pathIndex] : null;
        }
        return pathIndex >= path.Count;
    }

    // Take one step towards the target and face it; true when arrived
    bool MoveTo(Vector3 target, float speed)
    {
        target.y = transform.position.y;                 // only in the ground plane
        Vector3 dir = target - transform.position;
        if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        return Vector3.Distance(transform.position, target) < arriveRadius;
    }

    void OnDrawGizmos()
    {
        // View direction: green patrol, red chase, yellow return
        Gizmos.color = state == State.Chase ? Color.red : state == State.Return ? Color.yellow : Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * viewDistance);

        // While patrolling: line to the current target point
        if (state == State.Patrol && heading != null)
            Gizmos.DrawLine(transform.position, heading.transform.position);

        // Remaining path
        for (int i = pathIndex; i < path.Count; i++)
            Gizmos.DrawLine(i == pathIndex ? transform.position : path[i - 1], path[i]);
    }
}
