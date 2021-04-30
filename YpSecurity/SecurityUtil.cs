using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace YpSecurity
{
    public static class SecurityUtil
    {
        public static string B64HashEncrypt(string key, string toEncrypt)
        {
            byte[] keyArray;
            var toEncryptArray = Encoding.UTF8.GetBytes(toEncrypt);
            var hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(Encoding.UTF8.GetBytes(key));
            //Always release the resources and flush data
            // of the Cryptographic service provide. Best Practice
            hashmd5.Clear();
            var tdes = new TripleDESCryptoServiceProvider
            {
                //set the secret key for the tripleDES algorithm
                Key = keyArray,
                //mode of operation. there are other 4 modes.
                //We choose ECB(Electronic code Book)
                Mode = CipherMode.ECB,
                //padding mode(if any extra byte added)
                Padding = PaddingMode.PKCS7
            };
            var cTransform = tdes.CreateEncryptor();
            //transform the specified region of bytes array to resultArray
            var resultArray =
              cTransform.TransformFinalBlock(toEncryptArray, 0,
              toEncryptArray.Length);
            //Release resources held by TripleDes Encryptor
            tdes.Clear();
            //Return the encrypted data into unreadable string format
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public static string B64HashDecrypt(string key, string cipherString)
        {
            byte[] keyArray;
            //get the byte code of the string
            var toEncryptArray = Convert.FromBase64String(cipherString);
            //if hashing was used get the hash code with regards to your key
            var hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            //release any resource held by the MD5CryptoServiceProvider
            hashmd5.Clear();
            var tdes = new TripleDESCryptoServiceProvider
            {
                //set the secret key for the tripleDES algorithm
                Key = keyArray,
                //mode of operation. there are other 4 modes. 
                //We choose ECB(Electronic code Book)
                Mode = CipherMode.ECB,
                //padding mode(if any extra byte added)
                Padding = PaddingMode.PKCS7
            };
            var cTransform = tdes.CreateDecryptor();
            var resultArray = cTransform.TransformFinalBlock(
                                 toEncryptArray, 0, toEncryptArray.Length);
            //Release resources held by TripleDes Encryptor                
            tdes.Clear();
            //return the Clear decrypted TEXT
            return UTF8Encoding.UTF8.GetString(resultArray);
        }

        public static string ToBase16String(string text)
        {
            var r = new Random();
            var key = r.Next(5000, 10000);
            return IntToHex(key) + StrToHex(text, key);
        }

        public static string FromBase16String(string b16_text)
        {
            var key = HexToInt(b16_text.Substring(0, 4));
            b16_text = b16_text.Remove(0, 4);
            var text = HexToStr(b16_text, key);
            return text;
        }

        public static string ToBase64String(string text)
        {
            var text_bytes = new byte[text.Length];
            for (var i = 0; i < text_bytes.Length; i++)
            {
                text_bytes[i] = Convert.ToByte(text[i]);
            }

            var b64_text = Convert.ToBase64String(text_bytes);
            return b64_text;
        }

        public static string FromB64String(string b64_text)
        {
            var text_bytes = Convert.FromBase64String(b64_text);
            var text = "";
            for (var i = 0; i < text_bytes.Length; i++)
            {
                text += Convert.ToChar(text_bytes[i]);
            }

            return text;
        }

        public static SecureString SecureString(string text)
        {
            var ss = new SecureString();
            for (var i = 0; i < text.Length; i++)
            {
                ss.InsertAt(ss.Length, text[i]);
            }

            return ss;
        }

        public static string UnSecureString(SecureString protected_string)
        {
            var ptr_ps = Marshal.SecureStringToBSTR(protected_string);
            return Marshal.PtrToStringBSTR(ptr_ps);
        }

        public static string ToMd5String(string key, string text)
        {
            var key_bytes = new byte[key.Length];
            for (var i = 0; i < key_bytes.Length; i++)
            {
                key_bytes[i] = Convert.ToByte(key[i]);
            }

            var hmd5 = new HMACMD5(key_bytes);
            var text_bytes = new byte[text.Length];
            for (var j = 0; j < text_bytes.Length; j++)
            {
                text_bytes[j] = Convert.ToByte(text[j]);
            }

            var md5_bytes = new byte[text_bytes.Length];
            md5_bytes = hmd5.ComputeHash(text_bytes);
            var md5_text = "";
            foreach (var b in md5_bytes)
            {
                md5_text += Convert.ToChar(b);
            }

            return md5_text;
        }

        public static bool AreEquals(SecureString ss1, SecureString ss2) => UnSecureString(ss1) == UnSecureString(ss2);

        public static void ReleaseFromMemory(IDisposable IDisposable_obj) => IDisposable_obj.Dispose();

        public static void ReleaseFromMemory(ref string str_var) => str_var = null;

        public static void ReleaseUnUsedResources() => GC.Collect();

        #region Hexadecimal conversion

        private static string HexToStr(string hex, int key)
        {
            var list = new List<string>();
            var aux = "";
            for (var i = 0; i < hex.Length; i++)
            {
                if ((i + 1) % 4 == 0)
                {
                    list.Add(aux + hex[i]);
                    aux = "";
                }
                else
                {
                    aux += hex[i];
                }
            }
            var toret = "";
            foreach (var s in list)
            {
                toret = toret.Insert(0, Convert.ToChar(HexToInt(s) - key).ToString());
            }

            return toret;
        }

        private static int HexToInt(string hex)
        {
            var val = IntEquivalent(hex[0].ToString());
            for (var i = 1; i < hex.Length; i++)
            {
                val = val * 16 + IntEquivalent(hex[i].ToString());
            }

            return val;
        }

        private static int IntEquivalent(string hex)
        {
            switch (hex)
            {
                case "a":
                    return 10;
                case "b":
                    return 11;
                case "c":
                    return 12;
                case "d":
                    return 13;
                case "e":
                    return 14;
                case "f":
                    return 15;
                default:
                    return int.Parse(hex);
            }
        }

        private static string HexEquivalent(int i)
        {
            switch (i)
            {
                case 10:
                    return "a";
                case 11:
                    return "b";
                case 12:
                    return "c";
                case 13:
                    return "d";
                case 14:
                    return "e";
                case 15:
                    return "f";
                default:
                    return i.ToString();
            }
        }

        private static int RemoveFloatingPart(int i)
        {
            try { return int.Parse(i.ToString().Split('.')[0]); }
            catch { return i; }
        }

        private static string IntToHex(int i)
        {
            var hex = "";
            while (i >= 16)
            {
                hex = hex.Insert(0, HexEquivalent(i % 16));
                i = RemoveFloatingPart(i / 16);
            }
            hex = hex.Insert(0, HexEquivalent(i));
            return hex.Length == 1 ? hex.Insert(0, "0") : hex;
        }

        private static string StrToHex(string s, int key)
        {
            var hexStr = "";
            foreach (var c in s)
            {
                hexStr = hexStr.Insert(0, IntToHex(int.Parse(Encoding.ASCII.GetBytes(c.ToString())[0].ToString()) + key).ToString());
            }

            return hexStr;
        }

        #endregion
    }
}
