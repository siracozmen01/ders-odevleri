// File: Assets/Scripts/FSM/StateMachine.cs  (week 7 section 8.3, unchanged)
// Not a MonoBehaviour: the guard creates it and calls Update every frame.
using UnityEngine;

public class StateMachine<T>
{
    readonly T owner;

    public State<T> Current { get; private set; }
    public State<T> Previous { get; private set; }
    public State<T> Global { get; set; }

    public StateMachine(T owner)
    {
        this.owner = owner;
    }

    // Every frame: the global state first, then the current state
    public void Update()
    {
        Global?.Execute(owner);
        Current?.Execute(owner);
    }

    public void ChangeState(State<T> next)
    {
        if (next == null || next == Current) return;
        Debug.Log($"[{Time.time:0.0} sn] {Current?.Name ?? "-"} → {next.Name}");
        Previous = Current;        // 1. keep the old state
        Current?.Exit(owner);      // 2. leave the old state
        Current = next;            // 3. the new state becomes current
        Current.Enter(owner);      // 4. enter the new state
    }

    public void RevertToPreviousState()
    {
        ChangeState(Previous);
    }

    public bool IsIn(State<T> state)
    {
        return Current == state;
    }
}
