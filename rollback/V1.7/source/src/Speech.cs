using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Security;
using System.Security.Cryptography;
using System.Speech.Synthesis;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CET6Word {
 public interface IPronunciation : IDisposable {Task<string> SpeakAsync(string text);void CancelPending();}
 public class SpeechService : IPronunciation {
  public Func<string,byte[]> OnlineOverride;
  internal Func<string,HttpWebRequest> OnlineRequestFactory {get;set;}
  public Func<string,byte[]> OfflineOverride;
  public Action<byte[]> PlaybackOverride;
  readonly StateStore store;readonly Func<Preferences> settings;
  readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
  int requestVersion;bool disposed;
  public SpeechService(StateStore store,Func<Preferences> settings) {this.store=store;this.settings=settings;}
  public static bool RegionValid(string region) {return String.IsNullOrEmpty(region) || Regex.IsMatch(region,"^[a-z][a-z0-9]{1,39}$");}
  static string Hash(string value) {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
  public string CacheFile(string text,string provider) {return Path.Combine(store.DirectoryPath,"audio",Hash(provider+"|en-US|1.0|v1|"+text)+".wav");}
  public static bool WaveValid(byte[] bytes) {return bytes!=null && bytes.Length>=44 && bytes.Length<32*1024*1024 && Encoding.ASCII.GetString(bytes,0,4)=="RIFF" && Encoding.ASCII.GetString(bytes,8,4)=="WAVE";}
  byte[] GetOnline(string text) {
   if(OnlineOverride!=null)return OnlineOverride(text);
   string region=settings().AzureRegion,key=store.ReadKey();if(String.IsNullOrEmpty(region) || String.IsNullOrEmpty(key))throw new InvalidOperationException("尚未配置在线语音。");
   if(!RegionValid(region))throw new InvalidOperationException("在线语音区域无效。");
   ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
   string url="https://"+region+".tts.speech.microsoft.com/cognitiveservices/v1";
   var req=OnlineRequestFactory!=null?OnlineRequestFactory(url):(HttpWebRequest)WebRequest.Create(url);req.Method="POST";req.Timeout=5000;req.ReadWriteTimeout=5000;req.AllowAutoRedirect=false;req.ContentType="application/ssml+xml";req.Headers["Ocp-Apim-Subscription-Key"]=key;req.Headers["X-Microsoft-OutputFormat"]="riff-24khz-16bit-mono-pcm";req.UserAgent="CET6Word/1.7";
   byte[] body=Encoding.UTF8.GetBytes("<speak version='1.0' xml:lang='en-US'><voice name='en-US-JennyNeural'><prosody rate='0%'>"+SecurityElement.Escape(text)+"</prosody></voice></speak>");req.ContentLength=body.Length;
   using(var stream=req.GetRequestStream())stream.Write(body,0,body.Length);
   using(var response=req.GetResponse())using(var input=response.GetResponseStream())using(var output=new MemoryStream()){byte[] buffer=new byte[8192];int n;while((n=input.Read(buffer,0,buffer.Length))>0){output.Write(buffer,0,n);if(output.Length>32*1024*1024)throw new IOException("音频超过大小限制。");}return output.ToArray();}
  }
  byte[] GetOffline(string text) {
   if(OfflineOverride!=null)return OfflineOverride(text);
   using(var synth=new SpeechSynthesizer()) {
    var voice=synth.GetInstalledVoices().FirstOrDefault(v=>v.Enabled && v.VoiceInfo.Culture.Name=="en-US");
    if(voice==null)throw new InvalidOperationException("系统没有可用的美式英语语音。请在 Windows 语言设置安装英语（美国）语音，或配置在线语音。");
    synth.SelectVoice(voice.VoiceInfo.Name);synth.Rate=0;using(var memory=new MemoryStream()){synth.SetOutputToWaveStream(memory);synth.Speak(text);synth.SetOutputToNull();return memory.ToArray();}
   }
  }
  byte[] GetCached(string path) {if(!File.Exists(path))return null;try{if(new FileInfo(path).Length>32*1024*1024)return null;var b=File.ReadAllBytes(path);return WaveValid(b)?b:null;}catch{return null;}}
  void Cache(string path,byte[] bytes) {try {Directory.CreateDirectory(Path.GetDirectoryName(path));string tmp=path+".tmp";File.WriteAllBytes(tmp,bytes);if(File.Exists(path))File.Delete(path);File.Move(tmp,path);}catch{/* Playback remains available even on cache-write failure. */}}
  public async Task<string> SpeakAsync(string text) {
   int version=Interlocked.Increment(ref requestVersion);if(String.IsNullOrWhiteSpace(text) || text.Length>20000)return "没有可播放的英文内容。";
   return await Task.Run(async ()=>{
    await gate.WaitAsync();try {
     if(disposed || version!=requestVersion)return "";
     string onlineProvider="azure:"+settings().AzureRegion+":JennyNeural",offlineProvider="windows-en-US";
     string onlineFile=CacheFile(text,onlineProvider),offlineFile=CacheFile(text,offlineProvider);
     byte[] bytes=GetCached(onlineFile);string message="播放缓存美音";
     if(bytes==null) {
      try {bytes=GetOnline(text);if(!WaveValid(bytes))throw new IOException("在线语音返回无效音频。");Cache(onlineFile,bytes);message="在线美音";}
      catch {bytes=GetCached(offlineFile);message="本地美音（在线不可用或未配置）";if(bytes==null){bytes=GetOffline(text);if(!WaveValid(bytes))throw new IOException("本地语音返回无效音频。");Cache(offlineFile,bytes);}}
     }
     if(disposed || version!=requestVersion)return "";
     if(PlaybackOverride!=null)PlaybackOverride(bytes);else using(var memory=new MemoryStream(bytes))using(var player=new SoundPlayer(memory))player.PlaySync();return message;
    }catch(Exception ex){return "播放失败："+ex.Message;}finally{gate.Release();}
   });
  }
  public void CancelPending() {Interlocked.Increment(ref requestVersion);}
  public void Dispose() {disposed=true;CancelPending();}
 }
}
