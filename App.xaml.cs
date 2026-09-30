using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace TagWriter;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnUnhandledException;
    }

    void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TagWriter_crash.log");
            File.WriteAllText(path, $"{DateTime.Now:O}\r\n{e.Exception}");
        }
        catch { }
        MessageBox.Show($"TagWriter 시작 중 오류가 발생했습니다.\n\n{e.Exception.Message}\n\n실행 폴더의 TagWriter_crash.log를 확인해 주세요.", "TagWriter 오류");
        e.Handled = true;
        Shutdown(-1);
    }
}