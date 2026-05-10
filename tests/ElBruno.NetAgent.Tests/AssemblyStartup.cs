using System.Runtime.CompilerServices;
using System.Windows;

internal static class AssemblyStartup
{
    [ModuleInitializer]
    public static void Initialize()
    {
        try
        {
            if (Application.Current == null)
            {
                var app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }
        }
        catch { /* best-effort, tests may run in environments without WPF support */ }
    }
}
