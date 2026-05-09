using System;
using System.Windows;

namespace ElBruno.NetAgent
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            // Minimal startup and immediate exit to validate project runs
            var app = new Application();
            app.Shutdown();
        }
    }
}
