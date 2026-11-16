using UnityEngine;

/// <summary>
/// Week 3 assignment "Gözcü": a stationary guard that sees the player.
/// The field names and the two test methods follow the teacher's skeleton (section 7.2).
///
/// Seen = inside the vision cone (range + angle, own dot-product test)
///        AND the line of sight from the eye to the player is not blocked (Physics.Linecast).
///
/// Bonus tasks (section 7.3), each can be switched on/off in the Inspector or with keys 1/2/3:
///   scanEnabled         - Tarama: sweep +-60 degrees around the start direction at 30 deg/s.
///   heightLimitEnabled  - Yükseklik tuzağı: separate horizontal (90) and vertical (+-30) limits.
///   chaseEnabled        - Takip: turn towards the player, go back to scanning after 2 s unseen.
/// </summary>
public class Gozcu : MonoBehaviour
{
    public enum Side { Ahead = 0, Right = 1, Left = -1 }
    public enum Mode { Watching, Scanning, Chasing }

    [SerializeField] Transform oyuncu;
    [SerializeField] float menzil = 15f;
    [SerializeField] float gorusAcisi = 90f;      // degrees, the whole cone
    [SerializeField] float gozYuksekligi = 0.6f;  // up from the pivot
    [SerializeField] LayerMask engeller = 1;      // Default: terrain and trees (NOT Player)

    [Header("Bonus: Tarama / scan (+4)")]
    [SerializeField] bool scanEnabled = false;
    [SerializeField] float scanHalfAngle = 60f;   // +-60 degrees around the start direction
    [SerializeField] float scanSpeed = 30f;       // degrees per second

    [Header("Bonus: Yükseklik tuzağı / height trap (+3)")]
    [SerializeField] bool heightLimitEnabled = false;
    [SerializeField] float verticalHalfAngle = 30f; // +-30 degrees above/below the horizon

    [Header("Bonus: Takip / chase (+3)")]
    [SerializeField] bool chaseEnabled = false;
    [SerializeField] float chaseTurnSpeed = 90f;  // degrees per second
    [SerializeField] float loseSightTime = 2f;    // seconds without seeing before scanning again

    [Header("Console output")]
    [SerializeField] float sideLogInterval = 0.5f;   // at most one right/left change message per interval
    [SerializeField] bool verifyWithVectorAngle = true;

    Renderer govde;
    Quaternion startRotation;
    int scanDirection = 1;
    float lastSeenTime = float.NegativeInfinity;
    Vector3 lastSeenPosition;

    bool loggedSeeing;
    Side loggedSide;
    float lastLogTime = float.NegativeInfinity;
    float lastVerifyWarning = float.NegativeInfinity;

    // ---- read-only state for the HUD, the cone view and tests ----
    public bool IsSeeing { get; private set; }
    public bool PlayerInCone { get; private set; }
    public Side PlayerSide { get; private set; }
    public Mode CurrentMode { get; private set; } = Mode.Watching;
    public float AngleToPlayer { get; private set; }   // from the dot product, degrees
    public Transform Oyuncu => oyuncu;
    public float Menzil => menzil;
    public float GorusAcisi => gorusAcisi;
    public LayerMask Engeller => engeller;
    public Vector3 Goz => transform.position + Vector3.up * gozYuksekligi;

    public bool ScanEnabled { get => scanEnabled; set => scanEnabled = value; }
    public bool HeightLimitEnabled { get => heightLimitEnabled; set => heightLimitEnabled = value; }
    public bool ChaseEnabled { get => chaseEnabled; set => chaseEnabled = value; }

    void Awake()
    {
        govde = GetComponent<Renderer>();
        startRotation = transform.rotation;
    }

    void Update()
    {
        if (oyuncu == null) return;

        Vector3 goz = Goz;
        PlayerInCone = GorusKonisinde(goz, oyuncu.position);
        bool goruyor = PlayerInCone && GorusHattiAcik(goz, oyuncu.position);
        IsSeeing = goruyor;
        govde.material.color = goruyor ? Color.red : Color.green;

        PlayerSide = SideOf(transform.forward, oyuncu.position - goz);
        AngleToPlayer = HorizontalAngleTo(oyuncu.position - goz);
        LogSide(goruyor);

        if (goruyor)
        {
            lastSeenTime = Time.time;
            lastSeenPosition = oyuncu.position;
        }
        Turn(Time.deltaTime);
    }

    // ------------------------------------------------------------------
    // Vision cone: range + angle, written with the dot product (week 1).
    // ------------------------------------------------------------------
    public bool GorusKonisinde(Vector3 goz, Vector3 hedef)
    {
        Vector3 fark = hedef - goz;
        float mesafeKare = fark.sqrMagnitude;
        if (mesafeKare > menzil * menzil) return false;   // out of range (compare squares: no sqrt)
        if (mesafeKare < 1e-8f) return true;              // standing on the eye

        float cosYariAci = Mathf.Cos(gorusAcisi * 0.5f * Mathf.Deg2Rad);
        bool icinde;

        if (!heightLimitEnabled)
        {
            // One 3D cone: forward . dir = cos(alpha) >= cos(angle / 2)
            Vector3 yon = fark / Mathf.Sqrt(mesafeKare);
            icinde = Vector3.Dot(transform.forward, yon) >= cosYariAci;
        }
        else
        {
            // Height trap: horizontal and vertical angles are limited separately.
            Vector3 ileriYatay = Flat(transform.forward).normalized;
            Vector3 yatay = Flat(fark);
            float yatayBoy = yatay.magnitude;
            bool yatayIcinde = yatayBoy < 1e-4f || Vector3.Dot(ileriYatay, yatay / yatayBoy) >= cosYariAci;

            // Elevation e: sin(e) = dir . up. |e| <= verticalHalfAngle  <=>  |sin e| <= sin(limit)
            float sinYukseklik = Vector3.Dot(fark / Mathf.Sqrt(mesafeKare), Vector3.up);
            bool dikeyIcinde = Mathf.Abs(sinYukseklik) <= Mathf.Sin(verticalHalfAngle * Mathf.Deg2Rad);
            icinde = yatayIcinde && dikeyIcinde;
        }

        if (verifyWithVectorAngle) VerifyWithVectorAngle(fark, icinde);
        return icinde;
    }

    // ------------------------------------------------------------------
    // Line of sight: a segment from the eye to the target must not hit an obstacle.
    // ------------------------------------------------------------------
    public bool GorusHattiAcik(Vector3 goz, Vector3 hedef)
    {
        return !Physics.Linecast(goz, hedef, engeller, QueryTriggerInteraction.Ignore);
    }

    /// <summary>Right/left with the cross product (left-handed Unity): Cross(forward, toTarget).y &gt; 0 means right.</summary>
    public static Side SideOf(Vector3 forward, Vector3 toTarget)
    {
        float crossY = Vector3.Cross(forward, toTarget).y;
        float scale = Flat(forward).magnitude * Flat(toTarget).magnitude;
        // |crossY| = |f||d| sin(theta): treat less than ~2 degrees as straight ahead.
        if (scale < 1e-6f || Mathf.Abs(crossY) < 0.035f * scale) return Side.Ahead;
        return crossY > 0f ? Side.Right : Side.Left;
    }

    float HorizontalAngleTo(Vector3 toTarget)
    {
        Vector3 a = Flat(transform.forward).normalized, b = Flat(toTarget).normalized;
        return Mathf.Acos(Mathf.Clamp(Vector3.Dot(a, b), -1f, 1f)) * Mathf.Rad2Deg;
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // Vector3.Angle is used ONLY here, to check our own dot-product result.
    void VerifyWithVectorAngle(Vector3 fark, bool icinde)
    {
        bool check;
        float margin;
        if (!heightLimitEnabled)
        {
            float a = Vector3.Angle(transform.forward, fark);
            check = a <= gorusAcisi * 0.5f;
            margin = Mathf.Abs(a - gorusAcisi * 0.5f);
        }
        else
        {
            float h = Vector3.Angle(Flat(transform.forward), Flat(fark));
            float e = 90f - Vector3.Angle(Vector3.up, fark);
            check = h <= gorusAcisi * 0.5f && Mathf.Abs(e) <= verticalHalfAngle;
            margin = Mathf.Min(Mathf.Abs(h - gorusAcisi * 0.5f), Mathf.Abs(Mathf.Abs(e) - verticalHalfAngle));
        }
        // Exactly on the edge both answers are fine; only report a real disagreement.
        if (check != icinde && margin > 0.05f && Time.time - lastVerifyWarning > 1f)
        {
            lastVerifyWarning = Time.time;
            Debug.LogWarning($"[{name}] Dot-product cone test disagrees with Vector3.Angle (margin {margin:F2} deg)");
        }
    }

    // ------------------------------------------------------------------
    // Console: right/left only when something changes (no spam).
    // ------------------------------------------------------------------
    void LogSide(bool goruyor)
    {
        if (goruyor && !loggedSeeing)
        {
            loggedSeeing = true;
            loggedSide = PlayerSide;
            lastLogTime = Time.time;
            Debug.Log($"[{name}] Player SEEN - {SideText(PlayerSide)}");
        }
        else if (goruyor && PlayerSide != loggedSide && Time.time - lastLogTime >= sideLogInterval)
        {
            loggedSide = PlayerSide;
            lastLogTime = Time.time;
            Debug.Log($"[{name}] Player now {SideText(PlayerSide)}");
        }
        else if (!goruyor && loggedSeeing)
        {
            loggedSeeing = false;
            Debug.Log($"[{name}] Player lost");
        }
    }

    public static string SideText(Side side) => side switch
    {
        Side.Right => "on the RIGHT of the guard (sağında)",
        Side.Left => "on the LEFT of the guard (solunda)",
        _ => "straight AHEAD (tam önünde)",
    };

    // ------------------------------------------------------------------
    // Bonus behaviour: scan / chase / return.
    // ------------------------------------------------------------------
    void Turn(float dt)
    {
        bool chasing = chaseEnabled && Time.time - lastSeenTime < loseSightTime;

        if (chasing)
        {
            CurrentMode = Mode.Chasing;
            Vector3 toTarget = Flat(lastSeenPosition - transform.position);
            if (toTarget.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(toTarget), chaseTurnSpeed * dt);
        }
        else if (scanEnabled)
        {
            CurrentMode = Mode.Scanning;
            Quaternion scanTarget = startRotation * Quaternion.AngleAxis(scanDirection * scanHalfAngle, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, scanTarget, scanSpeed * dt);
            if (Quaternion.Angle(transform.rotation, scanTarget) < 0.01f) scanDirection = -scanDirection;
        }
        else
        {
            CurrentMode = Mode.Watching;
            // Base behaviour: stand still. After a chase, turn back to the original direction.
            transform.rotation = Quaternion.RotateTowards(transform.rotation, startRotation, scanSpeed * dt);
        }
    }

    // ------------------------------------------------------------------
    // Gizmos (Scene view): range sphere and the two cone edges.
    // ------------------------------------------------------------------
    void OnDrawGizmos()
    {
        Vector3 goz = transform.position + Vector3.up * gozYuksekligi;

        Gizmos.color = Application.isPlaying && IsSeeing ? Color.red : Color.green;
        Gizmos.DrawWireSphere(goz, menzil);

        Vector3 solKenar = Quaternion.AngleAxis(-gorusAcisi / 2f, Vector3.up) * transform.forward;
        Vector3 sagKenar = Quaternion.AngleAxis(gorusAcisi / 2f, Vector3.up) * transform.forward;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(goz, goz + solKenar * menzil);
        Gizmos.DrawLine(goz, goz + sagKenar * menzil);

        if (heightLimitEnabled)
        {
            // Vertical limits of the height trap: rotate forward around the guard's right axis.
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Vector3 yukari = Quaternion.AngleAxis(-verticalHalfAngle, transform.right) * transform.forward;
            Vector3 asagi = Quaternion.AngleAxis(verticalHalfAngle, transform.right) * transform.forward;
            Gizmos.DrawLine(goz, goz + yukari * menzil);
            Gizmos.DrawLine(goz, goz + asagi * menzil);
        }

        if (Application.isPlaying && oyuncu != null)
        {
            Gizmos.color = IsSeeing ? Color.red : (PlayerInCone ? Color.gray : Color.clear);
            Gizmos.DrawLine(goz, oyuncu.position);
        }
    }
}
