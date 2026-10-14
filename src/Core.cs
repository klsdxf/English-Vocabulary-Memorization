using System;
using System.Collections.Generic;
using System.Linq;

namespace CET6Word {
 public class Example { public string English {get;set;} public string Chinese {get;set;} }
 public class Sense { public string PartOfSpeech {get;set;} public string Definition {get;set;} public string UsIpa {get;set;} public List<int> ExampleIndexes {get;set;} = new List<int>(); }
 public class Word { public string Id {get;set;} public string English {get;set;} public string Meaning {get;set;} public string PartOfSpeech {get;set;} public string UsIpa {get;set;} public int OriginalIndex {get;set;} public bool Formal {get;set;} public string Source {get;set;} public string PronunciationSource {get;set;} public string SourceKey {get;set;} public List<Sense> Senses {get;set;} = new List<Sense>(); public List<Example> Examples {get;set;} = new List<Example>(); }
 public class Vocabulary { public string Id {get;set;} public string Name {get;set;} public string Source {get;set;} public bool BuiltIn {get;set;} public string Edition {get;set;} public List<Word> Words {get;set;} = new List<Word>(); }
 public class Progress { public bool Mastered {get;set;} public long MasteredAt {get;set;} public long LearnedAt {get;set;} public int LearnCount {get;set;} }
 public class ReadingPosition {public int Page {get;set;}=1;public string Anchor {get;set;} public int InPageOffset {get;set;} }
 public class ReadingPreferences {public int StudySize {get;set;}=20;public int OriginalSize {get;set;}=20;public int FavoriteSize {get;set;}=20;public int RecitedSize {get;set;}=20;public string RecitedFilter {get;set;}="all";public string RecitedSort {get;set;}="recent";public Dictionary<string,ReadingPosition> Positions {get;set;}=new Dictionary<string,ReadingPosition>();}
 public class MigrationRecord {public string Edition {get;set;} public int Matched {get;set;} public int Unmatched {get;set;} public int Added {get;set;} public List<string> Conflicts {get;set;}=new List<string>();public string PreviousSha256 {get;set;}public string BackupFile {get;set;}public List<string> LegacySessionIds {get;set;}=new List<string>(); }
 public class LibraryPosition { public string Mode {get;set;} = "fixed"; public List<string> RandomOrder {get;set;} = new List<string>(); public string FixedAnchor {get;set;} public string RandomAnchor {get;set;} }
 public class Notebook { public string Id {get;set;} public string Name {get;set;} public List<string> Words {get;set;} = new List<string>(); }
 public class ReviewEvent { public string Action {get;set;} public long At {get;set;} }
 public class ErrorReview { public bool Pending {get;set;} public int Streak {get;set;} public int WrongCount {get;set;} public int PracticeCount {get;set;} public List<ReviewEvent> Events {get;set;} = new List<ReviewEvent>(); }
 public class Attempt { public string WordId {get;set;} public string Answer {get;set;} public bool Correct {get;set;} public bool ResultKnown {get;set;}=true;public long At {get;set;} }
 public class Session { public string Id {get;set;} public string VocabularyId {get;set;} public string Mode {get;set;} public bool Listening {get;set;} public long Started {get;set;} public long Finished {get;set;} public bool AwaitingNext {get;set;} public bool LegacyIncomplete {get;set;} public string LegacyRecordJson {get;set;} public List<string> Words {get;set;} = new List<string>(); public List<Attempt> Attempts {get;set;} = new List<Attempt>(); }
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
  public ReadingPreferences Reading {get;set;} = new ReadingPreferences();
  public List<MigrationRecord> Migrations {get;set;} = new List<MigrationRecord>();
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
  public void Master(string id, bool mastered) { var p=GetProgress(id);if(mastered && !p.Mastered)p.MasteredAt=DateTime.UtcNow.Ticks;p.Mastered=mastered; }
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
    var present=new HashSet<string>(Position.RandomOrder);var missing=words.Select(w=>w.Id).Where(id=>!present.Contains(id)).ToList();
    if(Position.RandomOrder.Count==0)Position.RandomOrder=Shuffle(missing);else Position.RandomOrder.AddRange(Shuffle(missing));
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
  public List<string> Wrong(Session s) {return (s.Attempts??new List<Attempt>()).Where(a=>a.ResultKnown && !a.Correct && a.WordId!=null).Select(a=>a.WordId).Distinct().ToList();}
  public void EndSession() {if(Active!=null && Active.Finished==0) Active.Finished=DateTime.UtcNow.Ticks;State.ActiveSession=null;}
  public void Review(string id,bool pending) { var e=State.Errors[id];e.Pending=pending;if(pending)e.Streak=0;e.Events.Add(new ReviewEvent {Action=pending?"restore":"remove",At=DateTime.UtcNow.Ticks}); }
  public Vocabulary OriginalBook {get{return State.Vocabularies.Single(v=>v.Id=="builtin-cet6-original-v1");}}
  public List<Word> OriginalWords(){return OriginalBook.Words.Where(w=>w.Formal || OriginalBook.Edition==null).ToList();}
  public ReadingPosition ReadingAt(string key){ReadingPosition p;if(!State.Reading.Positions.TryGetValue(key,out p))State.Reading.Positions[key]=p=new ReadingPosition();return p;}
  public static string StudyKey(string vocabulary,string mode){return "study:"+vocabulary+":"+mode;}
  public List<Word> SearchEnglish(string query){query=(query??"").Trim();if(query.Length==0 || query.Any(c=>c>127 || (Char.IsLetter(c) && !(c>='a'&&c<='z'||c>='A'&&c<='Z'))))return new List<Word>();return OriginalWords().Where(w=>w.English.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToList();}
  public Dictionary<string,RecitationEvidence> EvidenceMap(){
   var latest=new Dictionary<string,Attempt>();var latestSession=new Dictionary<string,string>();var unknown=new Dictionary<string,List<Tuple<string,long>>>();
   var captured=new HashSet<string>((State.Migrations??new List<MigrationRecord>()).SelectMany(m=>m.LegacySessionIds??new List<string>()));
   Action<string,string,long> uncertain=(id,session,at)=>{if(id==null)return;List<Tuple<string,long>> bounds;if(!unknown.TryGetValue(id,out bounds))unknown[id]=bounds=new List<Tuple<string,long>>();bounds.Add(Tuple.Create(session,at));};
   foreach(var session in State.Sessions){
    foreach(var a in session.Attempts??new List<Attempt>()){if(a==null || a.WordId==null)continue;Attempt prior;if(!a.ResultKnown || a.At<=0){uncertain(a.WordId,session.Id,a.At>0?a.At:session.Finished);continue;}if(!latest.TryGetValue(a.WordId,out prior)||a.At>=prior.At){latest[a.WordId]=a;latestSession[a.WordId]=session.Id;}}
    if(session.LegacyIncomplete && (session.Attempts==null || session.Attempts.Count==0 || session.Attempts.Any(a=>a==null || a.WordId==null)))foreach(var id in session.Words??new List<string>())uncertain(id,session.Id,session.Finished);
   }
   var result=new Dictionary<string,RecitationEvidence>();
   foreach(var w in OriginalBook.Words){Progress p;State.Progress.TryGetValue(w.Id,out p);Attempt a;latest.TryGetValue(w.Id,out a);string recentSession;latestSession.TryGetValue(w.Id,out recentSession);List<Tuple<string,long>> bounds;bool unsure=unknown.TryGetValue(w.Id,out bounds)&&bounds.Any(bound=>{
     // A committed migration captured these exact historical sessions. A new reliable
     // session created afterwards can supersede them without assigning any old timestamp.
     if(recentSession!=null&&captured.Contains(bound.Item1)&&!captured.Contains(recentSession))return false;
     return bound.Item2<=0 || a==null || bound.Item2>=a.At;
    });bool? correct=unsure||a==null?(bool?)null:a.Correct;bool mastered=p!=null&&p.Mastered;result[w.Id]=new RecitationEvidence{WordId=w.Id,Mastered=mastered,LastCorrect=correct,SuccessAt=Math.Max(mastered?p.MasteredAt:0,correct==true?a.At:0)};}
   return result;
  }
  public RecitationEvidence Evidence(string id){RecitationEvidence e;return EvidenceMap().TryGetValue(id,out e)?e:new RecitationEvidence{WordId=id};}
  public List<Word> Recited(string filter="all",string sort="recent"){
   var evidence=EvidenceMap();var entries=OriginalBook.Words.Select((w,i)=>new {Word=w,Index=i,Evidence=evidence[w.Id]}).Where(x=>filter=="mastered"?x.Evidence.Mastered:filter=="correct"?x.Evidence.LastCorrect==true:x.Evidence.Mastered || x.Evidence.LastCorrect==true);
   if(sort=="original")return entries.Select(x=>x.Word).ToList();return entries.OrderByDescending(x=>x.Evidence.SuccessAt).ThenBy(x=>x.Index).Select(x=>x.Word).ToList();
  }
 }
 public class RecitationEvidence {public string WordId {get;set;}public bool Mastered {get;set;}public bool? LastCorrect {get;set;}public long SuccessAt {get;set;}}
 public class WordPage {
  public List<Word> Words {get;set;}public int Page {get;set;}public int TotalPages {get;set;}public int TotalWords {get;set;}public int Offset {get;set;}
  public static WordPage Build(List<Word> words,int size,ReadingPosition position,bool useAnchor=true){if(size<1 || size>200)throw new ArgumentException("每页词数需为 1–200。");int total=Math.Max(1,(words.Count+size-1)/size);int page=Math.Max(1,Math.Min(total,position.Page));int anchor=useAnchor && position.Anchor!=null?words.FindIndex(w=>w.Id==position.Anchor):-1;int offset=Math.Max(0,Math.Min(size-1,position.InPageOffset));if(anchor>=0){page=anchor/size+1;offset=anchor%size;}position.Page=page;var slice=words.Skip((page-1)*size).Take(size).ToList();if(slice.Count==0)offset=0;else offset=Math.Min(offset,slice.Count-1);return new WordPage{Words=slice,Page=page,TotalPages=total,TotalWords=words.Count,Offset=offset};}
  public static void Jump(ReadingPosition position,int number,int total){if(number<1 || number>total)throw new ArgumentException("页码需在 1–"+total+" 之间。");position.Page=number;position.Anchor=null;position.InPageOffset=0;}
 }
}
