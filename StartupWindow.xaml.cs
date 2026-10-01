using System.IO;
using System.Windows;
using TagWriter.Services;

namespace TagWriter;

public partial class StartupWindow : Window
{
    readonly string? _recentPath;

    public bool CreateNew { get; private set; }
    public string? ProjectPath { get; private set; }

    public StartupWindow(string? recentPath)
    {
        InitializeComponent();
        _recentPath = string.IsNullOrWhiteSpace(recentPath) ? null : recentPath;

        if (_recentPath != null && File.Exists(_recentPath))
        {
            RecentNameText.Text = Path.GetFileNameWithoutExtension(_recentPath);
            RecentPathText.Text = _recentPath;
            RecentButton.IsEnabled = true;
        }
        else
        {
            RecentNameText.Text = "최근 문서 없음";
            RecentPathText.Text = "저장된 최근 문서가 현재 위치에 없습니다.";
            RecentButton.IsEnabled = false;
        }
    }

    void RecentButton_Click(object sender, RoutedEventArgs e)
    {
        ProjectPath = _recentPath;
        CreateNew = false;
        DialogResult = true;
    }

    void NewButton_Click(object sender, RoutedEventArgs e)
    {
        ProjectPath = null;
        CreateNew = true;
        DialogResult = true;
    }
}
