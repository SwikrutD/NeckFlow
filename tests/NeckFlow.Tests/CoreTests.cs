using System.Text.Json;
using NeckFlow.Models;
Console.WriteLine("Running NeckFlow core tests...");
void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); }
Check(Exercises.All[0].DurationSeconds == 20 && Exercises.All.Count >= 5, "default exercises");
var s=new AppSettings{ReminderMinutes=45,Theme="Dark"}; var json=JsonSerializer.Serialize(s); var copy=JsonSerializer.Deserialize<AppSettings>(json)!;
Check(copy.ReminderMinutes == 45 && copy.Theme == "Dark", "settings round trip");
Check(Exercises.All.Select(x=>x.Kind).SequenceEqual(new AppSettings().ExerciseOrder), "exercise order");
Console.WriteLine("All core tests passed.");
/*
public class CoreTests
{
}
*/
