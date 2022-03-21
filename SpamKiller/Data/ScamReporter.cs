using System;

namespace SpamKiller.Data
{
    public class ScamReporter
    {
        #region Properties
        public ulong Id { get; set; }

        /// <summary> The id of the reporter. </summary>
        public ulong UserId { get; set; }

        /// <summary> The id of the server. </summary>
        public ulong ServerId { get; set; }

        /// <summary> The number of users this reporter has banned. </summary>
        public int BanCount { get; set; }

        /// <summary> The date and time of this reporter's last ban. </summary>
        public DateTime? LastBanTime { get; set; }

        /// <summary> The date and time of this reporter's last unban. </summary>
        public DateTime? LastUnbanTime { get; set; }
        #endregion
    }
}