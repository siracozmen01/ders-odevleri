// File: Assets/Scripts/Dev/ReportCapture.cs
// Report harness. Inactive unless the player is started with "-reportCapture".
// It applies the report's cost settings, logs what PathDebugDraw shows on screen, captures screenshots,
// tests the door / link, and times the NPC's run to the character with and without the mud slowdown (bonus).
//   Hafta6.exe -reportCapture -reportOut <folder> -screen-width 1000 -screen-height 900 -screen-fullscreen 0
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class ReportCapture : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-reportCapture") >= 0)
            new GameObject("ReportCapture").AddComponent<ReportCapture>();
    }

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    readonly StringBuilder log = new StringBuilder();
    string outDir;
    NavMeshAgent agent;
    AreaCostToggle toggle;
    PathDebugDraw debugDraw;
    NpcNavChase chase;
    MudSlowdown slow;
    JumpLinkTraverser jumper;
    SlidingDoor door;
    NavMeshLink link;
    Transform player;
    Vector3 start;

    static string Arg(string name)
    {
        string[] a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, name);
        return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
    }

    void W(string s)
    {
        log.AppendLine(s);
        Debug.Log("[Capture] " + s);
    }

    static string F(float v) => v.ToString("0.00", Inv);

    string Route() => agent.hasPath ? RouteClassifier.Letter(agent.path.corners) : "-";

    string ScreenText() =>
        $"route {Route()}, corners {debugDraw.CornerCount}, length {F(debugDraw.Length)} m, cost {F(debugDraw.Cost)}, status {agent.pathStatus}";

    IEnumerator Start()
    {
        outDir = Arg("-reportOut") ?? Path.Combine(Application.dataPath, "..", "capture");
        Directory.CreateDirectory(outDir);

        var npc = GameObject.Find("NPC");
        agent = npc.GetComponent<NavMeshAgent>();
        toggle = npc.GetComponent<AreaCostToggle>();
        debugDraw = npc.GetComponent<PathDebugDraw>();
        chase = npc.GetComponent<NpcNavChase>();
        slow = npc.GetComponent<MudSlowdown>();
        jumper = npc.GetComponent<JumpLinkTraverser>();
        door = FindAnyObjectByType<SlidingDoor>();
        link = FindAnyObjectByType<NavMeshLink>();
        player = GameObject.Find("Player").transform;
        start = npc.transform.position;
        Application.runInBackground = true;
        W($"agent on NavMesh at start: {agent.isOnNavMesh}");
        W($"Unity {Application.unityVersion}, window {UnityEngine.Screen.width}x{UnityEngine.Screen.height}");
        W($"NPC start ({F(start.x)}, {F(start.z)}), character ({F(player.position.x)}, {F(player.position.z)})");

        yield return new WaitForSeconds(0.5f);

        // 1. The cost settings, NPC speed 0 (as in the lab: the screen then shows the whole path)
        W("");
        W("== 1. Cost settings (speed 0) ==");
        float normalSpeed = agent.speed;
        slow.slowdownOn = false;
        agent.speed = 0f;
        for (int i = 0; i < toggle.presets.Length; i++)
        {
            yield return ResetNpc(i);
            var p = toggle.presets[i];
            W($"Ayar {i + 1} (Camur {F(p.mud)}, SigSu {F(p.water)}, Jump {F(p.jump)}): {ScreenText()}");
            yield return Shot($"ayar{i + 1}.png");
        }

        // 2. Moving obstacle: Ayar 4 uses the door corridor (D). Close the door and watch the path.
        W("");
        W("== 2. Door test (Ayar 4, speed 0) ==");
        yield return ResetNpc(3);
        W($"door open: {ScreenText()}");
        door.Toggle();
        float closedAt = Time.time;
        string before = Route();
        float changedAt = -1f;
        while (Time.time - closedAt < 4f)
        {
            if (changedAt < 0f && !agent.pathPending && Route() != before) changedAt = Time.time - closedAt;
            yield return null;
        }
        W($"door closed (K): route changed {before} -> {Route()} {F(changedAt)} s after the key press " +
          $"(door rises 2.1 m at 2 m/s = 1.05 s, then Time To Stationary 0.5 s); {ScreenText()}");
        yield return Shot("kapi-kapali.png");
        door.Toggle();
        yield return new WaitForSeconds(2.5f);
        W($"door opened again: {ScreenText()}");

        // 3. Link off (J) in Ayar 1
        W("");
        W("== 3. Link test (Ayar 1, speed 0) ==");
        yield return ResetNpc(0);
        W($"link on: {ScreenText()}");
        link.activated = false;
        yield return new WaitForSeconds(1f);
        W($"link off (J): {ScreenText()}");
        yield return Shot("baglanti-kapali.png");
        link.activated = true;
        yield return new WaitForSeconds(0.5f);

        // 4. Overview: all four routes drawn and lettered
        W("");
        W("== 4. Overview ==");
        yield return Overview();

        // 5. Real runs at normal speed
        W("");
        W("== 5. Runs to the character (speed 3.5 m/s, reach = horizontal distance < 1 m) ==");
        string[] titles = { "Ayar 1 (B, jump)", "Ayar 3 (C, water)", "Ayar 4 (D, door)" };
        int[] runPresets = { 0, 2, 3 };
        for (int r = 0; r < runPresets.Length; r++)
            yield return Run(runPresets[r], normalSpeed, false, titles[r]);

        W("");
        W("== 6. Bonus: Ayar 2 (route A through mud), slowdown off vs on ==");
        for (int rep = 0; rep < 2; rep++)
        {
            yield return Run(1, normalSpeed, false, $"Ayar 2, slowdown OFF, run {rep + 1}");
            yield return Run(1, normalSpeed, true, $"Ayar 2, slowdown ON,  run {rep + 1}");
        }

        File.WriteAllText(Path.Combine(outDir, "oyun-olcumleri.txt"), log.ToString(), new UTF8Encoding(false));
        yield return new WaitForSeconds(0.2f);
        Application.Quit();
    }

    // Bigger labels so the on-screen numbers stay readable in the report images
    void OnGUI() => GUI.skin.label.fontSize = 16;

    // Warp the NPC home, apply a setting and wait until the new path is in
    IEnumerator ResetNpc(int preset)
    {
        agent.speed = 0f;
        agent.Warp(start);
        toggle.ApplyPreset(preset);
        yield return new WaitForSeconds(1f);   // NpcNavChase repaths every 0.25 s
    }

    IEnumerator Shot(string file)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(outDir, file));
        yield return new WaitForSeconds(0.4f);
    }

    IEnumerator Run(int preset, float speed, bool slowdown, string title)
    {
        slow.slowdownOn = false;
        yield return ResetNpc(preset);
        string route = Route();
        float planned = debugDraw.Length;
        slow.slowdownOn = slowdown;
        agent.speed = speed;
        float t0 = Time.time, mudTime = 0f, maxSpeedSeen = 0f;
        bool jumped = false;
        float travelled = 0f;
        Vector3 last = agent.transform.position;
        while (true)
        {
            yield return null;
            Vector3 d = player.position - agent.transform.position;
            d.y = 0f;
            Vector3 step = agent.transform.position - last;
            step.y = 0f;
            travelled += step.magnitude;
            last = agent.transform.position;
            if (slow.InMud) mudTime += Time.deltaTime;
            if (jumper.IsJumping) jumped = true;
            maxSpeedSeen = Mathf.Max(maxSpeedSeen, agent.velocity.magnitude);
            if (d.magnitude < 1f || Time.time - t0 > 40f) break;
        }
        float t = Time.time - t0;
        W($"{title}: planned route {route} ({F(planned)} m), reached in {F(t)} s, walked {F(travelled)} m, " +
          $"time in mud {F(mudTime)} s, jumped {jumped}");
        slow.slowdownOn = false;
        agent.speed = 0f;
    }

    // Draw every route in its own colour (paths from NavMesh.CalculatePath with a filter that forces the route)
    IEnumerator Overview()
    {
        chase.enabled = false;            // hides the yellow line
        toggle.enabled = false;           // hide the on-screen panels for a clean picture
        debugDraw.enabled = false;
        slow.enabled = false;
        agent.Warp(start);

        int mud = NavMesh.GetAreaFromName("Camur"), water = NavMesh.GetAreaFromName("SigSu"), jump = NavMesh.GetAreaFromName("Jump");
        (string letter, float m, float s, float j, Color c)[] routes =
        {
            ("A", 1f, 50f, 50f, new Color(0.55f, 0.27f, 0.07f)),
            ("B", 50f, 50f, 1f, new Color(0.62f, 0.20f, 0.85f)),
            ("C", 50f, 1f, 50f, new Color(0.00f, 0.80f, 0.90f)),
            ("D", 50f, 50f, 50f, new Color(1.00f, 0.50f, 0.00f)),
        };
        var lineMat = agent.GetComponent<LineRenderer>().sharedMaterial;
        var lines = new GameObject("OverviewRoutes");
        foreach (var r in routes)
        {
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = NavMesh.AllAreas };
            filter.SetAreaCost(mud, r.m);
            filter.SetAreaCost(water, r.s);
            filter.SetAreaCost(jump, r.j);
            var path = new NavMeshPath();
            NavMesh.CalculatePath(start - Vector3.up * agent.baseOffset, player.position - Vector3.up, filter, path);
            var go = new GameObject("Route_" + r.letter);
            go.transform.SetParent(lines.transform);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = new Material(lineMat) { color = r.c };
            lr.widthMultiplier = 0.25f;
            lr.numCornerVertices = 2;
            lr.positionCount = path.corners.Length;
            for (int i = 0; i < path.corners.Length; i++)
                lr.SetPosition(i, path.corners[i] + Vector3.up * (r.letter == "B" ? 1.3f : 0.3f));
            float len = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++) len += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            W($"route {r.letter}: forced path is {RouteClassifier.Letter(path.corners)}, {path.corners.Length} corners, {F(len)} m");
        }
        yield return new WaitForSeconds(0.3f);
        yield return Shot("genel-rotalar.png");
        Destroy(lines);
        chase.enabled = true;
        toggle.enabled = true;
        debugDraw.enabled = true;
        slow.enabled = true;
        yield return new WaitForSeconds(0.5f);
    }
}
