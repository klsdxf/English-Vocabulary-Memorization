using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace CET6Word {
 public class BackupEnvelope { public string Format {get;set;} public int Version {get;set;} public string CreatedUtc {get;set;} public string Sha256 {get;set;} public string Data {get;set;} }
 public class StateStore : IDisposable {
  public string DirectoryPath {get;private set;}
  public string FilePath {get {return Path.Combine(DirectoryPath,"state.json");}}
  public string RecoveryNotice {get;private set;}
  FileStream instanceLock;
  public StateStore(string path,bool lockInstance=true) { DirectoryPath=Path.GetFullPath(path);Directory.CreateDirectory(DirectoryPath);if(lockInstance) try {instanceLock=new FileStream(Path.Combine(DirectoryPath,"instance.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException){throw new IOException("此数据目录已有程序在运行，请使用已打开的窗口。");} }
  public static JavaScriptSerializer Json() {return new JavaScriptSerializer {MaxJsonLength=128*1024*1024,RecursionLimit=120};}
  public static string Encode(AppState state) {return Json().Serialize(state);}
  static object Ordered(object value){var dictionary=value as IDictionary<string,object>;if(dictionary!=null){var sorted=new SortedDictionary<string,object>(StringComparer.Ordinal);foreach(var item in dictionary)sorted[item.Key]=Ordered(item.Value);return sorted;}var sequence=value as IEnumerable;if(sequence!=null && !(value is string))return sequence.Cast<object>().Select(Ordered).ToArray();return value;}
  public static bool Equivalent(string first,string second){return Json().Serialize(Ordered(Json().DeserializeObject(first)))==Json().Serialize(Ordered(Json().DeserializeObject(second)));}
  public static AppState Decode(string data) {var state=Json().Deserialize<AppState>(data);if(state!=null)UpgradeMigration.NormalizeLegacy(state,data);Validate(state);return state;}
  public AppState Load(Func<AppState> create) {
   if(!File.Exists(FilePath)){var initial=create();Save(initial);return initial;}
   try {return Decode(ReadBounded(FilePath));}
   catch(Exception ex) {
    string previous=FilePath+".previous";
    if(File.Exists(previous)) {
     try {var s=Decode(ReadBounded(previous));string damaged=Path.Combine(DirectoryPath,"damaged-"+DateTime.Now.ToString("yyyyMMdd-HHmmssfff")+".json");File.Copy(FilePath,damaged);File.Copy(previous,FilePath,true);RecoveryNotice="主档案损坏，已恢复上一份安全副本。损坏原件保留在数据目录。";return s;}catch(Exception recovery) {throw new IOException("档案和安全副本都无法读取。数据已保留，请从完整备份恢复。",recovery);}
    }
    throw new IOException("档案无法读取，原数据已保留。请检查数据目录或使用完整备份恢复。",ex);
   }
  }
  public void Save(AppState state) {Validate(state);AtomicWrite(FilePath,Encode(state),true);}
  public static void AtomicWrite(string path,string text,bool previous) {
   string tmp=path+".tmp-"+Guid.NewGuid().ToString("N");
   try {using(var stream=new FileStream(tmp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){byte[] bytes=new UTF8Encoding(false).GetBytes(text);stream.Write(bytes,0,bytes.Length);stream.Flush(true);}if(File.Exists(path))File.Replace(tmp,path,previous?path+".previous":null,true);else File.Move(tmp,path);}
   finally {if(File.Exists(tmp))File.Delete(tmp);}
  }
  public static string ReadBounded(string path) {if(new FileInfo(path).Length>128L*1024*1024)throw new InvalidDataException("文件超过 128 MB 限制。");return File.ReadAllText(path,Encoding.UTF8);}
  static string Hash(string text) {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
  public void Export(string path,AppState state) {
   if(Path.GetFullPath(path).Equals(FilePath,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("请勿覆盖正在使用的档案。");
   ExportRaw(path,Encode(state));
  }
  public void ExportRaw(string path,string data){if(Path.GetFullPath(path).Equals(FilePath,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("请勿覆盖正在使用的档案。");Decode(data);var b=new BackupEnvelope {Format="CET6Word.FullBackup",Version=1,CreatedUtc=DateTime.UtcNow.ToString("o"),Sha256=Hash(data),Data=data};AtomicWrite(path,Json().Serialize(b),false);}
  public AppState ReadBackup(string path) {var b=Json().Deserialize<BackupEnvelope>(ReadBounded(path));if(b==null || b.Format!="CET6Word.FullBackup" || b.Version!=1 || b.Data==null || b.Sha256!=Hash(b.Data))throw new InvalidDataException("备份格式、版本或校验码不正确。");return Decode(b.Data);}
  public void Restore(AppState restored) {Validate(restored);if(File.Exists(FilePath))File.Copy(FilePath,Path.Combine(DirectoryPath,"before-restore-"+DateTime.Now.ToString("yyyyMMdd-HHmmssfff")+".json"));Save(restored);}
  public static void Validate(AppState s) {
   if(s==null || (s.SchemaVersion!=1 && s.SchemaVersion!=2))throw new InvalidDataException("不支持的档案版本。");
   if(s.Settings==null || s.Vocabularies==null || s.Progress==null || s.Positions==null || s.Notebooks==null || s.Errors==null || s.Sessions==null)throw new InvalidDataException("档案缺少必要的数据。");
   if(s.Settings.ErrorThreshold<1 || s.Settings.ErrorThreshold>100)throw new InvalidDataException("错词阈值需为 1–100。");
   if(s.Vocabularies.Count<1 || s.Vocabularies.Count>1000)throw new InvalidDataException("词库数量异常。");
   var vocabIds=new HashSet<string>();var words=new HashSet<string>();var vocabWords=new Dictionary<string,HashSet<string>>();var wordIndex=new Dictionary<string,Word>();int total=0;
   foreach(var v in s.Vocabularies) {
    if(v==null || String.IsNullOrWhiteSpace(v.Id) || !vocabIds.Add(v.Id) || String.IsNullOrWhiteSpace(v.Name) || v.Words==null || v.Words.Count==0)throw new InvalidDataException("词库内容无效。");
    total+=v.Words.Count;if(total>100000)throw new InvalidDataException("档案最多支持 10 万词条。");var set=new HashSet<string>();vocabWords[v.Id]=set;
    foreach(var w in v.Words) {if(w==null || String.IsNullOrWhiteSpace(w.Id) || !words.Add(w.Id) || String.IsNullOrWhiteSpace(w.English) || w.English.Length>200 || String.IsNullOrWhiteSpace(w.Meaning) || w.Meaning.Length>20000 || w.Examples==null)throw new InvalidDataException("词条无效或包含重复标识。");set.Add(w.Id);wordIndex[w.Id]=w;if(w.Examples.Count>100 || w.Examples.Any(e=>e==null || String.IsNullOrWhiteSpace(e.English) || e.English.Length>20000 || (e.Chinese!=null && e.Chinese.Length>20000)))throw new InvalidDataException("例句内容无效。");}
   }
   if(!vocabIds.Contains(s.CurrentVocabulary))throw new InvalidDataException("当前词库不存在。");
   if(s.Progress.Any(p=>!words.Contains(p.Key) || p.Value==null || p.Value.LearnedAt<0 || p.Value.LearnedAt>DateTime.MaxValue.Ticks || p.Value.LearnCount<0))throw new InvalidDataException("学习进度关联无效。");
   foreach(var p in s.Positions) {if(!vocabIds.Contains(p.Key) || p.Value==null || (p.Value.Mode!="fixed" && p.Value.Mode!="random") || p.Value.RandomOrder==null)throw new InvalidDataException("位置记录无效。");var allowed=vocabWords[p.Key];if(p.Value.RandomOrder.Distinct().Count()!=p.Value.RandomOrder.Count || p.Value.RandomOrder.Any(id=>!allowed.Contains(id)) || (!String.IsNullOrEmpty(p.Value.FixedAnchor) && !allowed.Contains(p.Value.FixedAnchor)) || (!String.IsNullOrEmpty(p.Value.RandomAnchor) && !allowed.Contains(p.Value.RandomAnchor)))throw new InvalidDataException("排序或锚点关联无效。");}
   var books=new HashSet<string>();foreach(var b in s.Notebooks)if(b==null || String.IsNullOrWhiteSpace(b.Id) || !books.Add(b.Id) || String.IsNullOrWhiteSpace(b.Name) || b.Words==null || b.Words.Distinct().Count()!=b.Words.Count || b.Words.Any(id=>!words.Contains(id)))throw new InvalidDataException("单词本关联无效。");
   if(!books.Contains("default"))throw new InvalidDataException("默认收藏缺失。");
   foreach(var e in s.Errors)if(!words.Contains(e.Key) || e.Value==null || e.Value.Streak<0 || e.Value.WrongCount<1 || e.Value.PracticeCount<e.Value.WrongCount || e.Value.Events==null || e.Value.Events.Any(x=>x==null || x.At<0 || x.Action==null))throw new InvalidDataException("错词记录无效。");
   var sessions=new HashSet<string>();foreach(var q in s.Sessions){
    if(q!=null && q.LegacyIncomplete && !String.IsNullOrEmpty(q.LegacyRecordJson)){if(String.IsNullOrEmpty(q.Id) || !sessions.Add(q.Id))throw new InvalidDataException("旧轮次标识无效。");continue;}
    if(q==null || String.IsNullOrEmpty(q.Id) || !sessions.Add(q.Id) || !vocabIds.Contains(q.VocabularyId) || q.Words==null || q.Words.Count==0 || q.Attempts==null || q.Attempts.Count>q.Words.Count || q.Words.Distinct().Count()!=q.Words.Count || q.Words.Any(id=>!vocabWords[q.VocabularyId].Contains(id)) || (q.AwaitingNext && q.Attempts.Count==0) || (q.Finished==0 && q.Attempts.Count==q.Words.Count && !q.AwaitingNext))throw new InvalidDataException("默写轮次无效。");for(int i=0;i<q.Attempts.Count;i++){var a=q.Attempts[i];if(a==null || a.WordId!=q.Words[i] || a.Answer==null || a.At<0 || !a.ResultKnown || a.Correct!=Policy.AnswerMatches(wordIndex[a.WordId].English,a.Answer))throw new InvalidDataException("默写作答记录无效。");}}
   if(s.SchemaVersion==2){if(s.Reading==null || s.Reading.Positions==null || s.Migrations==null || new[]{s.Reading.StudySize,s.Reading.OriginalSize,s.Reading.FavoriteSize,s.Reading.RecitedSize}.Any(n=>n<1 || n>200) || s.Reading.Positions.Any(p=>p.Value==null || p.Value.Page<1 || p.Value.InPageOffset<0 || (p.Value.Anchor!=null && !words.Contains(p.Value.Anchor))))throw new InvalidDataException("分页档案无效。");}
   if(s.ActiveSession!=null && !sessions.Contains(s.ActiveSession))throw new InvalidDataException("当前默写轮次缺失。");
  }
  public void SaveKey(string key) {string path=Path.Combine(DirectoryPath,"speech-key.bin");byte[] data=ProtectedData.Protect(Encoding.UTF8.GetBytes(key??""),null,DataProtectionScope.CurrentUser);File.WriteAllBytes(path,data);}
  public string ReadKey() {try {string path=Path.Combine(DirectoryPath,"speech-key.bin");return File.Exists(path)?Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser)):"";}catch{return "";}}
  public void Dispose() {if(instanceLock!=null)instanceLock.Dispose();}
 }
}
