using System;
using System.Globalization;

// Week 1 - "YZ'nin Matematigi": lab questions + IsInFov / SideOf homework tests.
// Run with:  dotnet run
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

Console.WriteLine("Digital Game AI - Week 1: Vector2D, field of view and side tests");
Console.WriteLine(new string('=', 70));
Console.WriteLine();

int failures = LabQuestions.Run();
failures += FovTests.Run();

Console.WriteLine();
Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
return failures == 0 ? 0 : 1;
