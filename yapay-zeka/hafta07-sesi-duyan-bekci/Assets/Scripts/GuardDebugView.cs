// File: Assets/Scripts/GuardDebugView.cs  (visual only, not part of the FSM)
// Draws in the Game view what the Scene window gizmos would show, so screenshots can be read:
//   - the guard's view cone, clipped by walls (white),
//   - the guard's NavMesh path (on the LineRenderer that NpcNavChase used in week 6),
//   - a panel with the current/previous state, timers and the last noise result.
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Guard))]
public class GuardDebugView : MonoBehaviour
{
    public Material coneMaterial;
    public int coneRays = 41;

    Guard g;
    NavMeshAgent agent;
    LineRenderer pathLine, cone, target;
    GUIStyle box, text;

    void Start()
    {
        g = GetComponent<Guard>();
        agent = GetComponent<NavMeshAgent>();
        pathLine = GetComponent<LineRenderer>();

        var coneGo = new GameObject("GorusKonisi");
        cone = coneGo.AddComponent<LineRenderer>();
        cone.sharedMaterial = coneMaterial;
        cone.useWorldSpace = true;
        cone.loop = true;
        cone.widthMultiplier = 0.06f;
        cone.startColor = cone.endColor = new Color(1f, 1f, 1f, 0.75f);
        cone.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        cone.receiveShadows = false;

        // Small ring on the point the guard is heading for: noise (purple) or last seen position (yellow)
        var targetGo = new GameObject("HedefIsareti");
        target = targetGo.AddComponent<LineRenderer>();
        target.sharedMaterial = coneMaterial;
        target.useWorldSpace = false;
        target.loop = true;
        target.widthMultiplier = 0.1f;
        target.positionCount = 24;
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI * 2f / 24;
            target.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.45f, 0f, Mathf.Sin(a) * 0.45f));
        }
        target.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        target.receiveShadows = false;
    }

    void LateUpdate()
    {
        if (g == null || g.FSM == null) return;

        // Path (corners of the agent's current path)
        if (pathLine != null)
        {
            Vector3[] c = agent.hasPath && !agent.isStopped ? agent.path.corners : new Vector3[0];
            pathLine.positionCount = c.Length;
            for (int i = 0; i < c.Length; i++) pathLine.SetPosition(i, new Vector3(c[i].x, 0.1f, c[i].z));
        }

        // Target ring (same information as the gizmos in Guard.OnDrawGizmos)
        bool investigating = g.FSM.IsIn(g.Investigate) || (g.FSM.IsIn(g.WaitAtDoor) && g.FSM.Previous == g.Investigate);
        bool searching = g.FSM.IsIn(g.Chase) || g.FSM.IsIn(g.Search) || (g.FSM.IsIn(g.WaitAtDoor) && g.FSM.Previous == g.Search);
        target.enabled = investigating || searching;
        if (target.enabled)
        {
            Vector3 p = investigating ? g.noisePosition : g.lastSeenPosition;
            target.transform.position = new Vector3(p.x, 0.07f, p.z);
            target.startColor = target.endColor = investigating ? GuardInvestigate.Purple : Color.yellow;
        }

        // View cone: rays at the guard's eye height, cut where they hit something
        Vector3 eye = transform.position;
        cone.positionCount = coneRays + 1;
        cone.SetPosition(0, new Vector3(eye.x, 0.08f, eye.z));
        for (int i = 0; i < coneRays; i++)
        {
            float a = -g.viewAngle / 2f + g.viewAngle * i / (coneRays - 1);
            Vector3 dir = Quaternion.Euler(0f, a, 0f) * transform.forward;
            float d = g.viewDistance;   // nearest hit that is not the player (the player does not block sight)
            foreach (RaycastHit hit in Physics.RaycastAll(eye, dir, g.viewDistance))
                if (!hit.transform.IsChildOf(g.player) && hit.transform != transform && hit.distance < d) d = hit.distance;
            Vector3 p = eye + dir * d;
            cone.SetPosition(i + 1, new Vector3(p.x, 0.08f, p.z));
        }
    }

    void OnGUI()
    {
        if (g == null || g.FSM == null || g.FSM.Current == null) return;
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { normal = { background = Texture2D.whiteTexture } };
            text = new GUIStyle(GUI.skin.label) { fontSize = 17, richText = true, normal = { textColor = Color.white } };
        }

        string state = g.FSM.Current.Name;
        string timer = "";
        if (g.FSM.IsIn(g.Investigate)) timer = g.Arrived() ? $"bakınıyor, kalan {g.Investigate.LookTimer:0.0} sn" : "ses noktasına gidiyor";
        else if (g.FSM.IsIn(g.Search)) timer = g.Arrived() ? $"bakınıyor, kalan {g.Search.LookTimer:0.0} sn" : "son görülen noktaya gidiyor";
        else if (g.FSM.IsIn(g.Chase)) timer = $"son görülme {Time.time - g.lastSeenTime:0.0} sn önce";
        else if (g.FSM.IsIn(g.Patrol)) timer = $"hedef P{g.patrolIndex}";
        else if (g.FSM.IsIn(g.WaitAtDoor)) timer = "kapı kapalı, bekliyor";

        Vector3 d = g.player.position - transform.position;
        d.y = 0f;
        string col = ColorUtility.ToHtmlStringRGB(StateColor());

        var r = new Rect(12, 12, 330, 196);
        GUI.backgroundColor = new Color(0f, 0f, 0f, 0.72f);
        GUI.Box(r, GUIContent.none, box);
        GUI.backgroundColor = Color.white;
        GUILayout.BeginArea(new Rect(r.x + 12, r.y + 8, r.width - 24, r.height - 12));
        GUILayout.Label($"<b>Durum: <color=#{col}>{state}</color></b>", text);
        GUILayout.Label($"Önceki: {g.FSM.Previous?.Name ?? "-"}", text);
        GUILayout.Label(timer, text);
        GUILayout.Label($"Oyuncu uzaklığı: {d.magnitude:0.0} m", text);
        GUILayout.Label($"Son ses: {g.lastHearing}", text);
        GUILayout.Label("<size=14>WASD hareket · N ses · K kapı</size>", text);
        GUILayout.EndArea();
    }

    Color StateColor()
    {
        if (g.FSM.IsIn(g.Investigate)) return GuardInvestigate.Purple;
        if (g.FSM.IsIn(g.Chase)) return new Color(1f, 0.3f, 0.3f);
        if (g.FSM.IsIn(g.Search)) return Color.yellow;
        if (g.FSM.IsIn(g.WaitAtDoor)) return new Color(0.75f, 0.75f, 0.75f);
        return new Color(0.3f, 1f, 0.3f);
    }
}
