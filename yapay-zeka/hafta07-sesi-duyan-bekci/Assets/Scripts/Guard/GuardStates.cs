// File: Assets/Scripts/Guard/GuardStates.cs  (week 7 section 8.5 + homework "Sesi Duyan Bekçi")
// Six classes in one file. None is a MonoBehaviour, so the file name does not have to match a class name.
// Compare each class with the table in 5.1 (and the Araştır row in the report).
using UnityEngine;

// Global state: runs every frame, BEFORE the current state (6.2)
public class GuardGlobal : State<Guard>
{
    public override void Execute(Guard g)
    {
        if (!g.CanSeePlayer()) return;
        g.lastSeenPosition = g.player.position;
        g.lastSeenTime = Time.time;
        if (!g.FSM.IsIn(g.Chase)) g.FSM.ChangeState(g.Chase);   // also covers Araştır: no extra code (homework rule)
    }
}

public class GuardPatrol : State<Guard>
{
    public override string Name => "Devriye";

    public override void Enter(Guard g)
    {
        g.SetColor(Color.green);
        g.Agent.SetDestination(g.patrolPoints[g.patrolIndex].position);
    }

    public override void Execute(Guard g)
    {
        if (g.DoorBlocksMe()) { g.FSM.ChangeState(g.WaitAtDoor); return; }
        if (g.Arrived())   // reached the point: the state does not change, only the next point (3.2)
        {
            g.patrolIndex = (g.patrolIndex + 1) % g.patrolPoints.Length;
            g.Agent.SetDestination(g.patrolPoints[g.patrolIndex].position);
        }
    }
}

public class GuardChase : State<Guard>
{
    public override string Name => "Kovala";

    public override void Enter(Guard g)
    {
        g.SetColor(Color.red);
        g.Agent.speed = g.chaseSpeed;
    }

    public override void Execute(Guard g)
    {
        if (Time.time - g.lastSeenTime > g.loseTime) { g.FSM.ChangeState(g.Search); return; }
        g.Agent.SetDestination(g.lastSeenPosition);
    }

    public override void Exit(Guard g)
    {
        g.Agent.speed = g.patrolSpeed;   // undo the setting changed in Enter
    }
}

public class GuardSearch : State<Guard>
{
    float lookTimer;   // this state object belongs to one guard, so it can hold data (6.5)

    public override string Name => "Ara";

    public override void Enter(Guard g)
    {
        g.SetColor(Color.yellow);
        g.Agent.SetDestination(g.lastSeenPosition);
        lookTimer = g.searchTime;   // the timer starts from zero on every entry (3.3)
    }

    public override void Execute(Guard g)
    {
        if (g.DoorBlocksMe()) { g.FSM.ChangeState(g.WaitAtDoor); return; }
        if (!g.Arrived()) return;                              // not there yet
        g.transform.Rotate(0f, 90f * Time.deltaTime, 0f);      // turn and look around
        lookTimer -= Time.deltaTime;
        if (lookTimer <= 0f) g.FSM.ChangeState(g.Patrol);
    }

    public float LookTimer => lookTimer;   // read by GuardDebugView
}

public class GuardWaitAtDoor : State<Guard>
{
    public override string Name => "KapıdaBekle";

    public override void Enter(Guard g)
    {
        g.SetColor(Color.gray);
        g.Agent.isStopped = true;
    }

    public override void Execute(Guard g)
    {
        if (g.door.IsOpen) g.FSM.RevertToPreviousState();   // Devriye, Ara or [ÖDEV] Araştır (6.3)
    }

    public override void Exit(Guard g)
    {
        g.Agent.isStopped = false;   // without this line the guard freezes (5.2)
    }
}

// [ÖDEV] Fifth state: the guard heard a noise. It walks to the noise position, looks around for
// investigateTime seconds and goes back to patrol. Seeing the player is handled by GuardGlobal.
//   Enter:   purple, investigate speed, target = noise position, start the look timer
//   Execute: door in the way? -> KapıdaBekle; arrived? turn and look; time is up? -> Devriye
//   Exit:    restore the patrol speed (every setting changed in Enter is undone in Exit, 5.2)
public class GuardInvestigate : State<Guard>
{
    public static readonly Color Purple = new Color(0.62f, 0.22f, 0.95f);   // not green, red, yellow or gray

    float lookTimer;

    public override string Name => "Araştır";

    public override void Enter(Guard g)
    {
        g.SetColor(Purple);
        g.Agent.speed = g.investigateSpeed;
        g.Agent.SetDestination(g.noisePosition);
        lookTimer = g.investigateTime;   // also restarts after KapıdaBekle sends the guard back here
    }

    public override void Execute(Guard g)
    {
        if (g.DoorBlocksMe()) { g.FSM.ChangeState(g.WaitAtDoor); return; }
        if (!g.Arrived()) return;                              // still walking to the noise
        g.transform.Rotate(0f, 90f * Time.deltaTime, 0f);      // turn and look around
        lookTimer -= Time.deltaTime;
        if (lookTimer <= 0f) g.FSM.ChangeState(g.Patrol);
    }

    public override void Exit(Guard g)
    {
        g.Agent.speed = g.patrolSpeed;   // Chase / WaitAtDoor / Patrol all expect the normal speed
    }

    // A new noise while investigating: not a transition, Enter/Exit do not run.
    // Only the target and the look timer change (called from Guard.HearNoise).
    public void Retarget(Guard g)
    {
        g.Agent.SetDestination(g.noisePosition);
        lookTimer = g.investigateTime;
    }

    public float LookTimer => lookTimer;   // read by GuardDebugView
}
