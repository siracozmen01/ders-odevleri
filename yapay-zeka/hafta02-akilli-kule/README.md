# Week 2 - Smart turret (Digital Game AI)

Course: *Dijital Oyunlarda Yapay Zekâ*, week 2 "YZ'nin Fiziği" (Dr. Öğr. Üyesi Can Meşe).

The Turkish report for the teacher, with the full console output, the scenario table and the
required comment ("where is the approximate method not good enough?"), is in **[RAPOR.md](RAPOR.md)**.

## Assignment

In my own words, from slides 21-23 of the week 2 deck:

- **Lab (slide 21)**
  1. Extend `Vector2D` with `Truncate`, `Distance`, `DistanceSq` (test: `(6, 8).Truncate(5) = (3, 4)`).
  2. Write an `Agent` class (Position, Velocity, Heading, Mass, MaxSpeed, MaxForce, `Update(force, dt)`), produce the Euler error table for Δt = 0.5, 0.1, 0.02, and show that MaxSpeed = 3 caps the speed.
  3. `FuturePosition(u, a, t)` and `TimeToCoverDistance(u, a, distance)` (unreachable = -1), and reproduce the soccer ball table.
  4. `InterceptTime(P, V, s)`: solve examples A and B approximately and exactly and print the aim points side by side.
- **Homework (slide 22), "Akıllı kule"**
  - `bool NisanAl(Vector2D kule, Vector2D hedef, Vector2D hedefHiz, double mermiSurati, out Vector2D nisan, out double sure)`: exact lead aim, false if the projectile can never catch the target.
  - `bool AtisAcisi(double v, double x, double y, out double alcak, out double yuksek)`: the two ballistic angles, false when out of range.
  - At least 6 scenarios (crossing, receding, approaching, stationary, faster than the bullet, out of range), each with approximate and exact aim side by side.
  - A 3-5 sentence comment on where the approximate method fails.
  - Bonus (3D): launch velocity vector with `Vector3D`.
- **Exercises (slide 23)**: the seven exercises, checked against the answer key in the speaker notes.

## How to run

Requires the .NET SDK (built and tested with .NET 10.0.100).

```
cd yapay-zeka/hafta02-akilli-kule
dotnet run
```

It is a console program with no controls. It prints every table and a PASS/FAIL per check, and
exits with code 0 only if all checks pass. Current result: `Özet: 58 PASS, 0 FAIL`.
The console output is in Turkish because it is pasted into the Turkish report.

## Files

| File | Contents |
|---|---|
| `Vector2D.cs` | Week 1 vector struct + `Truncate`, `Distance`, `DistanceSq`. |
| `Vector3D.cs` | Minimal 3D vector for the bonus (Unity axes, y up). |
| `Agent.cs` | Moving agent with semi-implicit Euler `Update(force, dt)`; force clipped to `MaxForce`, speed to `MaxSpeed`. |
| `Kinematics.cs` | `FuturePosition`, `TimeToCoverDistance`, `TimeToStop`, `StoppingDistance`. |
| `Interception.cs` | `InterceptTime` (exact quadratic), `ApproximateInterceptTime` (Buckland), `MissDistance`. |
| `SmartTurret.cs` | `NisanAl`, `AtisAcisi`, `ApproximateAim`, `FlightTime`, `RootTerm`, `LaunchVelocity` (3D bonus), `BallisticLeadAim` (iterative correction for arcing shells). |
| `Lab.cs` | Lab steps 1-4 with the slide values as expected results. |
| `Homework.cs` | `AtisAcisi` vs the slide 16-17 table, the 9 turret scenarios, the ballistic table, the iterative correction and the 3D bonus. |
| `Exercises.cs` | The 7 exercises of slide 23. |
| `Report.cs` | Console formatting and PASS/FAIL counting. |
| `Program.cs` | Entry point. |
| `Hafta02.csproj` | Project file (`net10.0`, `LangVersion 9.0` so the code also compiles in Unity 6). |

## Requirement to code mapping

| Requirement | Where | Verified against |
|---|---|---|
| `Truncate`, `Distance`, `DistanceSq` | `Vector2D` | `(6, 8).Truncate(5) = (3, 4)`, slide 7 |
| `Agent` + `Update(force, dt)` | `Agent` | slide 9 |
| Euler error table (0.5 / 0.1 / 0.02) | `Lab.Step2Agent` | 3.75 / 2.75 / 2.55 m, slide 10 |
| MaxSpeed = 3 demo | `Lab.Step2Agent` | speed stays at 3, slide 21 notes |
| `FuturePosition`, `TimeToCoverDistance` (-1) + ball table | `Kinematics`, `Lab.Step3Ball` | 5 s, 25 m, 21 m, 2 s, -1, slide 12 |
| `InterceptTime`, examples A and B | `Interception`, `Lab.Step4Intercept` | A: 0.556 s / 0.680 s, B: 0.5 s / 1 s, slide 15 |
| `NisanAl` | `SmartTurret.NisanAl` | each scenario: the bullet and target arrive together (miss 0) |
| `AtisAcisi` | `SmartTurret.AtisAcisi` | 23.7°/66.3°, 36.2°/63.3°, out of range at 45 m, slides 16-17 |
| 6+ scenarios, approximate vs exact | `Homework.ScenarioTable` | 6 required + 3 extra |
| Comment (3-5 sentences) | `RAPOR.md`, section 3 | |
| Bonus 3D launch velocity | `SmartTurret.LaunchVelocity` | (11.6, 5.1, 15.5), slide 18 notes; landing point simulated |
| Exercises 1-7 | `Exercises.Run` | slide 23 notes |

## Design notes

- `NisanAl` returns false instead of a wrong point, so the AI can move closer or switch targets.
  The approximate method (`ApproximateAim`) is kept only for comparison: it always returns a point,
  even for a target that can never be hit.
- `InterceptTime` handles the case where the target is exactly as fast as the bullet (the quadratic
  becomes linear) and a target faster than the bullet that is coming towards the turret (two
  positive roots; the earliest one is used).
- `AtisAcisi` returns angles in degrees with `g = 9.81`, like Unity's default `Physics.gravity`.
- The exact intercept assumes a straight-flying projectile. `BallisticLeadAim` repeats the prediction
  with the real ballistic flight time, as suggested on slide 18; in scenario 1 the miss drops from
  0.075 m to 0 within three rounds.

## Screenshots

Not applicable (console program). The full output is in [RAPOR.md](RAPOR.md), section 6.

## Bonus done

- 3D launch velocity with `Vector3D` (`SmartTurret.LaunchVelocity`): yaw 36.87°, low pitch 14.69°,
  velocity (11.608, 5.071, 15.477), lands exactly on (12, 1, 16).
- Extras: three additional scenarios, the ballistic table for every exact aim point, and the iterative
  flight-time correction.

## Manual steps

None. `bin/` and `obj/` are build output and are ignored by git. The folder has its own `.gitignore`
with `!*.csproj`, because the repository root ignores `*.csproj` for the Unity projects.
