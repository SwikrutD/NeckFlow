using System.Text.Json;
using System.IO;
namespace NeckFlow.Services;
public sealed class ActivityService
{
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeckFlow", "activity.json");
    private readonly List<ActivityEntry> _entries = new();
    public ActivityService() { try { if (File.Exists(_path)) _entries.AddRange(JsonSerializer.Deserialize<List<ActivityEntry>>(File.ReadAllText(_path)) ?? new()); } catch (Exception error) { LogService.Write("Activity load failed", error); } Trim(); }
    public void Record(string exercise, bool completed) { _entries.Add(new ActivityEntry(DateTime.Now, exercise, completed)); Trim(); try { Directory.CreateDirectory(Path.GetDirectoryName(_path)!); File.WriteAllText(_path, JsonSerializer.Serialize(_entries)); } catch (Exception error) { LogService.Write("Activity save failed", error); } }
    private void Trim() { _entries.RemoveAll(x => x.When < DateTime.Now.AddDays(-30)); }
    public IReadOnlyList<ActivityEntry> Recent => _entries;
    public sealed record ActivityEntry(DateTime When, string Exercise, bool Completed);
}
