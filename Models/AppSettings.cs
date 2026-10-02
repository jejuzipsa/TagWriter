using System.Text.Json.Serialization;

namespace TagWriter.Models;

public sealed class AppSettings {
 [JsonPropertyName("theme")] public string Theme {get;set;}="dark";
 [JsonPropertyName("editorFontFamily")] public string EditorFontFamily {get;set;}="Malgun Gothic";
 [JsonPropertyName("editorFontSize")] public double EditorFontSize {get;set;}=17;
 [JsonPropertyName("paperMode")] public bool PaperMode {get;set;}=false;
 [JsonPropertyName("paperZoom")] public double PaperZoom {get;set;}=1.0;
 [JsonPropertyName("paperFit")] public bool PaperFit {get;set;}=true;
 [JsonPropertyName("typingSoundEnabled")] public bool TypingSoundEnabled {get;set;}=false;
 [JsonPropertyName("typingVolume")] public double TypingVolume {get;set;}=35;
 [JsonPropertyName("typingSoundMode")] public string TypingSoundMode {get;set;}="typewriter";
 [JsonPropertyName("autoSaveMinutes")] public int AutoSaveMinutes {get;set;}=10;
 [JsonPropertyName("editorPaperStyle")] public string EditorPaperStyle {get;set;}="blank";
 [JsonPropertyName("recentProjectPath")] public string RecentProjectPath {get;set;}="";
 [JsonPropertyName("recentProjectPaths")] public List<string> RecentProjectPaths {get;set;}=[];
 [JsonPropertyName("windowLeft")] public double? WindowLeft {get;set;}
 [JsonPropertyName("windowTop")] public double? WindowTop {get;set;}
 [JsonPropertyName("windowWidth")] public double WindowWidth {get;set;}=1460;
 [JsonPropertyName("windowHeight")] public double WindowHeight {get;set;}=920;
 [JsonPropertyName("windowMaximized")] public bool WindowMaximized {get;set;}=false;
 [JsonPropertyName("leftPanelWidth")] public double LeftPanelWidth {get;set;}=250;
 [JsonPropertyName("rightPanelWidth")] public double RightPanelWidth {get;set;}=310;
 [JsonPropertyName("leftPanelCollapsed")] public bool LeftPanelCollapsed {get;set;}=false;
 [JsonPropertyName("rightPanelCollapsed")] public bool RightPanelCollapsed {get;set;}=false;
}
