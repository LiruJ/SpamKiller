using SpamKiller.Reporting;
using System;

namespace SpamKiller.Blacklist
{
    /// <summary> Represents the reason for a user being blacklisted. </summary>
    public enum BanReason
    {
        /// <summary> The user is a spammer (sends invite links in DMs). </summary>
        Spammer,

        /// <summary> The user is a scammer (tries to obtain wallet information or otherwise steal). </summary>
        Scammer
    }

    /// <summary> Represents a ban, including the user who was banned, the ban date/time, and the person who banned. </summary>
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