using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using NetTopologySuite.Algorithm;

namespace FINE.Service.Utilities
{
    public static class Ultils
    {
        public static string GenerateRandomCode()
        {
            var randomCode = new Random();

            string chars = "0123456789zxcvbnmasdfghjklqwertyuiop";
            int length = 10;
            return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[randomCode.Next(s.Length)]).ToArray());
        }

        public static byte[] GetHash (string password, string fineSugar)
        {
            byte[] byteCode =  Encoding.Unicode.GetBytes(string.Concat(password, fineSugar));

            SHA256Managed hashCode = new SHA256Managed();
            byte[] pass =  hashCode.ComputeHash(byteCode);

            return pass;
        }

        public static bool CompareHash(string attemptedPassword, byte[] hash, string salt)
        {
            string base64Hash = Convert.ToBase64String(hash);
            string base64AttemptedHash = Convert.ToBase64String(GetHash(attemptedPassword, salt));

            var result = base64Hash == base64AttemptedHash;
            return result;
        }

        public static string RandomPassword()
        {
            Random pass = new Random();
            var chars = "1234567890qwertyuiopasdfghjklzxcvbnm";
            var length = 10;
            return new string(Enumerable.Repeat(chars, length)
                                        .Select(s => s[pass.Next(s.Length)]).ToArray());
        }
        public static string ToSnakeCase(this string o) => Regex.Replace(o, @"(\w)([A-Z])", "$1-$2").ToLower();

        public static bool CheckVNPhone(string phoneNumber)
        {
            string strRegex = @"(^(0)(3[2-9]|5[6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-4|6-9])[0-9]{7}$)";
            Regex re = new Regex(strRegex);
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");

            if (re.IsMatch(phoneNumber))
            {
                return true;
            }
            else
                return false;
        }
    }
}
