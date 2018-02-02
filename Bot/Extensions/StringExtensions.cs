using System.IO;

namespace Bot.Extensions
{
    public static class StringExtensions
    {
        public static string LimitLength(this string str, int limit)
            => str.Length > limit ? str.Substring(0, limit) : str;
        
        public static string LimitLines(this string str, int maxLines) 
        {
            using (var reader = new StringReader(str))
            {
                var newStr = "";
                for (var i = 0; i < maxLines; i++)
                {
                    string line;
                    if ((line = reader.ReadLine()) != null) { newStr += line + "\n"; }
                }

                if (reader.ReadLine() != null) { newStr += "\n..."; }
                
                return newStr;
            }
        }
    }
}