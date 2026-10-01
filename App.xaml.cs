using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using TagWriter.Services;

namespace TagWriter;

public partial class App : Application
{
    static readonly object CrashLogLock = new();
    static string? _crashLogPath;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var settings = AppSettingsStore.Load();
        var dark = !string.Equals(settings.Theme, "light", StringComparison.OrdinalIgnoreCase);
        ApplyStartupTheme(dark);

        var recentPaths=new List<string>();
        if(settings.RecentProjectPaths!=null)recentPaths.AddRange(settings.RecentProjectPaths);
        if(!string.IsNullOrWhiteSpace(settings.RecentProjectPath)&&!recentPaths.Contains(settings.RecentProjectPath,StringComparer.OrdinalIgnoreCase))
            recentPaths.Add(settings.RecentProjectPath);

        var chooser = new StartupWindow(recentPaths, dark);
        if (chooser.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        var main = new MainWindow();
        MainWindow = main;

        if (!chooser.CreateNew && !string.IsNullOrWhiteSpace(chooser.ProjectPath))
            await main.LoadProjectFromPathAsync(chooser.ProjectPath!);

        main.Show();
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    static void ApplyStartupTheme(bool dark)
    {
        void Set(string key, string hex) => Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        Set("Bg", dark ? "#181B1F" : "#E8EBEF");
        Set("Surface", dark ? "#20242A" : "#F5F6F8");
        Set("Panel", dark ? "#242930" : "#EEF1F4");
        Set("Panel2", dark ? "#2B3139" : "#FFFFFF");
        Set("EditorBg", dark ? "#1D2126" : "#FFFFFF");
        Set("Text", dark ? "#E6E9ED" : "#20242A");
        Set("Muted", dark ? "#939BA6" : "#66707C");
        Set("Border", dark ? "#343B45" : "#D4D9E0");
        Set("Accent", dark ? "#70A9E8" : "#2F72B7");
        Set("Selection", dark ? "#304B68" : "#BFD9F4");
        Set("Hover", dark ? "#2B3139" : "#E1E5EA");
        Set("ToolTipBg", dark ? "#242930" : "#FFFFFF");
        Set("ToolTipText", dark ? "#E6E9ED" : "#20242A");
        Set("TabSelected", dark ? "#1E2227" : "#E1E5EA");
    }

    public static string CrashLogPath => _crashLogPath ??= BuildCrashLogPath();

    static string BuildCrashLogPath()
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TagWriter",
                "Crash");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "TagWriter_crash.log");
        }
        catch
        {
            return Path.Combine(Path.GetTempPath(), "TagWriter_crash.log");
        }
    }

    static void WriteCrashLog(string source, Exception? exception, string? extra = null)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("============================================================");
            sb.AppendLine(DateTimeOffset.Now.ToString("O"));
            sb.AppendLine($"Source: {source}");
            sb.AppendLine($"AppVersion: {typeof(App).Assembly.GetName().Version}");
            sb.AppendLine($"OS: {Environment.OSVersion}");
            sb.AppendLine($"64BitProcess: {Environment.Is64BitProcess}");
            sb.AppendLine($"BaseDirectory: {AppContext.BaseDirectory}");
            if (!string.IsNullOrWhiteSpace(extra)) sb.AppendLine(extra);
            if (exception != null)
            {
                sb.AppendLine();
                sb.AppendLine(exception.ToString());
            }

            lock (CrashLogLock)
            {
                var path = CrashLogPath;
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Never allow crash logging itself to hide the original failure.
        }
    }

    void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrashLog("DispatcherUnhandledException", e.Exception);
        try
        {
            MessageBox.Show(
                $"TagWriter 오류가 발생했습니다.\n\n{e.Exception.Message}\n\n로그:\n{CrashLogPath}",
                "TagWriter 오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch { }

        e.Handled = true;
        Shutdown(-1);
    }

    static void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        WriteCrashLog(
            "AppDomain.UnhandledException",
            exception,
            $"IsTerminating: {e.IsTerminating}\r\nExceptionObject: {e.ExceptionObject}");
    }

    static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteCrashLog("TaskScheduler.UnobservedTaskException", e.Exception);
        e.SetObserved();
    }
}
