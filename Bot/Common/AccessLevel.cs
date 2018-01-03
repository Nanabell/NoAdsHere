namespace Bot.Common
{
    public enum AccessLevel
    {
        /// <summary>
        /// Users with this rank will not be able to execute any commands.
        /// </summary>
        Blocked,

        /// <summary>
        /// This Level is only for Bots, it will limit if and which commands bots can use
        /// </summary>
        Bot,

        /// <summary>
        /// Generl User with no further AccessLevel
        /// </summary>
        User,

        /// <summary>
        /// Users with Kick Permissions
        /// </summary>
        Moderator,

        /// <summary>
        /// Users with Ban Permissions
        /// </summary>
        Admin,

        /// <summary>
        /// The Owner of the Server
        /// </summary>
        Owner,

        /// <summary>
        /// The Bot Owner
        /// </summary>
        BotOwner
    }
}