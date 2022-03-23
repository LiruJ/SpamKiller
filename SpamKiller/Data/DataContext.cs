using Microsoft.EntityFrameworkCore;
using SpamKiller.Blacklist;
using SpamKiller.Reporting;
using SpamKiller.Settings;
using System.Configuration;

namespace SpamKiller.Data
{
    /// <summary> Handles interaction with the local database. </summary>
    public class DataContext : DbContext
    {
        #region Constants
        /// <summary> The connection string used to connect to the local database. </summary>
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["main"].ConnectionString;
        #endregion

        #region Properties
        /// <summary> The users who are still banned. </summary>
        public DbSet<UserBan> BannedUsers { get; set; }

        /// <summary> The users who were previously banned, but were exonerated. </summary>
        public DbSet<UserPreviousBan> PreviousBannedUsers { get; set; }

        /// <summary> The users who can ban users. </summary>
        public DbSet<ScamReporter> ScamReporters { get; set; }

        /// <summary> The settings for each server the bot is in. </summary>
        public DbSet<ServerSettings> ServerSettings { get; set; }
        #endregion

        #region Initialisation Functions
        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(connectionString);
        #endregion
    }
}