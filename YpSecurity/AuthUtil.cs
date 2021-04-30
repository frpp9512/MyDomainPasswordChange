using System;
using System.Security;
using System.Threading;

namespace YpSecurity
{
    public delegate void PossibleAttack(object sender, LogonEventArgs e);

    public static class AuthUtil
    {
        [SecurityCritical]
        internal static int logonTryCount = 0;

        public static event PossibleAttack PossibleAttackDetected = null;

        public static SecureString GenerateEncryptedPassword(string username, ref string password, bool releasePassword = true)
        {
            var ss = SecurityUtil.SecureString(SecurityUtil.B64HashEncrypt(username, password));
            if (releasePassword)
            {
                SecurityUtil.ReleaseFromMemory(ref password);
            }
            SecurityUtil.ReleaseUnUsedResources();
            return ss;
        }

        public static SecureString DecodeEncryptedPassword(string username, ref string password, SecureString b64Code)
        {
            var key = SecurityUtil.B64HashDecrypt(username, SecurityUtil.UnSecureString(b64Code));
            if (key == password)
            {
                SecurityUtil.ReleaseFromMemory(ref password);
                b64Code.Clear();
                var ss = SecurityUtil.SecureString(key);
                SecurityUtil.ReleaseFromMemory(ref key);
                return ss;
            }
            throw new Exception("Incorrect password.");
        }

        public static bool TryAuth(string username, ref string password, SecureString b64Password, bool releasePassword = true)
        {
            if (logonTryCount > 4)
            {
                PossibleAttackDetected?.Invoke("AuthUtil static class", new LogonEventArgs(logonTryCount, DateTime.Now));
            }

            var pow = Math.Pow(2, logonTryCount * 2);
            var responseTimeOut = int.Parse(pow.ToString());
            if (SecurityUtil.AreEquals(GenerateEncryptedPassword(username, ref password, releasePassword), b64Password))
            {
                SecurityUtil.ReleaseUnUsedResources();
                b64Password.Clear();
                logonTryCount = 0;
                return true;
            }
            logonTryCount++;
            Thread.Sleep(responseTimeOut);
            return false;
        }


        public static void CountLoginFail()
        {
            logonTryCount++;
            if (logonTryCount > 5)
            {
                PossibleAttackDetected?.Invoke("AuthUtil static class", new LogonEventArgs(logonTryCount, DateTime.Now));
            }

            var pow = Math.Pow(2, logonTryCount * 2);
            var responseTimeOut = int.Parse(pow.ToString());
            Thread.Sleep(responseTimeOut);
        }
    }
}
