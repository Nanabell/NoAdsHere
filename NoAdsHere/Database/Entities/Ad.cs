using System.Text.RegularExpressions;

namespace NoAdsHere.Database.Entities
{
    public class Ad
    {
        public Ad(string name, Regex regex)
        {
            Name = name;
            Regex = regex;
        }

        public string Name { get; set; }
        public Regex Regex { get; set; }
    }
}