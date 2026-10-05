using System.Text.Json;
using System.IO;
using NeckFlow.Models;

namespace NeckFlow.Services;

public sealed class SettingsService
{
    private readonly string _dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeckFlow");
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    public AppSettings Current { get; private set; } = new();
    public string PathName => Path.Combine(_dir, "settings.json");
    public void Load()
    {
        try { Directory.CreateDirectory(_dir); if (File.Exists(PathName)) Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(PathName), _json) ?? new AppSettings(); }
        catch (Exception error) { LogService.Write("Settings load failed; regenerated defaults", error); try { File.Move(PathName, PathName + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss"), true); } catch { } Current = new AppSettings(); Save(); }
    }
    public void Save() { try { Directory.CreateDirectory(_dir); File.WriteAllText(PathName, JsonSerializer.Serialize(Current, _json)); } catch (Exception error) { LogService.Write("Settings save failed", error); } }
}
