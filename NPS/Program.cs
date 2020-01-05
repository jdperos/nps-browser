using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Dialogs;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;

namespace NPS
{
    internal static class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            BuildAvaloniaApp().Start(AppMain, args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<App>()
                // Doing these #if switches manually means we don't include unnecessary assemblies for publish.
#if PUBLISHWIN
                .UseWin32()
                .UseSkia()
#elif PUBLISHLINUX
                .UseX11()
                .UseSkia()
#elif PUBLISHOSX
                .UseAvaloniaNative()
                .UseSkia()
#else
                .UsePlatformDetect()
                .UseSkia()
#endif
                .LogToDebug()
                .UseReactiveUI();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Native system dialogs are broken on Linux right now with Avalonia.
                // I hear Skiasharp has a fix for this but let's just wait.
                builder.UseManagedSystemDialogs();
            }

            return builder;
        }


        // Your application's entry point. Here you can initialize your MVVM framework, DI
        // container, etc.
        private static void AppMain(Application app, string[] args)
        {
            var window = new NpsBrowser();

            window.Start();

            app.Run(window);
        }
    }
}