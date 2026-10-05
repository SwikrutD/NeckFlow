using System.IO;

namespace NeckFlow.Services;

public static class LogService
{
    private static readonly object Gate = new();
    private static readonly string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeckFlow", "Logs");
    public static void Write(string message, Exception? error = null)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(DirectoryPath); var path = Path.Combine(DirectoryPath, "neckflow.log");
                File.AppendAllText(path, $"{DateTime.Now:O} {message}{(error is null ? string.Empty : " | " + error)}{Environment.NewLine}");
                var info = new FileInfo(path); if (info.Exists && info.Length > 512 * 1024) File.Move(path, Path.Combine(DirectoryPath, "neckflow.previous.log"), true);
            }
        }
        catch { }
    }
}
