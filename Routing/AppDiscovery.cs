using Microsoft.Win32;

namespace OpenHost.Routing;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class AppDiscovery
{
    private static readonly IReadOnlyDictionary<string, string[]> TargetExecutables = new Dictionary<string, string[]>
    {
        [OpenTargets.Office] = ["WINWORD.EXE", "EXCEL.EXE", "POWERPNT.EXE"],
        [OpenTargets.Wps] = ["wps.exe", "et.exe", "wpp.exe", "wpspdf.exe"]
    };

    public static IReadOnlyList<AppLocation> QueryOfficeAndWps()
    {
        return TargetExecutables
            .SelectMany(pair => pair.Value.Select(exeName => new AppLocation(
                pair.Key,
                exeName,
                FindExecutable(exeName))))
            .ToArray();
    }

    public static string? FindExecutable(string exeName)
    {
        return FindAppPathFromRegistry(exeName) ?? FindAppPathFromCommonFolders(exeName);
    }

    private static string? FindAppPathFromRegistry(string exeName)
    {
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var appPaths = baseKey.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}");
                    var value = appPaths?.GetValue(string.Empty) as string;
                    if (!string.IsNullOrWhiteSpace(value) && File.Exists(value))
                    {
                        return value;
                    }
                }
                catch
                {
                }
            }
        }

        return null;
    }

    private static string? FindAppPathFromCommonFolders(string exeName)
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Office"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Office"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WPS Office"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WPS Office"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Kingsoft"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Kingsoft"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kingsoft"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WPS Office")
        };

        foreach (var root in candidates.Where(Directory.Exists))
        {
            try
            {
                var match = Directory.EnumerateFiles(root, exeName, SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(match))
                {
                    return match;
                }
            }
            catch
            {
            }
        }

        return null;
    }
}

internal sealed record AppLocation(string Target, string ExecutableName, string? ExecutablePath);
