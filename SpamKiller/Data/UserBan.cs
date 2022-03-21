using System;

namespace SpamKiller.Data
{
    public enum BanReason
    {
        Spammer,
        Scammer
    }

    public class UserBan
    {
        #region Properties
        /// <summary> The primary key of the ban. </summary>
        public ulong Id { get; set; }

        /// <summary> The Discord id of the banned user. </summary>
        public ulong BannedUserId { get; set; }

        /// <summary> The reason for the ban. </summary>
        public BanReason BanReason { get; set; }

        /// <summary> The id of the scam reporter, relational to the ScamReporter table. </summary>
        public ulong ReporterId { get; set; }

        /// <summary> The scam reporter who banned the user. </summary>
        public ScamReporter Reporter { get; set; }

        /// <summary> The date and time of the ban. </summary>
        public DateTime BanDate { get; set; }
        #endregion
    }
}
