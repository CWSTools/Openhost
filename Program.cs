using OpenHost.Config;
using OpenHost.Protocol;
using OpenHost.Routing;

namespace OpenHost;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            return 2;
        }

        var config = HostConfig.Load();
        var router = new OpenRouter(config.OpenMethodPreferences);
        var protocolHandler = new ProtocolHandler(router, config);
        var exitCode = 0;
        var handledAny = false;

        foreach (var arg in args.Where(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase)))
        {
            if (protocolHandler.TryHandle(arg, out var protocolHandled))
            {
                handledAny = true;
                if (!protocolHandled)
                {
                    exitCode = 1;
                }

                continue;
            }

            if (File.Exists(arg))
            {
                handledAny = true;
                if (!router.Open(arg))
                {
                    exitCode = 1;
                }
            }
        }

        return handledAny ? exitCode : 0;
    }
}
