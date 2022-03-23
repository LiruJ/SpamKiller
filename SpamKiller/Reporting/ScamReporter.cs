using System;

namespace SpamKiller.Reporting
{
    /// <summary> Represents a user who is capable of banning and unbanning users. </summary>
    public class ScamReporter
    {
        #region Properties
        /// <summary> The reporter's primary key. </summary>
        public ulong Id { get; set; }

        /// <summary> The Discord id of the reporter. </summary>
        public ulong UserId { get; set; }

        /// <summary> The Discord id of the server. </summary>
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