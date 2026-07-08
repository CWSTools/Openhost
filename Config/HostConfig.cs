using System.Text.Json;

namespace OpenHost.Config;

internal sealed class HostConfig
{
    public Dictionary<string, string> OpenMethodPreferences { get; set; } = [];

    public static HostConfig Load()
    {
        try
        {
            var path = GetConfigPath();
            if (!File.Exists(path))
            {
                path = Path.Combine(AppContext.BaseDirectory, "Config", "config.json");
            }

            if (!File.Exists(path))
            {
                return new HostConfig();
            }

            return JsonSerializer.Deserialize<HostConfig>(File.ReadAllText(path)) ?? new HostConfig();
        }
        catch
        {
            return new HostConfig();
        }
    }

    public bool Save()
    {
        try
        {
            var path = GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(path, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string GetConfigPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenHost",
            "config.json");
    }
}
