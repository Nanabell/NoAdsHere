namespace NoAdsHere.Database.Entities.Guild
{
    public class Settings
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public string Prefix { get; set; }
        public string GithubRepo { get; set; }
    }
}