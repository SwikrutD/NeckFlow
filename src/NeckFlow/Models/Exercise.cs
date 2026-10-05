namespace NeckFlow.Models;

public enum ExerciseKind { ChinTuck, LookLeftRight, SideTilt, ShoulderRolls, ShoulderBladeSqueeze }

public sealed class ExerciseDefinition
{
    public ExerciseKind Kind { get; init; }
    public string Name { get; init; } = "Exercise";
    public string Instructions { get; init; } = "Move gently within a comfortable range.";
    public string Targets { get; init; } = "Neck";
    public int DurationSeconds { get; init; } = 20;
    public bool Enabled { get; set; } = true;
}

public static class Exercises
{
    public static readonly IReadOnlyList<ExerciseDefinition> All = new[]
    {
        new ExerciseDefinition { Kind=ExerciseKind.ChinTuck, Name="Chin Tuck", Targets="Deep neck flexors · suboccipitals", DurationSeconds=20, Instructions="Keep your eyes level and gently draw your chin straight backward." },
        new ExerciseDefinition { Kind=ExerciseKind.LookLeftRight, Name="Look Left / Right", Targets="Sternocleidomastoid", DurationSeconds=30, Instructions="Slowly turn your head left and right. Stay within a comfortable range." },
        new ExerciseDefinition { Kind=ExerciseKind.SideTilt, Name="Side Tilt", Targets="Upper trapezius · scalenes", DurationSeconds=30, Instructions="Gently tilt your ear toward your shoulder without raising the shoulder." },
        new ExerciseDefinition { Kind=ExerciseKind.ShoulderRolls, Name="Shoulder Rolls", Targets="Upper trapezius · levator scapulae", DurationSeconds=30, Instructions="Roll your shoulders slowly up, back, down, and forward." },
        new ExerciseDefinition { Kind=ExerciseKind.ShoulderBladeSqueeze, Name="Shoulder Blade Squeeze", Targets="Rhomboids · middle trapezius", DurationSeconds=20, Instructions="Gently draw your shoulder blades backward and down." }
    };
}
