// File: Assets/Scripts/FSM/State.cs  (week 7 section 8.2, unchanged)
// T: the class that owns the state (Guard in this course)
public abstract class State<T>
{
    public virtual string Name => GetType().Name;
    public virtual void Enter(T owner) { }      // once, when entering the state
    public abstract void Execute(T owner);      // every frame while in the state
    public virtual void Exit(T owner) { }       // once, when leaving the state
}
