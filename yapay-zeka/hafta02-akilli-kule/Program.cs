using System;
using System.Globalization;
using System.Text;

// Week 2 - "YZ'nin Fizigi": lab steps 1-4, homework "Akilli kule" (smart turret), 3D bonus and exercises.
// Run with:  dotnet run
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
Console.OutputEncoding = new UTF8Encoding(false);

Console.WriteLine("Dijital Oyunlarda Yapay Zekâ - Hafta 2: Akıllı kule");
Console.WriteLine(new string('=', 60));

Lab.Run();
Homework.Run();
Exercises.Run();

Console.WriteLine();
Console.WriteLine($"Özet: {Report.Passed} PASS, {Report.Failed} FAIL");
return Report.Failed == 0 ? 0 : 1;
