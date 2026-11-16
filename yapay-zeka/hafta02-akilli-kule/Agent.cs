using System;

/// <summary>
/// A moving agent (simplified Buckland Vehicle / MovingEntity).
/// The AI only decides a force; Update turns it into acceleration, velocity and position
/// with semi-implicit Euler: velocity first, then position.
/// </summary>
public class Agent
{
    public Vector2D Position;
    public Vector2D Velocity;
    /// <summary>Facing direction (unit vector). Only updated while the agent moves.</summary>
    public Vector2D Heading = new Vector2D(0, 1);
    public double Mass = 1.0;
    public double MaxSpeed = double.MaxValue;
    public double MaxForce = double.MaxValue;

    public Agent() { }

    public Agent(Vector2D position, double mass, double maxSpeed, double maxForce)
    {
        Position = position;
        Mass = mass;
        MaxSpeed = maxSpeed;
        MaxForce = maxForce;
    }

    /// <summary>One Euler step of length <paramref name="dt"/> seconds.</summary>
    public void Update(Vector2D force, double dt)
    {
        force = force.Truncate(MaxForce);                 // the "engine" cannot push harder than MaxForce
        Vector2D acceleration = force * (1.0 / Mass);     // a = F / m
        Velocity = Velocity + acceleration * dt;          // v += a * dt
        Velocity = Velocity.Truncate(MaxSpeed);           // |v| <= MaxSpeed
        Position = Position + Velocity * dt;              // p += v * dt
        if (Velocity.LengthSq() > 1e-8)                   // a stopped agent keeps its old heading
            Heading = Velocity.Normalized();
    }
}
