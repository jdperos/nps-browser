using Avalonia;
using Avalonia.Controls;
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
            => AppBuilder.Configure<App>()
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