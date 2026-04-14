using System.Linq;
using System.Windows;

namespace VolumeGuard;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Any(static a => string.Equals(a, "--watchdog", StringComparison.OrdinalIgnoreCase)))
        {
            return WatchdogRunner.Run(args);
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
        return 0;
    }
}
