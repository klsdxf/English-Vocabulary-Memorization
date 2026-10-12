using System;
using System.Collections.Generic;
using System.Linq;

namespace CET6Word {
 public class Example { public string English {get;set;} public string Chinese {get;set;} }
 public class Word { public string Id {get;set;} public string English {get;set;} public string Meaning {get;set;} public List<Example> Examples {get;set;} = new List<Example>(); }
 public class Vocabulary { public string Id {get;set;} public string Name {get;set;} public string Source {get;set;} public bool BuiltIn {get;set;} public List<Word> Words {get;set;} = new List<Word>(); }
 public class Progress { public bool Mastered {get;set;} public long LearnedAt {get;set;} public int LearnCount {get;set;} }
 public class LibraryPosition { public string Mode {get;set;} = "fixed"; public List<string> RandomOrder {get;set;} = new List<string>(); public string FixedAnchor {get;set;} public string RandomAnchor {get;set;} }
 public class Notebook { public string Id {get;set;} public string Name {get;set;} public List<string> Words {get;set;} = new List<string>(); }
 public class ReviewEvent { public string Action {get;set;} public long At {get;set;} }
 public class ErrorReview { public bool Pending {get;set;} public int Streak {get;set;} public int WrongCount {get;set;} public int PracticeCount {get;set;} public List<ReviewEvent> Events {get;set;} = new List<ReviewEvent>(); }
 public class Attempt { public string WordId {get;set;} public string Answer {get;set;} public bool Correct {get;set;} public long At {get;set;} }
 public class Session { public string Id {get;set;} public string VocabularyId {get;set;} public string Mode {get;set;} public bool Listening {get;set;} public long Started {get;set;} public long Finished {get;set;} public bool AwaitingNext {get;set;} public List<string> Words {get;set;} = new List<string>(); public List<Attempt> Attempts {get;set;} = new List<Attempt>(); }
 public class Preferences { public bool Translation {get;set;} public bool AutoRead {get;set;} public int ErrorThreshold {get;set;} = 2; public string AzureRegion {get;set;} = ""; }
 public class AppState {
  public int SchemaVersion {get;set;} = 1;
  public string CurrentVocabulary {get;set;}
  public List<Vocabulary> Vocabularies {get;set;} = new List<Vocabulary>();
  public Dictionary<string, Progress> Progress {get;set;} = new Dictionary<string, Progress>();
  public Dictionary<string, LibraryPosition> Positions {get;set;} = new Dictionary<string, LibraryPosition>();
  public List<Notebook> Notebooks {get;set;} = new List<Notebook>();
  public Dictionary<string, ErrorReview> Errors {get;set;} = new Dictionary<string, ErrorReview>();
  public List<Session> Sessions {get;set;} = new List<Session>();
  public string ActiveSession {get;set;}
  public Preferences Settings {get;set;} = new Preferences();
 }
 // Unconfirmed choices are kept together so they can be changed without redesigning storage.
 public static class Policy {
  public static bool IncludeMasteredInQuiz = true;
  public static bool AnswerMatches(string expected, string answer) { return String.Equals(expected.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase); }
  public static int NextExpansion(int level, bool globalTranslation) { return globalTranslation ? (level == 2 ? 0 : 2) : ((level + 1) % 3); }
 }
 public class LearningEngine {
  public AppState State {get; private set;}
  readonly Random random;
  public LearningEngine(AppState state, int? seed = null) { State = state; random = seed.HasValue ? new Random(seed.Value) : new Random(); }
  public Vocabulary Current {get { return State.Vocabularies.Single(v => v.Id == State.CurrentVocabulary); }}
  public Word Find(string id) { return State.Vocabularies.SelectMany(v => v.Words).First(w => w.Id == id); }
  public Progress GetProgress(string id) { Progress p; if (!State.Progress.TryGetValue(id, out p)) State.Progress[id] = p = new Progress(); return p; }
  public LibraryPosition Position {get { LibraryPosition p; if (!State.Positions.TryGetValue(Current.Id, out p)) State.Positions[Current.Id] = p = new LibraryPosition(); return p; }}
  public void Learn(string id) { var p = GetProgress(id); p.LearnedAt = Math.Max(DateTime.UtcNow.Ticks, State.Progress.Values.Select(x => x.LearnedAt).DefaultIfEmpty(0).Max() + 1); p.LearnCount++; }
  public void Master(string id, bool mastered) { GetProgress(id).Mastered = mastered; }
  public void ToggleFavorite(string id) { var b = State.Notebooks.Single(n => n.Id == "default"); if (b.Words.Contains(id)) b.Words.Remove(id); else b.Words.Add(id); }
  public bool Favorite(string id) { return State.Notebooks.Single(n => n.Id == "default").Words.Contains(id); }
  public Notebook AddNotebook(string name) { name = CheckName(name); if (State.Notebooks.Any(b => b.Name == name)) throw new ArgumentException("单词本名称已存在。"); var b = new Notebook {Id = Guid.NewGuid().ToString("N"), Name = name}; State.Notebooks.Add(b); return b; }
  public void RenameNotebook(Notebook b, string name) { if (b.Id == "default") throw new ArgumentException("默认收藏不能重命名。"); name = CheckName(name); if(State.Notebooks.Any(n=>n.Id!=b.Id && n.Name==name)) throw new ArgumentException("单词本名称已存在。"); b.Name=name; }
  public void DeleteNotebook(Notebook b) { if (b.Id == "default") throw new ArgumentException("默认收藏不能删除。"); State.Notebooks.Remove(b); }
  public void PutInNotebook(Notebook b, string id) { if (!b.Words.Contains(id)) b.Words.Add(id); }
  static string CheckName(string name) { if (String.IsNullOrWhiteSpace(name) || name.Trim().Length > 80) throw new ArgumentException("名称需为 1–80 个字符。"); return name.Trim(); }
  public List<Word> StudyWords() {
   var words = Current.Words;
   if (Position.Mode == "random") {
    var valid = new HashSet<string>(words.Select(w => w.Id));
    Position.RandomOrder = Position.RandomOrder.Where(valid.Contains).Distinct().ToList();
    if(Position.RandomOrder.Count != words.Count) Position.RandomOrder = Shuffle(words.Select(w=>w.Id).ToList());
    var byId = words.ToDictionary(w => w.Id);
    words = Position.RandomOrder.Select(id=>byId[id]).ToList();
   }
   return words.Where(w=>!GetProgress(w.Id).Mastered).ToList();
  }
  public void Sort(string mode, string anchor) { if(mode!="fixed" && mode!="random") throw new ArgumentException("无效排序。"); SetAnchor(anchor); Position.Mode=mode; SetAnchor(anchor); StudyWords(); }
  public string Anchor {get {return Position.Mode == "random" ? Position.RandomAnchor : Position.FixedAnchor;}}
  public void SetAnchor(string id) { if(Position.Mode=="random") Position.RandomAnchor=id; else Position.FixedAnchor=id; }
  public List<T> Shuffle<T>(List<T> items) { var result = new List<T>(items); for(int i=result.Count-1;i>0;i--) {int j=random.Next(i+1);var t=result[i];result[i]=result[j];result[j]=t;}return result; }
  public List<string> Candidates(string source) {
   var ids = Current.Words.Where(w=>Policy.IncludeMasteredInQuiz || !GetProgress(w.Id).Mastered).Select(w=>w.Id).ToList();
   if (source=="recent" || source=="learned") return ids.Where(id=>GetProgress(id).LearnedAt>0).OrderByDescending(id=>GetProgress(id).LearnedAt).ToList();
   if (source=="all") return ids;
   if (source=="errors") return ids.Where(id=>State.Errors.ContainsKey(id) && State.Errors[id].Pending).ToList();
   var book=State.Notebooks.SingleOrDefault(b=>b.Id==source); if(book==null) throw new ArgumentException("所选单词本不存在。");
   return ids.Where(book.Words.Contains).ToList();
  }
  public Session Start(string source, int count, bool listening, bool recent = false) { if(count<1 || count>10000) throw new ArgumentException("题数需为 1–10000。"); var ids=Candidates(source); if(!recent) ids=Shuffle(ids); return StartWith(ids.Take(count).ToList(),listening,source); }
  public Session StartWith(List<string> ids,bool listening,string mode) {
   if(ids==null || ids.Count==0) throw new ArgumentException("该范围没有可默写的词。请先主动展开词条或收藏单词。");
   if(Active!=null && Active.Finished==0) throw new InvalidOperationException("请先继续或结束未完成的默写。");
   var s=new Session {Id=Guid.NewGuid().ToString("N"), VocabularyId=Current.Id, Mode=mode, Listening=listening, Started=DateTime.UtcNow.Ticks, Words=ids.Distinct().ToList()};
   foreach(var id in s.Words) Find(id);
   State.Sessions.Add(s);State.ActiveSession=s.Id;return s;
  }
  public Session Active {get {return State.Sessions.FirstOrDefault(s=>s.Id==State.ActiveSession);}}
  public Word Question {get {var s=Active; int i=s.Attempts.Count-(s.AwaitingNext?1:0); return Find(s.Words[i]);}}
  public Attempt Submit(string answer) {
   var s=Active; if(s==null || s.Finished>0 || s.AwaitingNext) throw new InvalidOperationException("此题已提交或本轮已结束。");
   if(String.IsNullOrWhiteSpace(answer)) throw new ArgumentException("请输入英文后提交。"); if(answer.Length>1000) throw new ArgumentException("输入内容过长。");
   var w=Question;var a=new Attempt {WordId=w.Id,Answer=answer,Correct=Policy.AnswerMatches(w.English,answer),At=DateTime.UtcNow.Ticks};s.Attempts.Add(a);s.AwaitingNext=true;
   ErrorReview e; State.Errors.TryGetValue(w.Id,out e);
   if(!a.Correct) { if(e==null) State.Errors[w.Id]=e=new ErrorReview(); e.WrongCount++; e.Streak=0;e.Pending=true;e.Events.Add(new ReviewEvent {Action="wrong",At=a.At}); }
   else if(e!=null) { e.Streak++; if(e.Pending && e.Streak>=State.Settings.ErrorThreshold) {e.Pending=false;e.Events.Add(new ReviewEvent {Action="threshold",At=a.At});} }
   if(e!=null) e.PracticeCount++;
   return a;
  }
  public bool Next() {var s=Active;if(!s.AwaitingNext) return false;s.AwaitingNext=false;if(s.Attempts.Count==s.Words.Count){s.Finished=DateTime.UtcNow.Ticks;return false;}return true;}
  public List<string> Wrong(Session s) {return s.Attempts.Where(a=>!a.Correct).Select(a=>a.WordId).Distinct().ToList();}
  public void EndSession() {if(Active!=null && Active.Finished==0) Active.Finished=DateTime.UtcNow.Ticks;State.ActiveSession=null;}
  public void Review(string id,bool pending) { var e=State.Errors[id];e.Pending=pending;if(pending)e.Streak=0;e.Events.Add(new ReviewEvent {Action=pending?"restore":"remove",At=DateTime.UtcNow.Ticks}); }
 }
}
