// File: Assets/Scripts/Dev/Hafta7Capture.cs
// Test harness for the report. Inactive unless the player is started with "-reportCapture".
// It plays one scripted run that exercises every homework rule, saves the console log and screenshots.
// N and K are pressed through a virtual keyboard, so the real Input Actions (MakeNoise, ToggleDoor) are used.
// The player is moved by teleporting (instead of WASD) so the run is repeatable.
//   Hafta7.app/Contents/MacOS/<exe> -reportCapture -reportOut <folder> -screen-width 1200 -screen-height 900 -screen-fullscreen 0
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class Hafta7Capture : MonoBehaviour
{
    static readonly StringBuilder log = new StringBuilder();
    static bool active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Listen()
    {
        active = Array.IndexOf(Environment.GetCommandLineArgs(), "-reportCapture") >= 0;
        if (active) Application.logMessageReceived += OnLog;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-reportCapture") >= 0)
            new GameObject("Hafta7Capture").AddComponent<Hafta7Capture>();
    }

    string outDir;
    Guard g;
    Transform player;
    CharacterController cc;
    SlidingDoor door;
    Keyboard kb;
    int passed, failed;

    static string Arg(string name)
    {
        string[] a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, name);
        return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
    }

    static void OnLog(string msg, string stack, LogType type)
    {
        string prefix = type == LogType.Log ? "" : $"<{type}> ";
        log.AppendLine(prefix + msg);
    }

    void T(string s) => Debug.Log($"[Test {Time.time:0.0} sn] {s}");

    void Check(string what, bool ok)
    {
        if (ok) passed++; else failed++;
        T($"{(ok ? "TAMAM" : "HATA")}: {what}");
    }

    IEnumerator Start()
    {
        outDir = Arg("-reportOut") ?? Path.Combine(Application.dataPath, "..", "capture");
        Directory.CreateDirectory(outDir);
        Application.runInBackground = true;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        kb = InputSystem.AddDevice<Keyboard>("TestKlavyesi");

        g = FindAnyObjectByType<Guard>();
        player = g.player;
        cc = player.GetComponent<CharacterController>();
        door = g.door;
        T($"Unity {Application.unityVersion}, pencere {Screen.width}x{Screen.height}");
        T($"Ayarlar: duyma {g.hearingDistance} m, araştırma bakınma {g.investigateTime} sn, hız {g.investigateSpeed} m/sn, " +
          $"gözden kaybetme {g.loseTime} sn, arama bakınma {g.searchTime} sn");

        yield return new WaitForSeconds(1f);

        // 1. A noise farther than 8 m is not heard
        T("1) Uzak ses: oyuncu bekçiden 8 m'den uzakta N'ye basıyor");
        Teleport(HidePoint());
        yield return Press(Key.N);
        yield return new WaitForSeconds(0.3f);
        Check("8 m'den uzak ses duyulmadı, bekçi Devriye'de kaldı", g.FSM.IsIn(g.Patrol));

        // 2. Patrol -> Investigate: noise behind the U's top arm while the guard walks over it
        T("2) Devriye'de yakın ses: bekçi U'nun üst kolunun kuzeyinde batıya yürürken oyuncu U'nun içinde N'ye basıyor");
        yield return WaitFor(() => g.FSM.IsIn(g.Patrol) && g.patrolIndex == 0 &&
                                   g.transform.position.z > 15.2f && g.transform.position.x < 12f && g.transform.position.x > 8f,
                             40f, "bekçi üst kolun kuzeyinde");
        Vector3 n1 = new Vector3(9.5f, 0f, 11.5f);
        T($"Bekçi ({g.transform.position.x:0.0}; {g.transform.position.z:0.0}), ses noktası ({n1.x:0.0}; {n1.z:0.0}), " +
          $"arada duvar: {WallBetween(n1)}, bekçi oyuncuyu görüyor mu: {SeesPoint(n1)}");
        Teleport(n1);
        yield return Press(Key.N);
        Teleport(HidePoint());
        Check("Devriye → Araştır", g.FSM.IsIn(g.Investigate) && g.FSM.Previous == g.Patrol);
        yield return new WaitForSeconds(0.7f);
        yield return Shot("01-devriye-arastir.png");
        yield return WaitFor(() => g.FSM.IsIn(g.Investigate) && g.Arrived() && g.Investigate.LookTimer < g.investigateTime - 1.3f,
                             15f, "ses noktasında bakınıyor");
        yield return Shot("02-arastir-bakinma.png");
        float lookStart = Time.time - (g.investigateTime - g.Investigate.LookTimer);
        yield return WaitFor(() => g.FSM.IsIn(g.Patrol), 6f, "Devriye'ye dönüş");
        Check($"3 sn bakındıktan sonra Araştır → Devriye (bakınma {Time.time - lookStart:0.0} sn)", g.FSM.IsIn(g.Patrol) && g.FSM.Previous == g.Investigate);

        // 3. Chase ignores the noise
        yield return new WaitForSeconds(1.5f);
        T("3) Kovala'da ses: oyuncu bekçinin önüne çıkıyor, sonra N'ye basıyor");
        Teleport(FrontPoint());
        yield return WaitFor(() => g.FSM.IsIn(g.Chase), 2f, "Kovala");
        yield return new WaitForSeconds(0.5f);
        yield return Press(Key.N);
        yield return null;
        yield return Shot("04-kovala-ses-yok.png");
        Check("Kovala'da ses görmezden gelindi", g.FSM.IsIn(g.Chase));
        Teleport(HidePoint());
        yield return WaitFor(() => g.FSM.IsIn(g.Search), 3f, "Ara");
        Check("Oyuncu 1 sn görünmeyince Kovala → Ara", g.FSM.IsIn(g.Search));

        // 4. Search -> Investigate, then a second noise while investigating (new target, no transition)
        yield return new WaitForSeconds(0.8f);
        T("4) Ara'da ses: oyuncu bekçinin arkasında, görüş konisinin dışında N'ye basıyor");
        Vector3 n2 = BehindPoint(5f);
        T($"Ses noktası ({n2.x:0.0}; {n2.z:0.0}), uzaklık {Flat(n2 - g.transform.position):0.0} m, görüyor mu: {SeesPoint(n2)}");
        Teleport(n2);
        yield return Press(Key.N);
        Teleport(HidePoint());
        Check("Ara → Araştır", g.FSM.IsIn(g.Investigate) && g.FSM.Previous == g.Search);
        yield return new WaitForSeconds(0.6f);
        yield return Shot("05-ara-arastir.png");
        Vector3 n3 = BehindPoint(4f);
        T($"Araştır'dayken ikinci ses ({n3.x:0.0}; {n3.z:0.0})");
        Teleport(n3);
        yield return Press(Key.N);
        Teleport(HidePoint());
        Check("Araştır'da yeni ses: durum değişmedi, hedef yenilendi",
              g.FSM.IsIn(g.Investigate) && Flat(g.noisePosition - new Vector3(n3.x, 0f, n3.z)) < 0.01f);

        // 5. Investigate -> Chase through the global rule (no code in GuardInvestigate for this)
        yield return new WaitForSeconds(0.8f);
        T("5) Araştır'da oyuncu görünüyor: global kural");
        Teleport(FrontPoint());
        yield return WaitFor(() => g.FSM.IsIn(g.Chase), 2f, "Kovala");
        Check("Araştır → Kovala (global kural)", g.FSM.IsIn(g.Chase) && g.FSM.Previous == g.Investigate);
        Check($"Araştır.Exit hızı geri aldı, Kovala.Enter 4 m/sn yaptı (hız {g.Agent.speed:0.0})", Mathf.Approximately(g.Agent.speed, g.chaseSpeed));
        yield return new WaitForSeconds(0.25f);
        yield return Shot("03-arastir-kovala.png");
        Teleport(HidePoint());
        yield return WaitFor(() => g.FSM.IsIn(g.Patrol), 25f, "Ara bitti, Devriye");
        Check($"Devriye hızı {g.Agent.speed:0.0} m/sn", Mathf.Approximately(g.Agent.speed, g.patrolSpeed));

        // 6. Investigate -> WaitAtDoor -> back to Investigate (previous state)
        T("6) Araştır'da kapı: bekçi P1'de, ses U'nun sırt duvarının doğusunda; bekçi kapıya yaklaşınca K");
        yield return WaitFor(() => g.FSM.IsIn(g.Patrol) && g.patrolIndex == 2 &&
                                   Flat(g.transform.position - g.patrolPoints[1].position) < 1.2f, 60f, "bekçi P1'de");
        Vector3 n4 = new Vector3(15.5f, 0f, 7f);
        T($"Ses noktası ({n4.x:0.0}; {n4.z:0.0}), uzaklık {Flat(n4 - g.transform.position):0.0} m, arada duvar: {WallBetween(n4)}");
        Teleport(n4);
        yield return Press(Key.N);
        Teleport(HidePoint());
        Check("Devriye → Araştır (kapının öbür yanındaki ses)", g.FSM.IsIn(g.Investigate));
        yield return WaitFor(() => Flat(door.transform.position - g.transform.position) < 2.3f, 6f, "kapıya 2,3 m");
        T($"Bekçi kapıya {Flat(door.transform.position - g.transform.position):0.0} m uzakta: K");
        yield return Press(Key.K);
        yield return null;
        Check("Araştır → KapıdaBekle", g.FSM.IsIn(g.WaitAtDoor) && g.FSM.Previous == g.Investigate);
        yield return new WaitForSeconds(2.5f);
        Check("Kapıda beklerken ajan durdu (isStopped)", g.Agent.isStopped && g.Agent.velocity.magnitude < 0.05f);
        yield return Shot("06-arastir-kapida-bekle.png");
        yield return Press(Key.K);
        yield return null;
        Check("Kapı açıldı: KapıdaBekle → Araştır (önceki durum)", g.FSM.IsIn(g.Investigate) && !g.Agent.isStopped);
        yield return new WaitForSeconds(1.2f);
        yield return Shot("07-kapidan-sonra-arastir.png");
        yield return WaitFor(() => g.FSM.IsIn(g.Patrol), 15f, "Devriye");
        Check("Ses noktasında bakındı, Araştır → Devriye", g.FSM.IsIn(g.Patrol) && g.FSM.Previous == g.Investigate);

        T($"Bitti: {passed} kontrol tamam, {failed} hata");
        yield return new WaitForSeconds(0.5f);
        File.WriteAllText(Path.Combine(outDir, "konsol-gunlugu.txt"), log.ToString(), new UTF8Encoding(false));
        Application.Quit();
    }

    // ---- helpers ----

    static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

    void Teleport(Vector3 p)
    {
        cc.enabled = false;
        player.position = new Vector3(p.x, 1f, p.z);
        cc.enabled = true;
        Physics.SyncTransforms();
    }

    IEnumerator Press(Key key)
    {
        InputSystem.QueueStateEvent(kb, new KeyboardState(key));
        yield return null;   // processed in the next frame's input update: the action fires in that Update
        InputSystem.QueueStateEvent(kb, new KeyboardState());
        yield return null;
    }

    IEnumerator WaitFor(Func<bool> cond, float timeout, string what)
    {
        float t0 = Time.time;
        while (!cond())
        {
            if (Time.time - t0 > timeout) { T($"ZAMAN AŞIMI: {what}"); failed++; yield break; }
            yield return null;
        }
    }

    // The screenshot is written at the end of the frame, so wait a frame before moving the player again
    IEnumerator Shot(string file)
    {
        ScreenCapture.CaptureScreenshot(Path.Combine(outDir, file));
        T($"Ekran görüntüsü: {file} (durum {g.FSM.Current.Name})");
        yield return new WaitForEndOfFrame();
        yield return null;
    }

    bool WallBetween(Vector3 p)
    {
        Vector3 eye = g.transform.position;
        Vector3 to = new Vector3(p.x, 1.5f, p.z) - eye;
        return Physics.Raycast(eye, to.normalized, out RaycastHit hit, to.magnitude) && !hit.transform.IsChildOf(player);
    }

    // Same test as Guard.CanSeePlayer, for a point where the player is not standing yet
    bool SeesPoint(Vector3 p)
    {
        Vector3 to = new Vector3(p.x, 1.5f, p.z) - g.transform.position;
        return to.magnitude <= g.viewDistance && Vector3.Angle(g.transform.forward, to) <= g.viewAngle / 2f && !WallBetween(p);
    }

    // The south-west corner is more than 10 m from every patrol point and from the patrol path, so the guard
    // cannot see it while patrolling. Only if the guard is near it, use the corner farthest from the guard.
    Vector3 HidePoint()
    {
        Vector3[] corners = { new(1.5f, 0f, 1.5f), new(18.5f, 0f, 1.5f), new(18.5f, 0f, 18.5f), new(1.5f, 0f, 18.5f) };
        if (Flat(corners[0] - g.transform.position) > g.viewDistance + 1f) return corners[0];
        Vector3 best = corners[0];
        foreach (var c in corners)
            if (Flat(c - g.transform.position) > Flat(best - g.transform.position)) best = c;
        return best;
    }

    // A walkable point in front of the guard with a clear line of sight
    Vector3 FrontPoint()
    {
        foreach (float r in new[] { 3f, 2.5f, 2f, 3.5f })
            foreach (float a in new[] { 0f, -20f, 20f, -35f, 35f })
            {
                Vector3 p = g.transform.position + Quaternion.Euler(0f, a, 0f) * g.transform.forward * r;
                if (NavMesh.SamplePosition(p, out NavMeshHit hit, 0.3f, NavMesh.AllAreas) && SeesPoint(hit.position))
                    return hit.position;
            }
        return g.transform.position + g.transform.forward * 2f;
    }

    // A walkable point behind the guard (outside the view cone), within hearing distance
    Vector3 BehindPoint(float r)
    {
        foreach (float rr in new[] { r, r - 1f, r + 1f, r - 2f })
            foreach (float a in new[] { 180f, 150f, 210f, 120f, 240f })
            {
                Vector3 p = g.transform.position + Quaternion.Euler(0f, a, 0f) * g.transform.forward * rr;
                if (NavMesh.SamplePosition(p, out NavMeshHit hit, 0.3f, NavMesh.AllAreas) && !SeesPoint(hit.position)
                    && Flat(hit.position - g.transform.position) < g.hearingDistance - 0.5f)
                    return hit.position;
            }
        return g.transform.position - g.transform.forward * 3f;
    }
}
