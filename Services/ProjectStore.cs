using System.IO; using System.Text.Json; using System.Text.Json.Serialization; using TagWriter.Models;
namespace TagWriter.Services;
public static class ProjectStore {
 static readonly JsonSerializerOptions Options=new(){WriteIndented=true,PropertyNamingPolicy=JsonNamingPolicy.CamelCase,Converters={new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)}};
 public static async Task<ProjectDocument> LoadAsync(string path){
  await using var s=File.OpenRead(path);
  var d=await JsonSerializer.DeserializeAsync<ProjectDocument>(s,Options)??throw new InvalidDataException("TagWriter 프로젝트를 읽을 수 없습니다.");
  if(!string.Equals(d.Format,"tagwriter",StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("TagWriter 프로젝트 형식이 아닙니다.");
  return d;
 }
 public static async Task SaveAsync(string path,ProjectDocument d){
  d.Revision++; d.Project.UpdatedAt=DateTimeOffset.Now; var tmp=path+".tmp";
  await using(var s=File.Create(tmp)) await JsonSerializer.SerializeAsync(s,d,Options);
  File.Move(tmp,path,true);
 }
}