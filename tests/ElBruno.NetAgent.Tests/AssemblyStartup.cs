using System.Runtime.CompilerServices;
using System.Windows;

internal static class AssemblyStartup
{
    [ModuleInitializer]
    public static void Initialize()
    {
        try
        {
            // Ensure there is a running WPF dispatcher for tests that expect Application.Current
            // Create the Application on a dedicated STA background thread and run its Dispatcher loop.
            if (Application.Current == null)
            {
                var th = new System.Threading.Thread(() =>
                {
                    try
                    {
                        var app = new Application();
                        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                        System.Windows.Threading.Dispatcher.Run();
                    }
                    catch { /* swallow - some environments may not support WPF */ }
                });
                th.SetApartmentState(System.Threading.ApartmentState.STA);
                th.IsBackground = true;
                th.Start();
            }
        }
        catch { /* best-effort, tests may run in environments without WPF support */ }
    }
}
