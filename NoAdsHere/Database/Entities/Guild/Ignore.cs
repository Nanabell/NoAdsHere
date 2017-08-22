using Discord;
using NoAdsHere.Common;

namespace NoAdsHere.Database.Entities.Guild
{
    public class Ignore
    {
        public Ignore(IGuild guild, IUser user, BlockType blockType, string ignroedString = null)
        {
            GuildId = guild.Id;
            IgnoreType = IgnoreType.User;
            IgnoredId = user.Id;
            BlockType = blockType;
            IgnoredString = ignroedString;
        }

        public Ignore(IGuild guild, IRole role, BlockType blockType, string ignroedString = null)
        {
            GuildId = guild.Id;
            IgnoreType = IgnoreType.Role;
            IgnoredId = role.Id;
            BlockType = blockType;
            IgnoredString = ignroedString;
        }

        public Ignore(ulong guildId, IgnoreType ignoreType, ulong ignoredId, BlockType blockType, string ignroedString = null)
        {
            GuildId = guildId;
            IgnoreType = ignoreType;
            IgnoredId = ignoredId;
            BlockType = blockType;
            IgnoredString = ignroedString;
        }

        public Ignore()
        {
        }

        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public IgnoreType IgnoreType { get; set; }
        public ulong IgnoredId { get; set; }
        public BlockType BlockType { get; set; }
        public string IgnoredString { get; set; }
    }
}