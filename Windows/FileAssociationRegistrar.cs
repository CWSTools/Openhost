using OpenHost.Routing;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace OpenHost.Windows;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class FileAssociationRegistrar
{
    private const string Scheme = "openhost";
    private const string ProgIdPrefix = "OpenHost";

    public static bool Register(string appDirectory)
    {
        try
        {
            var hostPath = Path.Combine(appDirectory, "OpenHost.exe");
            if (!File.Exists(hostPath))
            {
                hostPath = Environment.ProcessPath ?? hostPath;
            }

            RemoveLegacyRegistration();
            RegisterProtocol(hostPath);
            foreach (var extension in OpenRouter.ExtensionMap.SelectMany(pair => pair.Value).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                RegisterExtension(hostPath, extension);
            }

            SHChangeNotify(0x08000000, 0, nint.Zero, nint.Zero);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RemoveLegacyRegistration()
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\cwstool", false);
        foreach (var extension in OpenRouter.ExtensionMap.SelectMany(pair => pair.Value).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\CWSOpenHost{extension}", false);
        }
    }

    private static void RegisterProtocol(string hostPath)
    {
        using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Scheme}");
        key.SetValue(string.Empty, "URL:OpenHost");
        key.SetValue("URL Protocol", string.Empty);

        using var icon = key.CreateSubKey("DefaultIcon");
        icon.SetValue(string.Empty, Quote(hostPath));

        using var command = key.CreateSubKey(@"shell\open\command");
        command.SetValue(string.Empty, $"{Quote(hostPath)} \"%1\"");
    }

    private static void RegisterExtension(string hostPath, string extension)
    {
        var progId = $"{ProgIdPrefix}{extension}";
        using var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}");
        progIdKey.SetValue(string.Empty, $"OpenHost {extension.ToUpperInvariant()}");

        using var icon = progIdKey.CreateSubKey("DefaultIcon");
        icon.SetValue(string.Empty, Quote(hostPath));

        using var command = progIdKey.CreateSubKey(@"shell\open\command");
        command.SetValue(string.Empty, $"{Quote(hostPath)} \"%1\"");

        using var extensionKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}");
        extensionKey.SetValue(string.Empty, progId);

        using var openWithProgids = extensionKey.CreateSubKey("OpenWithProgids");
        openWithProgids.SetValue(progId, Array.Empty<byte>(), RegistryValueKind.None);
    }

    private static string Quote(string value)
    {
        return $"\"{value}\"";
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, uint flags, nint item1, nint item2);
}
