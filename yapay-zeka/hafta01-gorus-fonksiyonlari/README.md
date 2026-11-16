# Week 1 - Vision and side functions (Digital Game AI)

Course: *Dijital Oyunlarda Yapay Zekâ*, week 1 "YZ'nin Matematiği" (Dr. Öğr. Üyesi Can Meşe).

## Assignment

Teacher's requirement (slide 26, "ÖDEV - Görüş ve yön fonksiyonları"), in my own words:

- Write `bool IsInFov(Vector2D agent, Vector2D heading, Vector2D target, double fovDeg, double range)`: true when the target is within range **and** inside the view cone.
- Write `int SideOf(Vector2D agent, Vector2D heading, Vector2D target)`: `+1` left, `-1` right, `0` on the same line.
- Do it **without square root and without acos**: squared distance and a cosine threshold.
- At least 6 test cases, including boundary angles, with results printed to the console.
- Use my own `Vector2D` struct (it moves to Unity in week 3).

The lab part (slide 25) is solved in code as well: the five "solve by hand, then verify" questions.

## How to run

Requires the .NET SDK (built and tested with .NET 10.0.100).

```
cd yapay-zeka/hafta01-gorus-fonksiyonlari
dotnet run
```

The program prints the lab answers and every test with `PASS`/`FAIL`, and exits with code 0 only if all checks pass. There are no controls; it is a console program.

## Files

| File | Contents |
|---|---|
| `Vector2D.cs` | Own 2D vector struct: `Length`, `LengthSq`, `Dot`, `Cross`, `Normalized` (zero-safe), `+ - * /`. No `System.Numerics`, no Unity types. |
| `FovFunctions.cs` | Homework: `IsInFov`, `SideOf`, plus `CosHalfAngle` and an `IsInFov` overload that takes a precomputed cosine. |
| `LabQuestions.cs` | The 5 lab questions of slide 25, checked against the answers in the speaker notes. |
| `FovTests.cs` | 20 `IsInFov` cases, 9 `SideOf` cases and a randomized cross-check, printed as PASS/FAIL. |
| `Program.cs` | Entry point. |
| `Hafta01.csproj` | Project file (`net10.0`, `LangVersion 9.0` so the code also compiles in Unity 6). |

## Requirement to code mapping

| Requirement | Where |
|---|---|
| `IsInFov(agent, heading, target, fovDeg, range)` | `FovFunctions.IsInFov` |
| `SideOf(agent, heading, target)` (+1 / -1 / 0) | `FovFunctions.SideOf` |
| No sqrt, no acos: squared distance | `distSq > rangeSq` in `FovFunctions.IsInFov` |
| No sqrt, no acos: cos threshold | `FovFunctions.IsInFov` (see "How the angle test works" below) |
| At least 6 tests incl. boundary angles | `FovTests.Run` (29 cases + 100,000 random cross-checks) |
| Own `Vector2D` | `Vector2D.cs` |
| Lab questions (slide 25) | `LabQuestions.Run` |

## How the angle test works (no sqrt, no acos)

The target is in the cone when the angle between the heading `h` and the direction `d = target - agent` is at most half the field of view:

```
cos(angle) = (h . d) / (|h| |d|)  >=  c = cos(fovDeg / 2)
```

`|h|` and `|d|` need square roots, so the inequality is squared without losing the sign. With
`f(x) = x * |x|` (strictly increasing, and `f(k x) = k^2 f(x)` for `k > 0`) it becomes

```
f(h . d)  >=  f(c) * |h|^2 * |d|^2
```

which uses only `LengthSq` and `Dot`. One comparison covers every case: narrow cones, a 180° cone
(`c = 0`), and cones wider than 180° (`c < 0`). The heading does not need to be normalized.
`Math.Cos` is only used to get `c`; in a game it can be computed once per NPC type
(`FovFunctions.CosHalfAngle`) and passed to the overload.

Boundaries are inclusive (a target exactly on the cone edge or exactly at max range is seen). A small
relative tolerance (`1e-9`) makes this hold despite floating-point round-off; without it, the exact
45° target `(3, 3)` would be rejected because `cos(45°)^2` evaluates to `0.5000000000000001`.

Special cases: a target on top of the agent is visible, `fovDeg >= 360` is a pure range check, and
a zero heading means "no facing direction" (returns false).

`SideOf` uses the sign of the 2D cross product `h x d = h.x*d.y - h.y*d.x` (math convention, y up:
positive = left). It returns 0 when `cross^2 <= 1e-12 * |h|^2 * |d|^2`, i.e. when the sine of the angle
is practically zero. In Unity's left-handed `(x, z)` plane the sign is reversed
(`Vector3.Cross(forward, dir).y > 0` means right), as the slide notes point out.

## Verification: real console output

Output of `dotnet run` (copied unchanged):

```
Digital Game AI - Week 1: Vector2D, field of view and side tests
======================================================================

=== Lab questions (slide 25) ===
Q1 Distance A(2, 1) -> B(8, 9)             computed: 10                                     expected: 10                                     PASS
Q2 |(6, 8)| and unit vector                computed: 10, (0.6, 0.8)                         expected: 10, (0.6, 0.8)                         PASS
Q3 Angle heading (1, 0) -> target (2, 2)   computed: dir (0.707, 0.707), dot 0.707, 45 deg  expected: dir (0.707, 0.707), dot 0.707, 45 deg  PASS
Q4 Heading (0, 1), target dir (-3, 1)      computed: cross 3 -> left                        expected: cross 3 -> left                        PASS
Q5 20 u/s at 60 deg -> (v.x, v.y)          computed: (10, 17.321)                           expected: (10, 17.32)                            PASS

=== IsInFov(agent, heading, target, fovDeg, range) ===
#  Case                                        agent    heading  target           fov  range expected actual  result
1  Slide 20: P1 in front                       (0, 0)   (0, 1)   (1, 3)           90   10    True     True    PASS
2  Slide 20: P2 too far to the side            (0, 0)   (0, 1)   (4, 1)           90   10    False    False   PASS
3  Boundary: exactly 45 deg (half of 90)       (0, 0)   (0, 1)   (3, 3)           90   10    True     True    PASS
4  Just inside: 44.9 deg                       (0, 0)   (0, 1)   (3.529, 3.542)   90   10    True     True    PASS
5  Just outside: 45.1 deg                      (0, 0)   (0, 1)   (3.542, 3.529)   90   10    False    False   PASS
6  Directly behind (180 deg)                   (0, 0)   (0, 1)   (0, -5)          90   10    False    False   PASS
7  Boundary: exactly at range                  (0, 0)   (0, 1)   (0, 10)          90   10    True     True    PASS
8  Just beyond range                           (0, 0)   (0, 1)   (0, 10.001)      90   10    False    False   PASS
9  Non-unit heading (0, 7), same as #1         (0, 0)   (0, 7)   (1, 3)           90   10    True     True    PASS
10 Agent not at origin, ~14 deg                (5, 5)   (1, 0)   (9, 6)           60   8     True     True    PASS
11 Narrow 10 deg cone, target at 4 deg         (0, 0)   (0, 1)   (0.419, 5.985)   10   10    True     True    PASS
12 Narrow 10 deg cone, target at 6 deg         (0, 0)   (0, 1)   (0.627, 5.967)   10   10    False    False   PASS
13 Boundary: 180 deg cone, exactly sideways    (0, 0)   (0, 1)   (5, 0)           180  10    True     True    PASS
14 180 deg cone, slightly behind the side      (0, 0)   (0, 1)   (5, -0.01)       180  10    False    False   PASS
15 Wide 270 deg cone, target at 120 deg        (0, 0)   (0, 1)   (4.33, -2.5)     270  10    True     True    PASS
16 Boundary: 270 deg cone, exactly 135 deg     (0, 0)   (0, 1)   (-4, -4)         270  10    True     True    PASS
17 Wide 270 deg cone, target directly behind   (0, 0)   (0, 1)   (0, -5)          270  10    False    False   PASS
18 Full 360 deg cone, target behind            (0, 0)   (0, 1)   (0, -5)          360  10    True     True    PASS
19 Target on top of the agent                  (0, 0)   (0, 1)   (0, 0)           90   10    True     True    PASS
20 Zero heading (no facing direction)          (0, 0)   (0, 0)   (0, 5)           90   10    False    False   PASS

=== SideOf(agent, heading, target): +1 left, -1 right, 0 collinear ===
#  Case                                        agent    heading  target                      expected actual  result
1  Slide 21: (1, 3) is right of (0, 1)         (0, 0)   (0, 1)   (1, 3)                      -1       -1      PASS
2  Lab Q4: (-3, 1) is left of (0, 1)           (0, 0)   (0, 1)   (-3, 1)                     +1       +1      PASS
3  Straight ahead                              (0, 0)   (0, 1)   (0, 5)                      0        0       PASS
4  Straight behind                             (0, 0)   (0, 1)   (0, -5)                     0        0       PASS
5  Diagonal, collinear, agent moved            (2, 2)   (1, 1)   (5, 5)                      0        0       PASS
6  Diagonal heading, target left               (2, 2)   (1, 1)   (3, 5)                      +1       +1      PASS
7  Diagonal heading, target right              (2, 2)   (1, 1)   (5, 3)                      -1       -1      PASS
8  Heading +x, target (2, 2) is left           (0, 0)   (1, 0)   (2, 2)                      +1       +1      PASS
9  Long heading, target barely left            (0, 0)   (0, 10)  (-0.001, 100)               +1       +1      PASS

Randomized cross-check vs sqrt+acos reference: 100000 cases (17405 visible), 0 mismatches  PASS

Summary: 30 passed, 0 failed

ALL CHECKS PASSED
```

The randomized check compares `IsInFov` with a naive reference that uses `Length()` and `Math.Acos`
(it lives only in the test file). Cases within `1e-6` of a boundary are skipped because the two
versions may legitimately differ by round-off there; the boundaries are covered by the explicit cases
3, 7, 13 and 16.

## Screenshots

Not applicable (console program); the output above is the evidence.

## Bonus

No bonus was set for this week. Extras: the precomputed-cosine overload, wide cones (> 180°), and the
randomized cross-check.

## Manual steps

None. `bin/` and `obj/` are build output and are ignored by git. The folder contains its own
`.gitignore` with `!*.csproj`, because the repository root ignores `*.csproj` for the Unity projects.
