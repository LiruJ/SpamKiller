namespace SpamKiller.Settings
{
    /// <summary> Represents the settings for a single Discord server. </summary>
    public class ServerSettings
    {
        #region Properties
        /// <summary> The primary key of the settings. </summary>
        public ulong Id { get; set; }

        /// <summary> The Discord id of the server. </summary>
        public ulong ServerId { get; set; }

        /// <summary> Is <c>true</c> if the server has been whitelisted for the admins to ban users; otherwise <c>false</c>. </summary>
        public bool IsWhitelisted { get; set; }

        /// <summary> The Discord id of the log channel. </summary>
        public ulong? LogChannelId { get; set; }

        /// <summary> The Discord id of the reporter role. </summary>
        public ulong? ReporterRoleId { get; set; }
        #endregion
    }
}