using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

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

    public static string CrashLogPath => _crashLogPath ??= BuildCrashLogPath();

    static string BuildCrashLogPath()
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TagWriter",
                "Logs");
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
