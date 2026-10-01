using System.IO;
using System.Text.Json;
using TagWriter.Models;

namespace TagWriter.Services;

public static class AppSettingsStore {
 static readonly JsonSerializerOptions Options=new(){WriteIndented=true,PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
 static string DirectoryPath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TagWriter");
 public static string SettingsPath=>Path.Combine(DirectoryPath,"settings.json");

 public static AppSettings Load(){
  try{
   if(!File.Exists(SettingsPath))return new AppSettings();
   var json=File.ReadAllText(SettingsPath);
   return JsonSerializer.Deserialize<AppSettings>(json,Options)??new AppSettings();
  }catch{return new AppSettings();}
 }

 public static void Save(AppSettings settings){
  try{
   Directory.CreateDirectory(DirectoryPath);
   var tmp=SettingsPath+".tmp";
   File.WriteAllText(tmp,JsonSerializer.Serialize(settings,Options));
   File.Move(tmp,SettingsPath,true);
  }catch{}
 }
}
