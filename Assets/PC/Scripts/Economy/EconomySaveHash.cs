using System;
using System.Security.Cryptography;
using System.Text;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
#endif
namespace StoneGame.Economy
{
    internal static class EconomySaveHash
    {
        public static string Compute(string text)
        {
            var bytes=Encoding.UTF8.GetBytes(text);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // Unity's Mono SHA256 provider is managed even when named CryptoServiceProvider.
            // Use Windows CNG's standard SHA-256; stored digest bytes stay exactly the same.
            IntPtr algorithm=IntPtr.Zero;
            try
            {
                Check(BCryptOpenAlgorithmProvider(out algorithm,"SHA256",null,0));
                var digest=new byte[32];Check(BCryptHash(algorithm,IntPtr.Zero,0,bytes,bytes.Length,digest,digest.Length));
                return Convert.ToBase64String(digest);
            }
            catch(DllNotFoundException){}
            catch(EntryPointNotFoundException){}
            finally{if(algorithm!=IntPtr.Zero)BCryptCloseAlgorithmProvider(algorithm,0);}
#endif
            using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        static void Check(int status){if(status!=0)throw new CryptographicException("SHA-256 failed: 0x"+status.ToString("X8"));}
        [DllImport("bcrypt.dll",CharSet=CharSet.Unicode)]static extern int BCryptOpenAlgorithmProvider(out IntPtr algorithm,string id,string implementation,int flags);
        [DllImport("bcrypt.dll")]static extern int BCryptCloseAlgorithmProvider(IntPtr algorithm,int flags);
        [DllImport("bcrypt.dll")]static extern int BCryptHash(IntPtr algorithm,IntPtr secret,int secretLength,byte[] input,int inputLength,[Out]byte[] output,int outputLength);
#endif
    }
}
