using System.ComponentModel.DataAnnotations;

namespace NoAdsHere.Database.Entities.Global
{
    public class Master
    {
        public int Id { get; set; }
        public ulong UserId { get; set; }
    }
}