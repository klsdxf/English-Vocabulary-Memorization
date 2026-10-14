using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace CET6Word {
 public class ImportResult {public Vocabulary Vocabulary {get;set;} public int Duplicates {get;set;} public int MissingExamples {get;set;} public List<string> Rejected {get;set;} = new List<string>();}
 public static class VocabularyImport {
  static string Header(string s) {return (s??"").Trim().Trim('\uFEFF').Replace(" ","").ToLowerInvariant();}
  static int Column(List<string> headers,params string[] aliases) {for(int i=0;i<headers.Count;i++)if(aliases.Contains(Header(headers[i])))return i;return -1;}
  static string Cell(List<string> row,int col) {return col>=0 && col<row.Count?(row[col]??"").Trim():"";}
  public static ImportResult Read(string path) {
   if(new FileInfo(path).Length>50L*1024*1024)throw new InvalidDataException("词库文件超过 50 MB。");
   string ext=Path.GetExtension(path).ToLowerInvariant();List<List<string>> rows;
   if(ext==".csv") {byte[] raw=File.ReadAllBytes(path);string text;try{text=new UTF8Encoding(false,true).GetString(raw);}catch(DecoderFallbackException){text=Encoding.GetEncoding("GB18030").GetString(raw);}rows=ParseCsv(text);}
   else if(ext==".xlsx") rows=ReadXlsx(path);
   else throw new InvalidDataException("请导入 CSV 或 XLSX；旧 XLS 请先另存为 XLSX/CSV。");
   return FromRows(rows,Path.GetFileNameWithoutExtension(path));
  }
  public static ImportResult FromRows(List<List<string>> rows,string name) {
   if(rows.Count<2)throw new InvalidDataException("表格没有数据行。");
   int en=Column(rows[0],"word","english","英文","单词","英文单词"),cn=Column(rows[0],"meaning","translation","中文","释义","中文释义"),ex=Column(rows[0],"example","例句","英文例句"),tr=Column(rows[0],"example_translation","例句翻译","中文例句");
   if(en<0 || cn<0)throw new InvalidDataException("第一行需包含 word / meaning（或 单词 / 释义）列名。可选 example / example_translation。");
   var r=new ImportResult {Vocabulary=new Vocabulary {Id=Guid.NewGuid().ToString("N"),Name=name,Source="用户自行导入；请确保拥有使用权"}};var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   for(int i=1;i<rows.Count;i++) {
    var row=rows[i];if(row.All(String.IsNullOrWhiteSpace))continue;string english=Cell(row,en),meaning=Cell(row,cn),example=Cell(row,ex);
    if(english.Length==0 || meaning.Length==0 || english.Length>200 || meaning.Length>20000 || example.Length>20000 || Cell(row,tr).Length>20000 || !english.Any(c=>c>='A'&&c<='Z'||c>='a'&&c<='z') || english.Any(c=>Char.IsControl(c))) {r.Rejected.Add("第 "+(i+1)+" 行：英文/释义缺失或内容无效。");continue;}
    if(!seen.Add(english)){r.Duplicates++;continue;}
    var w=new Word {Id=Guid.NewGuid().ToString("N"),English=english,Meaning=meaning};if(example.Length>0)w.Examples.Add(new Example {English=example,Chinese=Cell(row,tr)});else r.MissingExamples++;
    r.Vocabulary.Words.Add(w);if(r.Vocabulary.Words.Count>100000)throw new InvalidDataException("单个词库最多 10 万词条。");
   }
   if(r.Vocabulary.Words.Count==0)throw new InvalidDataException("没有可导入的有效词条。"+String.Join("\n",r.Rejected.Take(8)));return r;
  }
  public static List<List<string>> ParseCsv(string text) {
   var rows=new List<List<string>>();var row=new List<string>();var field=new StringBuilder();bool quoted=false,closed=false;
   for(int i=0;i<text.Length;i++) {
    char c=text[i];if(quoted){if(c=='"'){if(i+1<text.Length && text[i+1]=='"'){field.Append('"');i++;}else{quoted=false;closed=true;}}else field.Append(c);}
    else if(c==',' || c=='\n' || c=='\r'){row.Add(field.ToString());field.Clear();closed=false;if(c!=','){if(c=='\r' && i+1<text.Length && text[i+1]=='\n')i++;rows.Add(row);row=new List<string>();}}
    else if(c=='"'){if(field.Length!=0 || closed)throw new InvalidDataException("CSV 引号格式错误。");quoted=true;}
    else if(closed){if(c!=' ' && c!='\t')throw new InvalidDataException("CSV 引号后的字符无效。");}
    else field.Append(c);
    if(field.Length>1000000 || rows.Count>200000 || row.Count>1000)throw new InvalidDataException("表格超出大小限制。");
   }
   if(quoted)throw new InvalidDataException("CSV 引号未闭合。");if(field.Length>0 || row.Count>0 || closed){row.Add(field.ToString());rows.Add(row);}return rows;
  }
  static XDocument Xml(ZipArchive z,string name) {
   var entry=z.GetEntry(name);if(entry==null)throw new InvalidDataException("XLSX 缺少 "+name);if(entry.Length>32L*1024*1024)throw new InvalidDataException("XLSX 内容过大。");
   using(var stream=entry.Open())using(var reader=XmlReader.Create(stream,new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=32L*1024*1024}))return XDocument.Load(reader);
  }
  static List<List<string>> ReadXlsx(string path) {
   using(var z=ZipFile.OpenRead(path)) {
    if(z.Entries.Count>1000 || z.Entries.Sum(e=>e.Length)>128L*1024*1024)throw new InvalidDataException("XLSX 解压大小超限。");
    XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main", rel="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    var shared=new List<string>();if(z.GetEntry("xl/sharedStrings.xml")!=null)shared=Xml(z,"xl/sharedStrings.xml").Descendants(ns+"si").Select(x=>String.Concat(x.Descendants(ns+"t").Select(t=>t.Value))).ToList();
    var sheet=Xml(z,"xl/workbook.xml").Descendants(ns+"sheet").FirstOrDefault();if(sheet==null)throw new InvalidDataException("XLSX 无工作表。");string rid=(string)sheet.Attribute(rel+"id");
    var link=Xml(z,"xl/_rels/workbook.xml.rels").Root.Elements().FirstOrDefault(x=>(string)x.Attribute("Id")==rid);if(link==null || (string)link.Attribute("TargetMode")=="External")throw new InvalidDataException("工作表关联无效。");
    string target=(string)link.Attribute("Target");if(String.IsNullOrEmpty(target) || target.Contains("..") || target.Contains("\\"))throw new InvalidDataException("工作表路径无效。");string sheetPath=target.StartsWith("/")?target.TrimStart('/'):("xl/"+target);
    var result=new List<List<string>>();foreach(var xr in Xml(z,sheetPath).Descendants(ns+"row")) {
     var row=new List<string>();foreach(var cell in xr.Elements(ns+"c")) {
      string reference=(string)cell.Attribute("r");int col=0;if(reference!=null){foreach(char ch in reference.TakeWhile(Char.IsLetter))col=checked(col*26+(Char.ToUpperInvariant(ch)-'A'+1));col--;}else col=row.Count;
      if(col<0 || col>999)throw new InvalidDataException("XLSX 列数过多。");while(row.Count<=col)row.Add("");
      string value=(string)cell.Element(ns+"v")??"",type=(string)cell.Attribute("t");
      if(type=="s"){int index;if(!Int32.TryParse(value,out index) || index<0 || index>=shared.Count)throw new InvalidDataException("XLSX 字符串索引无效。");value=shared[index];}
      else if(type=="inlineStr")value=String.Concat(cell.Descendants(ns+"t").Select(t=>t.Value));
      row[col]=value;
     }
     result.Add(row);if(result.Count>200000)throw new InvalidDataException("XLSX 行数过多。");
    }return result;
   }
  }
 }
}
