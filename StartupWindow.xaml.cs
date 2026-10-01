using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TagWriter.Services;

namespace TagWriter;

public partial class StartupWindow : Window
{
    readonly bool _dark;

    public bool CreateNew { get; private set; }
    public string? ProjectPath { get; private set; }

    public StartupWindow(IEnumerable<string> recentPaths, bool dark)
    {
        _dark = dark;
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyWindowChromeTheme();
        BuildRecentDocuments(recentPaths ?? []);
        VersionText.Text = $"ver. {Assembly.GetExecutingAssembly().GetName().Version}";
    }

    void ApplyWindowChromeTheme()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            var value = _dark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
        }
        catch
        {
        }
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    void BuildRecentDocuments(IEnumerable<string> recentPaths)
    {
        var items = recentPaths
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => new RecentDocument(
                path,
                GetDisplayName(path),
                File.GetLastWriteTimeUtc(path)))
            .OrderByDescending(item => item.ModifiedUtc)
            .ToList();

        RecentCountText.Text = items.Count == 0 ? "" : $"{items.Count}개";
        EmptyRecentText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RecentScrollViewer.Visibility = items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        foreach (var item in items)
        {
            var title = new TextBlock
            {
                Text = item.Name,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var path = new TextBlock
            {
                Text = item.Path,
                FontSize = 10,
                Foreground = (System.Windows.Media.Brush)FindResource("Muted"),
                Margin = new Thickness(0, 4, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var text = new StackPanel();
            text.Children.Add(title);
            text.Children.Add(path);

            var button = new Button
            {
                Content = text,
                Tag = item.Path,
                ToolTip = item.Path,
                Style = (Style)FindResource("RecentDocumentButtonStyle")
            };

            button.Click += RecentDocument_Click;
            RecentDocumentsHost.Children.Add(button);
        }
    }

    static string GetDisplayName(string path)
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            if (json.RootElement.TryGetProperty("project", out var project) &&
                project.TryGetProperty("title", out var title))
            {
                var value = title.GetString();
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
        }
        catch
        {
        }

        return Path.GetFileNameWithoutExtension(path);
    }

    void ClearRecent_Click(object sender, RoutedEventArgs e)
    {
        var settings = AppSettingsStore.Load();
        settings.RecentProjectPath = "";
        settings.RecentProjectPaths ??= [];
        settings.RecentProjectPaths.Clear();
        AppSettingsStore.Save(settings);

        RecentDocumentsHost.Children.Clear();
        RecentCountText.Text = "";
        RecentScrollViewer.Visibility = Visibility.Collapsed;
        EmptyRecentText.Visibility = Visibility.Visible;
    }

    void RecentDocument_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string path } || !File.Exists(path)) return;
        ProjectPath = path;
        CreateNew = false;
        DialogResult = true;
    }

    void NewButton_Click(object sender, RoutedEventArgs e)
    {
        ProjectPath = null;
        CreateNew = true;
        DialogResult = true;
    }

    sealed record RecentDocument(string Path, string Name, DateTime ModifiedUtc);
}
