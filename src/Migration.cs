using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CET6Word {
 public static class UpgradeMigration {
  public const string OriginalId="builtin-cet6-original-v1";
  public static string Sha256(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
  static string Canon(string value){return (value??"").Trim().ToLowerInvariant();}
  // Retain original JSON for incomplete historical sessions. Missing results stay unknown.
  public static void NormalizeLegacy(AppState state,string raw){
   if(state.SchemaVersion!=1)return;
   var root=StateStore.Json().DeserializeObject(raw) as Dictionary<string,object>;object rawSessions;
   if(root==null || !root.TryGetValue("Sessions",out rawSessions) || !(rawSessions is IEnumerable))return;
   int index=0;foreach(var value in (IEnumerable)rawSessions){var original=value as Dictionary<string,object>;if(original==null || index>=state.Sessions.Count){index++;continue;}var session=state.Sessions[index++];if(session==null)continue;
    bool incomplete=!original.ContainsKey("Words") || !original.ContainsKey("Attempts") || session.Words==null || session.Attempts==null || String.IsNullOrEmpty(session.Id) || String.IsNullOrEmpty(session.VocabularyId);
    object attempts;if(original.TryGetValue("Attempts",out attempts) && attempts is IEnumerable){int n=0;foreach(var a in (IEnumerable)attempts){var fields=a as Dictionary<string,object>;if(session.Attempts==null || n>=session.Attempts.Count)break;var attempt=session.Attempts[n++];if(attempt==null){incomplete=true;continue;}attempt.ResultKnown=fields!=null && fields.ContainsKey("Correct") && fields["Correct"] is bool && fields.ContainsKey("At") && attempt.At>0; if(!attempt.ResultKnown || attempt.Answer==null || String.IsNullOrEmpty(attempt.WordId))incomplete=true;}}
    if(incomplete){session.LegacyIncomplete=true;session.LegacyRecordJson=StateStore.Json().Serialize(original);if(String.IsNullOrEmpty(session.Id))session.Id="legacy-"+Sha256(session.LegacyRecordJson).Substring(0,16)+"-"+index;}
   }
  }
  static Vocabulary CloneBook(Vocabulary book){return StateStore.Json().Deserialize<Vocabulary>(StateStore.Json().Serialize(book));}
  public static AppState Merge(AppState old,Vocabulary fresh,string previousJson=""){
   var state=StateStore.Json().Deserialize<AppState>(StateStore.Encode(old));
   var book=state.Vocabularies.SingleOrDefault(v=>v.Id==OriginalId);
   if(state.SchemaVersion==2 && book!=null && book.Edition==fresh.Edition)return state;
   var report=new MigrationRecord{Edition=fresh.Edition,PreviousSha256=Sha256(previousJson),LegacySessionIds=state.Sessions.Select(q=>q.Id).ToList()};
   var replacement=CloneBook(fresh);replacement.Id=OriginalId;replacement.BuiltIn=true;
   var occupied=new HashSet<string>(state.Vocabularies.SelectMany(v=>v.Words).Select(w=>w.Id));
   var oldGroups=(book==null?new List<Word>():book.Words).GroupBy(w=>Canon(w.English)).ToDictionary(g=>g.Key,g=>g.ToList());
   var usedOld=new HashSet<string>();
   foreach(var word in replacement.Words){List<Word> matches;if(oldGroups.TryGetValue(Canon(word.English),out matches) && matches.Count==1){var original=matches[0];word.Id=original.Id;word.English=original.English;usedOld.Add(original.Id);report.Matched++;}
    else {if(matches!=null && matches.Count>1)report.Conflicts.Add("内置词同拼写冲突："+word.English);string proposed=word.Id;int suffix=0;while(occupied.Contains(proposed))proposed="v2-"+Sha256(OriginalId+"|"+word.English+"|"+(suffix++)).Substring(0,24);word.Id=proposed;report.Added++;}occupied.Add(word.Id);
   }
   if(book!=null){foreach(var original in book.Words.Where(w=>!usedOld.Contains(w.Id))){original.Formal=false;replacement.Words.Add(original);report.Unmatched++;}state.Vocabularies[state.Vocabularies.IndexOf(book)]=replacement;}
   else state.Vocabularies.Add(replacement);
   if(state.Reading==null)state.Reading=new ReadingPreferences();if(state.Migrations==null)state.Migrations=new List<MigrationRecord>();
   foreach(var vocabulary in state.Vocabularies){LibraryPosition position;if(!state.Positions.TryGetValue(vocabulary.Id,out position))continue;var ids=new HashSet<string>(vocabulary.Words.Select(w=>w.Id));position.RandomOrder=position.RandomOrder.Where(ids.Contains).Distinct().ToList();if(position.RandomOrder.Count>0){var existing=new HashSet<string>(position.RandomOrder);position.RandomOrder.AddRange(vocabulary.Words.Select(w=>w.Id).Where(id=>!existing.Contains(id)));}
    foreach(string mode in new[]{"fixed","random"}){string anchor=mode=="fixed"?position.FixedAnchor:position.RandomAnchor;var key=LearningEngine.StudyKey(vocabulary.Id,mode);if(!state.Reading.Positions.ContainsKey(key))state.Reading.Positions[key]=new ReadingPosition{Anchor=anchor};}
   }
   // User records are never remapped across vocabularies, and matched old IDs remain unchanged.
   state.SchemaVersion=2;state.Migrations.Add(report);StateStore.Validate(state);return state;
  }
  public static bool Apply(StateStore store,AppState current,Vocabulary fresh,out AppState migrated){
   var existing=current.Vocabularies.SingleOrDefault(v=>v.Id==OriginalId);
   if(current.SchemaVersion==2 && existing!=null && existing.Edition==fresh.Edition){migrated=current;return false;}
   string raw=StateStore.ReadBounded(store.FilePath);var before=StateStore.Decode(raw);string backupName="before-v2-"+DateTime.Now.ToString("yyyyMMdd-HHmmssfff")+".cet6backup";string backup=Path.Combine(store.DirectoryPath,backupName);
   store.ExportRaw(backup,raw);var readable=store.ReadBackup(backup);
   CheckPreserved(before,readable);
   var candidate=Merge(before,fresh,raw);candidate.Migrations.Last().BackupFile=backupName;
   CheckPreserved(before,candidate);store.Save(candidate);migrated=candidate;
   // The in-state report is committed atomically. A secondary report failure cannot undo that commit.
   try{StateStore.AtomicWrite(Path.Combine(store.DirectoryPath,"migration-v2-report.json"),StateStore.Json().Serialize(candidate.Migrations.Last()),false);}catch(IOException){}catch(UnauthorizedAccessException){}return true;
  }
  public static void CheckPreserved(AppState before,AppState after){
   string[] left={StateStore.Json().Serialize(before.Notebooks),StateStore.Json().Serialize(before.Progress),StateStore.Json().Serialize(before.Errors),StateStore.Json().Serialize(before.Sessions),StateStore.Json().Serialize(before.Settings),before.ActiveSession,before.CurrentVocabulary};
   string[] right={StateStore.Json().Serialize(after.Notebooks),StateStore.Json().Serialize(after.Progress),StateStore.Json().Serialize(after.Errors),StateStore.Json().Serialize(after.Sessions),StateStore.Json().Serialize(after.Settings),after.ActiveSession,after.CurrentVocabulary};
   if(Enumerable.Range(0,5).Any(i=>!StateStore.Equivalent(left[i],right[i])) || left[5]!=right[5] || left[6]!=right[6])throw new InvalidDataException("迁移核对失败：个人学习记录发生变化，未提交新档案。");
   foreach(var v in before.Vocabularies.Where(v=>v.Id!=OriginalId)){var preserved=after.Vocabularies.SingleOrDefault(x=>x.Id==v.Id);if(preserved==null || !StateStore.Equivalent(StateStore.Json().Serialize(v),StateStore.Json().Serialize(preserved)))throw new InvalidDataException("自定义词库迁移核对失败。");}
  }
 }
}
