using System.Text.Json.Serialization;
namespace TagWriter.Models;
public sealed class ProjectDocument {
 [JsonPropertyName("format")] public string Format {get;set;}="tagwriter";
 [JsonPropertyName("formatVersion")] public int FormatVersion {get;set;}=1;
 [JsonPropertyName("revision")] public long Revision {get;set;}=1;
 [JsonPropertyName("project")] public ProjectInfo Project {get;set;}=new();
 [JsonPropertyName("chapters")] public List<Chapter> Chapters {get;set;}=[];
 [JsonPropertyName("cards")] public List<EntityCard> Cards {get;set;}=[];
 [JsonPropertyName("mindmap")] public MindMap MindMap {get;set;}=new();
 [JsonPropertyName("ideas")] public List<Idea> Ideas {get;set;}=[];
 [JsonPropertyName("settings")] public ProjectSettings Settings {get;set;}=new();
 public static ProjectDocument CreateSample()=>new(){Project=new(){Title="새 소설"},Chapters=[new(){Id="chapter_001",Title="1장",Order=1,Scenes=[new(){Id="scene_001",Title="Scene 1",Order=1}]}]};
}
public sealed class ProjectInfo {
 [JsonPropertyName("title")] public string Title {get;set;}="새 소설";
 [JsonPropertyName("author")] public string Author {get;set;}="";
 [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt {get;set;}=DateTimeOffset.Now;
 [JsonPropertyName("updatedAt")] public DateTimeOffset UpdatedAt {get;set;}=DateTimeOffset.Now;
}
public sealed class Chapter {
 [JsonPropertyName("id")] public string Id {get;set;}=Guid.NewGuid().ToString("N");
 [JsonPropertyName("title")] public string Title {get;set;}="새 챕터";
 [JsonPropertyName("order")] public int Order {get;set;}
 [JsonPropertyName("scenes")] public List<Scene> Scenes {get;set;}=[];
}
public sealed class Scene {
 [JsonPropertyName("id")] public string Id {get;set;}=Guid.NewGuid().ToString("N");
 [JsonPropertyName("title")] public string Title {get;set;}="Scene";
 [JsonPropertyName("order")] public int Order {get;set;}
 [JsonPropertyName("content")] public string Content {get;set;}="";
}
public enum CardType { Character, Location, Item, Event }
public sealed class EntityCard {
 [JsonPropertyName("id")] public string Id {get;set;}=Guid.NewGuid().ToString("N");
 [JsonPropertyName("type")] public CardType Type {get;set;}
 [JsonPropertyName("name")] public string Name {get;set;}="";
 [JsonPropertyName("description")] public string Description {get;set;}="";
 [JsonPropertyName("aliases")] public List<string> Aliases {get;set;}=[];
 [JsonPropertyName("updatedAt")] public DateTimeOffset UpdatedAt {get;set;}=DateTimeOffset.Now;
}
public sealed class MindMap {
 [JsonPropertyName("nodes")] public List<MapNode> Nodes {get;set;}=[];
 [JsonPropertyName("edges")] public List<MapEdge> Edges {get;set;}=[];
}
public sealed class MapNode {
 [JsonPropertyName("cardId")] public string CardId {get;set;}="";
 [JsonPropertyName("x")] public double X {get;set;}
 [JsonPropertyName("y")] public double Y {get;set;}
}
public sealed class MapEdge {
 [JsonPropertyName("id")] public string Id {get;set;}=Guid.NewGuid().ToString("N");
 [JsonPropertyName("from")] public string From {get;set;}="";
 [JsonPropertyName("to")] public string To {get;set;}="";
 [JsonPropertyName("label")] public string Label {get;set;}="";
 [JsonPropertyName("source")] public string Source {get;set;}="manual";
 [JsonPropertyName("userEdited")] public bool UserEdited {get;set;}
}
public sealed class Idea {
 [JsonPropertyName("id")] public string Id {get;set;}=Guid.NewGuid().ToString("N");
 [JsonPropertyName("text")] public string Text {get;set;}="";
 [JsonPropertyName("links")] public List<string> Links {get;set;}=[];
 [JsonPropertyName("done")] public bool Done {get;set;}
 [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt {get;set;}=DateTimeOffset.Now;
 [JsonPropertyName("updatedAt")] public DateTimeOffset UpdatedAt {get;set;}=DateTimeOffset.Now;
}
public sealed class ProjectSettings {
 [JsonPropertyName("theme")] public string Theme {get;set;}="dark";
 [JsonPropertyName("zoom")] public double Zoom {get;set;}=1.0;
 [JsonPropertyName("editorFontFamily")] public string EditorFontFamily {get;set;}="Malgun Gothic";
 [JsonPropertyName("editorFontSize")] public double EditorFontSize {get;set;}=17;
 [JsonPropertyName("typewriterSoundEnabled")] public bool TypewriterSoundEnabled {get;set;}=false;
 [JsonPropertyName("typewriterVolume")] public double TypewriterVolume {get;set;}=35;
}