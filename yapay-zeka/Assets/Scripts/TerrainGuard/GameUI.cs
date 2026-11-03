using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Minimal IMGUI overlay: controls, alarm banner, guard states, "caught" screen.</summary>
public class GameUI : MonoBehaviour
{
    static bool caught;
    GUIStyle big, small, banner;
    GuardAI[] guards;

    public static void ShowCaught() { caught = true; }

    void Awake() { caught = false; }

    void Start() { guards = FindObjectsByType<GuardAI>(); }

    void Update()
    {
        if (InputHelper.RestartPressed())
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnGUI()
    {
        if (big == null)
        {
            big = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            banner = new GUIStyle(big) { fontSize = 30 };
            small = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            small.normal.textColor = Color.white;
        }

        GUI.Box(new Rect(10, 10, 330, 150), GUIContent.none);
        GUI.Label(new Rect(20, 15, 320, 140),
            "<b>Terrain & Gözcü</b>\n" +
            "WASD: hareket   Shift: koş   Ctrl: eğil\n" +
            "Sarı koni = devriye, turuncu = şüphe,\nkırmızı = kovalama\n" +
            "R: yeniden başla", new GUIStyle(small) { richText = true });

        float y = 170;
        if (guards != null)
            foreach (var g in guards)
            {
                if (g == null) continue;
                GUI.Label(new Rect(20, y, 320, 22), $"{g.name}: {StateTR(g.CurrentState)}  (fark etme %{Mathf.RoundToInt(g.Awareness * 100)})", small);
                y += 22;
            }

        if (AlarmSystem.IsActive && !caught)
        {
            bool blink = Mathf.FloorToInt(Time.time * 3f) % 2 == 0;
            banner.normal.textColor = blink ? Color.red : Color.white;
            GUI.Label(new Rect(0, 20, Screen.width, 50), $"!!! ALARM !!!  ({Mathf.CeilToInt(AlarmSystem.TimeLeft)})", banner);
        }

        if (caught)
        {
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            big.normal.textColor = Color.red;
            GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 60), "YAKALANDIN!", big);
            big.normal.textColor = Color.white;
            GUI.Label(new Rect(0, Screen.height / 2f + 10, Screen.width, 40), "Yeniden denemek için R'ye bas", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 22 });
        }
    }

    static string StateTR(GuardAI.State s) => s switch
    {
        GuardAI.State.Patrol => "Devriye",
        GuardAI.State.Suspicious => "Şüphelendi",
        GuardAI.State.Chase => "KOVALIYOR",
        GuardAI.State.Search => "Arıyor",
        _ => s.ToString()
    };
}
