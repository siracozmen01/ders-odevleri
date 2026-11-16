// File: Assets/Scripts/Waypoint.cs  (week 5 lecture notes, section 8.2)
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public List<Waypoint> neighbors = new();   // adjacency list

    void OnDrawGizmos()
    {
        // Node: cyan sphere
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.3f);

        // Edges: white lines
        Gizmos.color = Color.white;
        foreach (Waypoint n in neighbors)
            if (n != null) Gizmos.DrawLine(transform.position, n.transform.position);
    }
}
