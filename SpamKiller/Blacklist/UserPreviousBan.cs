using SpamKiller.Reporting;
using System;

namespace SpamKiller.Blacklist
{
    /// <summary> Represents a historic ban of a user, including the full ban information as well as some data about the unbanning. </summary>
    /// <remarks> Note that even if this record exists of a user, they may have been banned again. </remarks>
    public class UserPreviousBan
    {
        #region Properties
        /// <summary> The primary key of this previous ban. </summary>
        public ulong Id { get; set; }

        /// <inheritdoc cref="UserBan.BannedUserId"/>
        public ulong BannedUserId { get; set; }

        /// <inheritdoc cref="UserBan.BanReason"/>
        public BanReason BanReason { get; set; }

        /// <summary> The reason for the unban. </summary>
        public string UnbanReason { get; set; }

        /// <inheritdoc cref="UserBan.ReporterId"/>
        public ulong BanningReporterId { get; set; }

        /// <inheritdoc cref="UserBan.Reporter"/>
        public ScamReporter BanningReporter { get; set; }

        /// <summary> The id of the scam reporter who unbanned this user, relational to the ScamReporter table. </summary>
        public ulong UnbanningReporterId { get; set; }

        /// <summary> The scam reporter who unbanned this user. </summary>
        public ScamReporter UnbanningReporter { get; set; }

        /// <inheritdoc cref="UserBan.BanDate"/>
        public DateTime BanDate { get; set; }

        /// <summary> The date and time of the unban. </summary>
        public DateTime UnbanDate { get; set; }
        #endregion

        #region Constructors
        public UserPreviousBan() { }

        /// <summary> Creates a previous ban record using the given <paramref name="userBan"/> data and extra unbanning data. </summary>
        /// <param name="userBan"> The original ban. </param>
        /// <param name="unbanner"> The reporter who is unbanning the user. </param>
        /// <param name="unbanDate"> The date of the unbanning. </param>
        /// <param name="reason"> The reason for the unbanning. </param>
        public UserPreviousBan(UserBan userBan, ScamReporter unbanner, DateTime unbanDate, string reason)
        {
            // Set the ban data.
            BannedUserId = userBan.BannedUserId;
            if (userBan.Reporter != null) BanningReporter = userBan.Reporter;
            else BanningReporterId = userBan.ReporterId;
            BanReason = userBan.BanReason;
            BanDate = userBan.BanDate;

            // Set the unban data.
            UnbanReason = reason;
            UnbanningReporter = unbanner;
            UnbanDate = unbanDate;
        }
        #endregion
    }
}