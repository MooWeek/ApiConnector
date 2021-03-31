using System;
using System.Text;
using System.Security.Cryptography;

namespace oda
{
    class Hashers
    {
        public enum HashType
        {
            SHA1,
            URL,
            UNURL
        }

        /// <summary>
        /// Производит нужное хеширование
        /// </summary>
        /// <param name="InString">Строка для хеширования</param>
        /// <param name="Type">Тип хеширования</param>
        /// <returns>Хешированую строку</returns>
        public static string HashTo(string InString, string Type) 
        {
            switch (Type)
            {
                case "SHA1":
                    return ConvertToSHA1(InString);
                case "URL":
                    return _url(InString);
                case "UNURL":
                    return _unurl(InString);
                default:
                    return InString;
            }        
        }

        private static string ConvertToSHA1(string InString) 
        {
            byte[] bytes = Encoding.UTF8.GetBytes(InString);
            var sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(bytes);   
            var sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                var hex = b.ToString("x2");
                sb.Append(hex);
            }

            return sb.ToString();
        }

        private static string _url(string InString)
        {

            string url = Uri.EscapeDataString(InString);
            url = url.Replace("%20", "+");
            url = url.Replace("%26", "&");
            url = url.Replace("%3D", "=");
            return url;
        }

        /// <summary>
        /// Расшифровает строку
        /// </summary>
        /// <param name="InString">Строка для хеширования</param>
        /// <returns>Возращает расшифрованную строку</returns>
        private static string _unurl(string InString)
        {
            string url = Uri.UnescapeDataString(InString);
            url = url.Replace("+", " ");
            return url;
        }
    }
}