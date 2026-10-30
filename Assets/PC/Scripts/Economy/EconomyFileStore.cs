using System;
using System.IO;
using System.Text;
namespace StoneGame.Economy
{
    public interface IEconomyStore
    {
        bool TryRead(out string json,out string error);
        bool TryWrite(string json,out string error);
    }
    // One atomic document for both wallet and orders. Framework storage's void Write cannot report commit failure.
    public sealed class EconomyFileStore : IEconomyStore
    {
        public string Path {get;}
        public EconomyFileStore(string path){Path=System.IO.Path.GetFullPath(path);}
        public bool TryRead(out string json,out string error)
        {
            json=null;error=null;
            try
            {
                if(!File.Exists(Path))
                {
                    if(File.Exists(Path+".bak")){error="Ana ekonomi kaydı eksik; yedek mevcut. Kayıt otomatik sıfırlanmadı.";return false;}
                    return true;
                }
                json=File.ReadAllText(Path,Encoding.UTF8);return true;
            }
            catch(Exception ex){error="Ekonomi kaydı okunamadı: "+ex.Message;return false;}
        }
        public bool TryWrite(string json,out string error)
        {
            error=null;string temp=Path+".tmp";
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                var bytes=new UTF8Encoding(false).GetBytes(json);
                using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
                {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(Path))File.Replace(temp,Path,Path+".bak");else File.Move(temp,Path);
                return true;
            }
            catch(Exception ex){error="Kayıt tamamlanamadı; işlem uygulanmadı: "+ex.Message;return false;}
            finally{try{if(File.Exists(temp))File.Delete(temp);}catch(IOException){}}
        }
    }
}
