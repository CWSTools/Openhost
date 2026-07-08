using OpenHost.Config;
using OpenHost.Routing;
using OpenHost.Windows;

namespace OpenHost.Protocol;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class ProtocolHandler(OpenRouter router, HostConfig config)
{
    private const string Scheme = "openhost";
    private const string OpenFilePrefix = "openhost://open?file=";

    public bool TryHandle(string arg, out bool handled)
    {
        handled = false;
        if (TryHandleOpenFilePrefix(arg, out handled))
        {
            return true;
        }

        if (!Uri.TryCreate(arg, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var action = GetAction(uri);
        switch (action)
        {
            case "open":
                var filePath = GetFilePath(uri);
                handled = !string.IsNullOrWhiteSpace(filePath) &&
                          File.Exists(filePath) &&
                          router.Open(filePath);
                return true;
            case "register":
            case "register-open-host":
                handled = FileAssociationRegistrar.Register(AppContext.BaseDirectory);
                return true;
            case "set-open-method":
            case "open-method":
                handled = SetOpenMethod(uri);
                return true;
            case "query-apps":
            case "apps":
                handled = AppDiscoverySnapshot.Write();
                return true;
            default:
                handled = false;
                return true;
        }
    }

    private bool TryHandleOpenFilePrefix(string arg, out bool handled)
    {
        handled = false;
        if (!arg.StartsWith(OpenFilePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var filePath = Decode(arg[OpenFilePrefix.Length..].Trim('"'));
        handled = !string.IsNullOrWhiteSpace(filePath) &&
                  File.Exists(filePath) &&
                  router.Open(filePath);
        return true;
    }

    private static string GetAction(Uri uri)
    {
        if (!string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host.Trim().ToLowerInvariant();
        }

        return uri.AbsolutePath.Trim('/').ToLowerInvariant();
    }

    private static string? GetFilePath(Uri uri)
    {
        var queryFile = GetQueryValue(uri, "file");
        if (!string.IsNullOrWhiteSpace(queryFile))
        {
            return queryFile;
        }

        var path = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    private static string? GetQueryValue(Uri uri, string key)
    {
        var query = uri.Query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var name = Decode(parts[0]);
            if (!string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return parts.Length == 2 ? Decode(parts[1]) : string.Empty;
        }

        return null;
    }

    private static string Decode(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value.Replace("+", " "));
        }
        catch
        {
            return value.Replace("+", " ");
        }
    }

    private bool SetOpenMethod(Uri uri)
    {
        var entryKey = GetQueryValue(uri, "type") ??
                       GetQueryValue(uri, "entry") ??
                       GetQueryValue(uri, "kind");
        var target = GetQueryValue(uri, "target") ??
                     GetQueryValue(uri, "method") ??
                     GetQueryValue(uri, "open");

        if (string.IsNullOrWhiteSpace(entryKey) || string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        entryKey = OpenRouter.NormalizeEntryKey(entryKey);
        target = OpenRouter.NormalizeTarget(target);
        if (string.IsNullOrWhiteSpace(entryKey))
        {
            return false;
        }

        config.OpenMethodPreferences[entryKey] = target;
        return config.Save();
    }
}
