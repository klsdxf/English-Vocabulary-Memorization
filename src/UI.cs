using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Win32;

namespace CET6Word {
 public class WordView : INotifyPropertyChanged {
  public Word Word; public LearningEngine Engine;public int Level; public bool Global;public string Context="study";public bool More;
  public string English {get{return Word.English;}}public string Meaning {get{return Word.Meaning;}}public string Phonetic {get{return Word.UsIpa??"";}}
  public Visibility MeaningVisibility {get{return Global || Level>0?Visibility.Visible:Visibility.Collapsed;}}
  public Visibility ExampleVisibility {get{return Level==2 && Word.Examples.Count>0?Visibility.Visible:Visibility.Collapsed;}}
  public Visibility MissingVisibility {get{return Level==2 && Word.Examples.Count==0?Visibility.Visible:Visibility.Collapsed;}}
  public string ExampleEnglish {get{return Word.Examples.Count>0?Word.Examples[0].English:"";}}
  public string ExampleChinese {get{return Word.Examples.Count>0?Word.Examples[0].Chinese:"";}}
  public string FavoriteLabel {get{return Context=="search"?"":Engine.Favorite(Word.Id)?"★":"☆";}}
  public string MasterLabel {get{Progress p;return Engine.State.Progress.TryGetValue(Word.Id,out p)&&p.Mastered?"恢复":"熟词";}}
  public Visibility ManageVisibility {get{return Context=="search"?Visibility.Collapsed:Visibility.Visible;}}
  public string StatusLabel {get;set;}
  public Visibility MoreVisibility {get{return Word.Examples.Count>1?Visibility.Visible:Visibility.Collapsed;}}
  public string MoreLabel {get{return More?"收起例句":"查看更多例句";}}
  public Visibility ExtraVisibility {get{return More?Visibility.Visible:Visibility.Collapsed;}}
  public List<Example> ExtraExamples {get{return Word.Examples.Skip(1).ToList();}}
  public event PropertyChangedEventHandler PropertyChanged;
  public void Refresh(){if(PropertyChanged!=null)PropertyChanged(this,new PropertyChangedEventArgs(""));}
 }
 public partial class DesktopUI {
  readonly StateStore store; LearningEngine engine;readonly IPronunciation speech;
  public Window Window {get;private set;}
  ContentControl host;TextBlock status;ListBox studyList;ScrollViewer studyScroll;
  List<WordView> studyRows; Dictionary<string,int> expansion=new Dictionary<string,int>();
  DispatcherTimer saveTimer; bool restoring;string page="";bool unsaved;
  ComboBox bookSelect;ListBox bookList;ComboBox quizSource;ComboBox quizMode;ComboBox quizPrompt;TextBox quizCount;
  public DesktopUI(StateStore store,AppState state,IPronunciation pronunciation=null) {
   this.store=store;engine=new LearningEngine(state);speech=pronunciation??new SpeechService(store,()=>engine.State.Settings);
   using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("MainWindow.xaml"))Window=(Window)XamlReader.Load(stream);
   host=(ContentControl)Window.FindName("PageHost");status=(TextBlock)Window.FindName("Status");
   Window.AddHandler(System.Windows.Controls.Button.ClickEvent,new RoutedEventHandler(OnButton),true);
   Window.Closing+=(s,e)=>{speech.CancelPending();SavePosition();if(!Persist()){if(MessageBox.Show(Window,"最新修改尚未成功保存。仍要退出吗？","保存失败",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)e.Cancel=true;}};
   Window.Closed+=(s,e)=>{speech.Dispose();store.Dispose();};
   saveTimer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(450)};saveTimer.Tick+=(s,e)=>{saveTimer.Stop();Persist();};
   var more=(Button)Window.FindName("MoreNav");var menu=new ContextMenu();Menu(menu,"已掌握词汇",()=>ShowMastered());Menu(menu,"错词记录",()=>ShowErrors());Menu(menu,"词库管理",()=>ShowLibraries());Menu(menu,"设置与数据",()=>ShowSettings());more.ContextMenu=menu;more.Click+=(s,e)=>{menu.PlacementTarget=more;menu.IsOpen=true;};
   Window.PreviewKeyDown+=StudyKeys;
   ShowStudy();if(!String.IsNullOrEmpty(store.RecoveryNotice))MessageBox.Show(Window,store.RecoveryNotice,"数据恢复",MessageBoxButton.OK,MessageBoxImage.Information);
  }
  void Menu(ContextMenu menu,string text,Action action){var item=new MenuItem{Header=text};item.Click+=(s,e)=>Safe(action);menu.Items.Add(item);}
  void Safe(Action action) {try{action();}catch(Exception ex){MessageBox.Show(Window,ex.Message,"操作未完成",MessageBoxButton.OK,MessageBoxImage.Warning);}}
  bool Persist(){try{store.Save(engine.State);unsaved=false;if(status!=null)status.Text="已保存到本机";return true;}catch(Exception ex){unsaved=true;if(status!=null)status.Text="尚未保存："+ex.Message;return false;}}
  void DelayedSave(){unsaved=true;saveTimer.Stop();saveTimer.Start();}
  void EnterPage(string name){if(page=="study")SavePosition();saveTimer.Stop();if(unsaved)Persist();speech.CancelPending();page=name;studyList=null;studyScroll=null;searchPanel=null;
   foreach(string id in new[]{"StudyNav","QuizNav","BooksNav"}){var b=(Button)Window.FindName(id);b.Style=(Style)Window.Resources[id==(name=="study"?"StudyNav":name=="quiz"?"QuizNav":name=="books"?"BooksNav":"")?"Primary":typeof(Button)];}
  }
  static TextBlock Text(string text,int size=14,string color="#202B3C"){return new TextBlock{Text=text,FontSize=size,Foreground=(Brush)new BrushConverter().ConvertFromString(color),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)};}
  Button Button(string text,Action action,bool primary=false){var b=new Button{Content=text,Style=(Style)Window.Resources[primary?"Primary":typeof(Button)]};b.Click+=(s,e)=>Safe(action);return b;}
  static ComboBox Combo(IEnumerable<object> items,double width=200){var c=new ComboBox{Width=width,Margin=new Thickness(0,0,12,0)};foreach(var item in items)c.Items.Add(item);if(c.Items.Count>0)c.SelectedIndex=0;return c;}
  static StackPanel Line(){return new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,14)};}
  static Grid Layout(out StackPanel top){var g=new Grid();g.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});g.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});top=new StackPanel();g.Children.Add(top);return g;}
  static ListBox List(){return new ListBox{BorderBrush=(Brush)new BrushConverter().ConvertFromString("#DFE5ED"),BorderThickness=new Thickness(1),Background=Brushes.White,HorizontalContentAlignment=HorizontalAlignment.Stretch};}
  static void Body(Grid g,UIElement element){Grid.SetRow(element,1);g.Children.Add(element);}
  static T Child<T>(DependencyObject item) where T:DependencyObject{if(item==null)return null;for(int i=0;i<VisualTreeHelper.GetChildrenCount(item);i++){var c=VisualTreeHelper.GetChild(item,i);if(c is T)return (T)c;var result=Child<T>(c);if(result!=null)return result;}return null;}
  void OnButton(object sender,RoutedEventArgs args){var b=args.OriginalSource as Button??args.Source as Button;if(b==null || !(b.Tag is string))return;string tag=(string)b.Tag;
   if(tag=="nav-study"){Safe(ShowStudy);return;}if(tag=="nav-quiz"){Safe(ShowQuiz);return;}if(tag=="nav-books"){Safe(ShowBooks);return;}
   if(tag=="example-extra"){var example=b.DataContext as Example;if(example!=null)Play(example.English);args.Handled=true;return;}
   var row=b.DataContext as WordView;if(row==null)return;
   Safe(()=>{
    if(tag=="expand"){AnimateRow(row,()=>{row.Level=Policy.NextExpansion(row.Level,row.Global);if(row.Context=="study")expansion[row.Word.Id]=row.Level;row.Refresh();});if(row.Context=="study"&&row.Level>0){engine.Learn(row.Word.Id);DelayedSave();if(engine.State.Settings.AutoRead)Play(row.Level==2&&row.Word.Examples.Count>0?row.ExampleEnglish:row.English);}}
    else if(tag=="more-examples"){AnimateRow(row,()=>{row.More=!row.More;row.Refresh();});if(row.More&&row.Context=="study"){engine.Learn(row.Word.Id);DelayedSave();}}
    else if(tag=="speak")Play(row.English);
    else if(tag=="example")Play(row.ExampleEnglish);
    else if(tag=="favorite"&&row.Context!="search"){engine.ToggleFavorite(row.Word.Id);row.Refresh();DelayedSave();if(row.Context!="study"){RefreshBookRows();Window.UpdateLayout();var replacement=Tree(Window).OfType<Button>().FirstOrDefault(x=>x.Tag as string=="favorite" && x.DataContext is WordView && ((WordView)x.DataContext).Word.Id==row.Word.Id);if(replacement!=null)Pulse(replacement);}else Pulse(b);}
    else if(tag=="master"&&row.Context!="search"){SavePosition();CaptureRemoval(row);Progress p;bool was=engine.State.Progress.TryGetValue(row.Word.Id,out p)&&p.Mastered;engine.Master(row.Word.Id,!was);DelayedSave();if(row.Context=="study"){if(engine.Anchor==row.Word.Id){int i=studyRows.IndexOf(row);engine.SetAnchor(i+1<studyRows.Count?studyRows[i+1].Word.Id:i>0?studyRows[i-1].Word.Id:null);}var pos=StudyPosition;pos.Anchor=null;RefreshStudy(true);FadeRemoved(row);}else{row.Refresh();RefreshBookRows();}}
   });args.Handled=true;
  }
  async void Play(string text){status.Text="正在准备美式英语发音…";string result=await speech.SpeakAsync(text);if(!String.IsNullOrEmpty(result) && Window.IsVisible)status.Text=result;}
  void ShowQuiz(){EnterPage("quiz");var active=engine.Active;if(active!=null && active.LegacyIncomplete){engine.State.ActiveSession=null;DelayedSave();active=null;}if(active!=null && active.Finished==0){ShowQuestion();return;}if(active!=null && active.Finished>0){ShowResult(active);return;}
   var panel=new StackPanel{Margin=new Thickness(12,4,12,0)};panel.Children.Add(Text("开始默写",24));panel.Children.Add(Text("选择词汇范围与提示方式。最近学习只记录主动展开的词。",14,"#79869A"));
   var row=Line();quizMode=Combo(new object[]{"最近 X 个词","随机抽取"},160);row.Children.Add(quizMode);quizSource=Combo(new object[]{new Choice("全部当前词库","all"),new Choice("已主动学习词汇","learned")}.Concat(engine.State.Notebooks.Select(n=>(object)new Choice(n.Name,n.Id))),240);quizSource.DisplayMemberPath="Name";quizSource.IsEnabled=false;quizMode.SelectionChanged+=(s,e)=>{quizSource.IsEnabled=quizMode.SelectedIndex==1;};row.Children.Add(quizSource);panel.Children.Add(row);
   row=Line();row.Children.Add(Text("题数",14));quizCount=new TextBox{Text="10",Width=80,Margin=new Thickness(12,0,18,0)};row.Children.Add(quizCount);quizPrompt=Combo(new object[]{"中文提示","听音默写"},160);row.Children.Add(quizPrompt);panel.Children.Add(row);
   panel.Children.Add(Button("开始",()=>{int n;if(!Int32.TryParse(quizCount.Text,out n)||n<1||n>10000)throw new ArgumentException("题数需为 1–10000。");bool recent=quizMode.SelectedIndex==0;string source=recent?"recent":((Choice)quizSource.SelectedItem).Id;int available=engine.Candidates(source).Count;if(available>0 && available<n && MessageBox.Show(Window,"此范围只有 "+available+" 个词，按实际数量开始吗？","词汇数量",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;engine.Start(source,n,quizPrompt.SelectedIndex==1,recent);Persist();ShowQuestion();},true));host.Content=panel;
  }
  void ShowQuestion(){page="quiz";var q=engine.Active;if(q==null || q.Finished>0){ShowQuiz();return;}var word=engine.Question;var panel=new StackPanel{Margin=new Thickness(36,18,36,0)};int number=q.Attempts.Count+(q.AwaitingNext?0:1);panel.Children.Add(Text("第 "+number+" / "+q.Words.Count+" 题",13,"#79869A"));
   if(q.Listening){panel.Children.Add(Text("听音后输入英文",25));panel.Children.Add(Button("播放美式英语发音",()=>Play(word.English)));}else panel.Children.Add(Text(word.Meaning,24));
   var answer=new TextBox{FontSize=24,FontFamily=new FontFamily("Segoe UI"),Margin=new Thickness(0,22,0,16),MaxLength=1000};panel.Children.Add(answer);var actions=Line();panel.Children.Add(actions);
   Action submit=()=>{engine.Submit(answer.Text);Persist();ShowQuestion();};var submitButton=Button("提交",submit,true);actions.Children.Add(submitButton);
   if(q.AwaitingNext){var a=q.Attempts.Last();answer.Text=a.Answer;answer.IsReadOnly=true;submitButton.Visibility=Visibility.Collapsed;var feedback=Text(a.Correct?"✓ 回答正确":"✕ 回答错误 · 正确拼写："+word.English,18,a.Correct?"#237D59":"#B54747");panel.Children.Add(feedback);Feedback(feedback,a.Correct);actions.Children.Add(Button(q.Attempts.Count==q.Words.Count?"查看结果":"下一题",()=>{engine.Next();Persist();if(q.Finished>0)ShowResult(q);else ShowQuestion();},true));}
   actions.Children.Add(Button("结束本轮",()=>{if(MessageBox.Show(Window,"结束本轮？已提交的作答和错词会保留。","结束默写",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;q.Finished=DateTime.UtcNow.Ticks;Persist();ShowResult(q);}));
   answer.KeyDown+=(s,e)=>{if(e.Key==Key.Enter){Safe(()=>{if(!q.AwaitingNext)submit();else{engine.Next();Persist();if(q.Finished>0)ShowResult(q);else ShowQuestion();}});e.Handled=true;}};host.Content=panel;Window.Dispatcher.BeginInvoke(new Action(()=>answer.Focus()));if(q.Listening && !q.AwaitingNext)Play(word.English);
  }
  void ShowResult(Session q){page="quiz";var panel=new StackPanel{Margin=new Thickness(20,12,20,0)};panel.Children.Add(Text("本轮结果",25));int correct=q.Attempts.Count(a=>a.Correct),wrong=q.Attempts.Count-correct;panel.Children.Add(Text("计划 "+q.Words.Count+" 题 · 已答 "+q.Attempts.Count+" 题 · 正确 "+correct+" · 错误 "+wrong,18));var ids=engine.Wrong(q);var row=Line();if(ids.Count>0)row.Children.Add(Button("重新默写本轮错词",()=>{engine.EndSession();engine.State.CurrentVocabulary=q.VocabularyId;engine.StartWith(ids,q.Listening,"retry");Persist();ShowQuestion();},true));row.Children.Add(Button("结束",()=>{engine.EndSession();Persist();ShowQuiz();}));panel.Children.Add(row);panel.Children.Add(Text(ids.Count>0?"本轮错词已保存，可稍后再复习。":"本轮没有错词。",14,"#79869A"));var list=List();list.MaxHeight=350;foreach(string id in ids)list.Items.Add(Text(engine.Find(id).English+"  ·  "+engine.Find(id).Meaning,16));panel.Children.Add(list);host.Content=panel;}
  public class Choice{public string Name{get;set;}public string Id{get;set;}public Choice(string name,string id){Name=name;Id=id;}}
  DataTemplate SimpleWordTemplate(){return (DataTemplate)XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel Margin='16,12'><TextBlock Text='{Binding English}' FontFamily='Segoe UI' FontSize='20'/><TextBlock Text='{Binding Meaning}' TextWrapping='Wrap' Margin='0,6,0,0' Foreground='#65738A'/></StackPanel></DataTemplate>");}
  string Prompt(string title,string label,string value){var dialog=new Window{Title=title,Owner=Window,Width=420,Height=205,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner,FontFamily=Window.FontFamily,FontSize=14};var panel=new StackPanel{Margin=new Thickness(24)};panel.Children.Add(Text(label));var field=new TextBox{Text=value,MaxLength=80,Margin=new Thickness(0,0,0,16)};panel.Children.Add(field);var row=Line();var ok=new Button{Content="确定",IsDefault=true};ok.Click+=(s,e)=>{dialog.DialogResult=true;};row.Children.Add(ok);var cancel=new Button{Content="取消",IsCancel=true};row.Children.Add(cancel);panel.Children.Add(row);dialog.Content=panel;dialog.Loaded+=(s,e)=>{field.Focus();field.SelectAll();};return dialog.ShowDialog()==true?field.Text:null;}
  void ShowMastered(){EnterPage("mastered");StackPanel top;var g=Layout(out top);top.Children.Add(Text("已掌握词汇",24));top.Children.Add(Text("仅查看当前词库；恢复后重新出现在待背列表。",13,"#79869A"));var list=List();list.SelectionMode=SelectionMode.Extended;list.ItemTemplate=SimpleWordTemplate();list.ItemsSource=engine.Current.Words.Where(w=>engine.GetProgress(w.Id).Mastered).ToList();top.Children.Add(Button("恢复所选词",()=>{foreach(var w in list.SelectedItems.Cast<Word>().ToList())engine.Master(w.Id,false);Persist();ShowMastered();}));Body(g,list);host.Content=g;}
  void ShowErrors(){EnterPage("errors");StackPanel top;var g=Layout(out top);top.Children.Add(Text("错词记录",24));var row=Line();var mode=Combo(new object[]{"待复习错词","全部历史错词"},190);row.Children.Add(mode);var prompt=Combo(new object[]{"中文提示","听音默写"},140);row.Children.Add(prompt);row.Children.Add(Button("默写待复习词",()=>{if(engine.Active!=null && engine.Active.Finished==0)throw new ArgumentException("请先到默写页继续或结束当前轮次。");engine.EndSession();var ids=engine.Candidates("errors");engine.StartWith(engine.Shuffle(ids),prompt.SelectedIndex==1,"errors");Persist();ShowQuiz();},true));top.Children.Add(row);var list=List();list.SelectionMode=SelectionMode.Extended;Action fill=()=>{list.Items.Clear();foreach(var w in engine.Current.Words.Where(w=>engine.State.Errors.ContainsKey(w.Id))){var er=engine.State.Errors[w.Id];if(mode.SelectedIndex==0&&!er.Pending)continue;var item=new ListBoxItem{Tag=w.Id};item.Content=Text(w.English+"  ·  连续答对 "+er.Streak+" / "+engine.State.Settings.ErrorThreshold+"  ·  历史答错 "+er.WrongCount+"  ·  练习 "+er.PracticeCount+(er.Pending?"  待复习":"  已移出"),16);item.Padding=new Thickness(16,12,16,0);list.Items.Add(item);}};
   row=Line();row.Children.Add(Button("移出待复习",()=>{foreach(var item in list.SelectedItems.Cast<ListBoxItem>().ToList())engine.Review((string)item.Tag,false);Persist();fill();}));row.Children.Add(Button("恢复待复习",()=>{foreach(var item in list.SelectedItems.Cast<ListBoxItem>().ToList())engine.Review((string)item.Tag,true);Persist();fill();}));top.Children.Add(row);top.Children.Add(Text("移出复习列表会保留全部历史记录；切换“全部历史错词”可恢复。",12,"#79869A"));mode.SelectionChanged+=(s,e)=>fill();fill();Body(g,list);host.Content=g;}
  void ShowLibraries(){EnterPage("libraries");StackPanel top;var g=Layout(out top);top.Children.Add(Text("词库管理",24));top.Children.Add(Text("内置六级原词书 "+engine.OriginalWords().Count+" 词。导入词库独立管理，学习记录彼此隔离。",14,"#79869A"));var row=Line();row.Children.Add(Button("导入 CSV / XLSX",()=>{
    var d=new OpenFileDialog{Filter="词库表格|*.csv;*.xlsx"};if(d.ShowDialog(Window)!=true)return;var r=VocabularyImport.Read(d.FileName);string details="可导入 "+r.Vocabulary.Words.Count+" 词\n重复跳过 "+r.Duplicates+" 行 · 无例句 "+r.MissingExamples+" 词\n无效行 "+r.Rejected.Count+"\n"+String.Join("\n",r.Rejected.Take(8))+"\n\n创建独立词库，不覆盖现有学习进度。继续导入？";
    if(MessageBox.Show(Window,details,"导入预览",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;if(engine.State.Vocabularies.Sum(v=>v.Words.Count)+r.Vocabulary.Words.Count>100000)throw new ArgumentException("全部词库合计不能超过 10 万词条。");engine.State.Vocabularies.Add(r.Vocabulary);engine.State.CurrentVocabulary=r.Vocabulary.Id;Persist();ShowLibraries();
   },true));row.Children.Add(Button("保存导入模板",()=>{var d=new SaveFileDialog{FileName="词库导入模板.csv",Filter="CSV|*.csv"};if(d.ShowDialog(Window)==true)File.WriteAllText(d.FileName,"word,meaning,example,example_translation\r\nadapt,v. 适应；调整,The team had to adapt to a smaller budget.,团队不得不适应更小的预算。\r\n",new System.Text.UTF8Encoding(true));}));top.Children.Add(row);
   var list=List();list.ItemTemplate=(DataTemplate)XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel Margin='16,12'><TextBlock Text='{Binding Name}' FontSize='18'/><TextBlock Text='{Binding Source}' Foreground='#79869A' TextWrapping='Wrap' Margin='0,6,0,0'/></StackPanel></DataTemplate>");list.ItemsSource=engine.State.Vocabularies;top.Children.Add(Button("切换到所选词库",()=>{var v=list.SelectedItem as Vocabulary;if(v==null)throw new ArgumentException("请先选择词库。");engine.State.CurrentVocabulary=v.Id;Persist();ShowStudy();}));Body(g,list);host.Content=g;}
  void ShowSettings(){EnterPage("settings");var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto};var panel=new StackPanel{Margin=new Thickness(12,4,12,0)};panel.Children.Add(Text("设置与数据",24));var auto=new CheckBox{Content="主动展开时自动朗读",IsChecked=engine.State.Settings.AutoRead,Margin=new Thickness(0,0,0,18)};auto.Click+=(s,e)=>{engine.State.Settings.AutoRead=auto.IsChecked==true;Persist();};panel.Children.Add(auto);
   var row=Line();row.Children.Add(Text("错词连续答对后移出",14));var threshold=new TextBox{Text=engine.State.Settings.ErrorThreshold.ToString(),Width=65,Margin=new Thickness(12,0,12,0)};row.Children.Add(threshold);row.Children.Add(Button("保存阈值",()=>{int n;if(!Int32.TryParse(threshold.Text,out n)||n<1||n>100)throw new ArgumentException("阈值需为 1–100。");engine.State.Settings.ErrorThreshold=n;Persist();}));panel.Children.Add(row);
   panel.Children.Add(Text("语音",18));panel.Children.Add(Text("只播放美式英语，正常语速。未配置在线服务或在线失败时使用 Windows 本地美音。",13,"#79869A"));
   panel.Children.Add(Text("可选在线服务：Azure Speech（需自己的服务凭据，可能产生费用）",13));row=Line();var region=new TextBox{Text=engine.State.Settings.AzureRegion,Width=130,ToolTip="服务区域，例如 eastus",Margin=new Thickness(0,0,12,0)};row.Children.Add(region);var key=new PasswordBox{Width=240,Padding=new Thickness(8),ToolTip="新密钥；留空保留现有密钥",Margin=new Thickness(0,0,12,0)};row.Children.Add(key);row.Children.Add(Button("保存语音配置",()=>{string value=region.Text.Trim();if(!SpeechService.RegionValid(value))throw new ArgumentException("区域仅支持英文小写字母和数字，例如 eastus。");if(key.Password.Length>0)store.SaveKey(key.Password);engine.State.Settings.AzureRegion=value;key.Clear();Persist();}));panel.Children.Add(row);
   panel.Children.Add(Text("左侧填写区域，右侧填写密钥（留空保留）。清空区域可停用在线服务。在线朗读会将所读英文发送给该服务；密钥仅在本机加密保存。",12,"#79869A"));panel.Children.Add(Button("测试美式英语发音",()=>Play("Welcome to your vocabulary practice.")));
   panel.Children.Add(Text("完整备份",18));row=Line();row.Children.Add(Button("导出完整备份",()=>{SavePosition();var d=new SaveFileDialog{FileName="六级词汇备份-"+DateTime.Now.ToString("yyyyMMdd")+".cet6backup",Filter="完整备份|*.cet6backup"};if(d.ShowDialog(Window)==true){store.Export(d.FileName,engine.State);status.Text="完整备份已导出";}}));row.Children.Add(Button("从备份恢复",()=>{var d=new OpenFileDialog{Filter="完整备份|*.cet6backup"};if(d.ShowDialog(Window)!=true)return;var restored=store.ReadBackup(d.FileName);if(MessageBox.Show(Window,"恢复将覆盖现有学习档案。现有数据会先保留安全副本。确认恢复？","恢复完整备份",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;store.Restore(restored);AppState upgraded;if(restored.SchemaVersion==1){UpgradeMigration.Apply(store,restored,Program.LoadBuiltin(),out upgraded);restored=upgraded;}engine=new LearningEngine(restored);expansion.Clear();unsaved=false;ShowStudy();status.Text="已恢复完整学习档案；在线服务密钥需在新电脑重新配置。";}));panel.Children.Add(row);
   panel.Children.Add(Text("备份包括词库、位置、熟词、词本、学习和默写记录、错词及设置；不含语音密钥和可再生成的音频缓存。",12,"#79869A"));panel.Children.Add(Text("本机数据目录\n"+store.DirectoryPath,12,"#79869A"));panel.Children.Add(Text("软件 V2.0 · Windows 原生桌面 · 无账号和云同步",12,"#79869A"));scroll.Content=panel;host.Content=scroll;
  }
 }
}
