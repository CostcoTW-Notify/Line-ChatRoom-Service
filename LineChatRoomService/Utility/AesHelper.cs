using System.Security.Cryptography;

namespace LineChatRoomService.Utility
{
    public static class AesHelper
    {

        public static string EncryptString(Aes aes, string input)
        {
            ICryptoTransform encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);

            sw.Write(input);
            sw.Dispose();
            var encrypted = ms.ToArray();

            return Convert.ToBase64String(encrypted);
        }

        public static string DecryptString(Aes aes, string input)
        {
            var data = Convert.FromBase64String(input);

            ICryptoTransform decryptor = aes.CreateDecryptor();
            using MemoryStream ms = new(data);
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);

            var originalString = sr.ReadToEnd();
            return originalString;
        }


    }
}
