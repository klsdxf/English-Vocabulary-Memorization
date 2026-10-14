using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Web.Script.Serialization;

namespace CET6Word {
 public static class Program {
  [STAThread] public static int Main(string[] args) {
   StateStore store=null;
   try {
    string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CET6Word");
    bool migrateOnly=args.Length==3 && args[2]=="--migrate-only";
    if(args.Length>0){if((args.Length!=2 && !migrateOnly) || args[0]!="--data-dir")throw new ArgumentException("支持的参数：--data-dir 数据目录 [--migrate-only]");dir=args[1];}
    store=new StateStore(dir);AppState state;
    try {state=store.Load(CreateState);}
    catch(IOException ex) {
     if(MessageBox.Show(ex.Message+"\n\n是否选择完整备份恢复？现有损坏档案会先保留副本。","学习档案无法读取",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes){store.Dispose();return 1;}
     var open=new Microsoft.Win32.OpenFileDialog{Filter="完整备份|*.cet6backup"};if(open.ShowDialog()!=true){store.Dispose();return 1;}
     state=store.ReadBackup(open.FileName);store.Restore(state);
    }
    AppState upgraded;if(state.SchemaVersion==1){UpgradeMigration.Apply(store,state,LoadBuiltin(),out upgraded);state=upgraded;}
    if(migrateOnly){store.Dispose();return 0;}
    var app=new Application();var ui=new DesktopUI(store,state);
    app.DispatcherUnhandledException+=(s,e)=>{MessageBox.Show(ui.Window,e.Exception.Message,"操作未完成",MessageBoxButton.OK,MessageBoxImage.Warning);e.Handled=true;};
    app.Run(ui.Window);return 0;
   }catch(Exception ex){MessageBox.Show(ex.Message,"六级词汇无法启动",MessageBoxButton.OK,MessageBoxImage.Error);if(store!=null)store.Dispose();return 1;}
  }
  public static AppState CreateState() {
   var vocabulary=LoadBuiltin();var state=new AppState{SchemaVersion=2,CurrentVocabulary=vocabulary.Id};state.Vocabularies.Add(vocabulary);state.Notebooks.Add(new Notebook {Id="default",Name="默认收藏"});return state;
  }
  public static Vocabulary LoadBuiltin(){
   string json;using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("builtin.json"))using(var reader=new StreamReader(stream))json=reader.ReadToEnd();
   var vocabulary=StateStore.Json().Deserialize<Vocabulary>(json);
   if(vocabulary.Id!=UpgradeMigration.OriginalId || vocabulary.Words.Count<2800 || vocabulary.Words.Count>3200 || System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Select(vocabulary.Words,w=>w.English.ToLowerInvariant())).Count()!=vocabulary.Words.Count || vocabulary.Words.Exists(w=>!w.Formal || String.IsNullOrWhiteSpace(w.PartOfSpeech)||String.IsNullOrWhiteSpace(w.UsIpa)||w.Examples.Count==0||w.Examples.Exists(e=>String.IsNullOrWhiteSpace(e.Chinese))))throw new InvalidDataException("内置词库校验失败。旧档案未更新。");return vocabulary;
  }
 }
}
