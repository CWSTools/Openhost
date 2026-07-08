using System.Text.Json;

namespace OpenHost.Routing;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class AppDiscoverySnapshot
{
    public static bool Write()
    {
        try
        {
            var path = GetSnapshotPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(AppDiscovery.QueryOfficeAndWps(), new JsonSerializerOptions
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

    public static string GetSnapshotPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenHost",
            "app-locations.json");
    }
}
