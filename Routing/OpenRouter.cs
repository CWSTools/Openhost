using System.Diagnostics;

namespace OpenHost.Routing;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class OpenRouter(IReadOnlyDictionary<string, string> preferences)
{
    public static readonly IReadOnlyDictionary<string, string[]> ExtensionMap = new Dictionary<string, string[]>
    {
        ["powerpoint"] = [".ppt", ".pptx"],
        ["word"] = [".doc", ".docx"],
        ["excel"] = [".xls", ".xlsx"],
        ["pdf"] = [".pdf"]
    };

    public bool Open(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        var entryKey = ExtensionMap.FirstOrDefault(
            pair => pair.Value.Contains(extension, StringComparer.OrdinalIgnoreCase)).Key;

        if (string.IsNullOrWhiteSpace(entryKey))
        {
            return OpenWithSystem(filePath);
        }

        var target = preferences.TryGetValue(entryKey, out var configuredTarget)
            ? NormalizeTarget(configuredTarget)
            : OpenTargets.System;

        if (target == OpenTargets.System)
        {
            return OpenWithSystem(filePath);
        }

        var app = ResolveTargetApp(entryKey, target);
        return app is not null && OpenWithExecutable(app.ExecutablePath, filePath);
    }

    private static bool OpenWithSystem(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool OpenWithExecutable(string executablePath, string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static AppInfo? ResolveTargetApp(string entryKey, string target)
    {
        return (entryKey, target) switch
        {
            ("word", OpenTargets.Office) => FindApp("WINWORD.EXE"),
            ("excel", OpenTargets.Office) => FindApp("EXCEL.EXE"),
            ("powerpoint", OpenTargets.Office) => FindApp("POWERPNT.EXE"),
            ("pdf", OpenTargets.Office) => FindApp("WINWORD.EXE"),
            ("word", OpenTargets.Wps) => FindApp("wps.exe"),
            ("excel", OpenTargets.Wps) => FindApp("et.exe"),
            ("powerpoint", OpenTargets.Wps) => FindApp("wpp.exe"),
            ("pdf", OpenTargets.Wps) => FindApp("wpspdf.exe"),
            _ => null
        };
    }

    private static AppInfo? FindApp(string exeName)
    {
        var path = AppDiscovery.FindExecutable(exeName);
        return path is null ? null : new AppInfo(path);
    }

    public static string NormalizeEntryKey(string? entryKey)
    {
        if (string.IsNullOrWhiteSpace(entryKey))
        {
            return string.Empty;
        }

        var normalized = entryKey.Trim().ToLowerInvariant();
        if (!normalized.StartsWith('.'))
        {
            normalized = $".{normalized}";
        }

        var extensionEntryKey = ExtensionMap.FirstOrDefault(
            pair => pair.Value.Contains(normalized, StringComparer.OrdinalIgnoreCase)).Key;
        if (!string.IsNullOrWhiteSpace(extensionEntryKey))
        {
            return extensionEntryKey;
        }

        normalized = entryKey.Trim().ToLowerInvariant();
        return ExtensionMap.ContainsKey(normalized) ? normalized : string.Empty;
    }

    public static string NormalizeTarget(string? target)
    {
        var normalized = target?.Trim();
        if (string.Equals(normalized, OpenTargets.Office, StringComparison.OrdinalIgnoreCase))
        {
            return OpenTargets.Office;
        }

        if (string.Equals(normalized, OpenTargets.Wps, StringComparison.OrdinalIgnoreCase))
        {
            return OpenTargets.Wps;
        }

        if (string.Equals(normalized, OpenTargets.System, StringComparison.OrdinalIgnoreCase))
        {
            return OpenTargets.System;
        }

        return OpenTargets.System;
    }
}
