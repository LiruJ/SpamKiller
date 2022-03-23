using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SpamKiller.Bot;
using SpamKiller.Data;
using SpamKiller.Reporting;
using System;
using System.Threading.Tasks;

namespace SpamKiller.Blacklist
{
    /// <summary> The result of a banning. </summary>
    public enum BlacklistResult
    {
        /// <summary> The user was successfully banned. </summary>
        Success,

        /// <summary> The given user was invalid. </summary>
        InvalidUser,

        /// <summary> The given banner is not a registered reporter on the server. </summary>
        ReporterNotRegistered,

        /// <summary> The given user is a reporter, and cannot be banned. </summary>
        ReporterUser,

        /// <summary> The given reporter has banned too recently. </summary>
        ReporterCooldown,

        /// <summary> The given user is already banned. </summary>
        UserAlreadyBanned
    }

    /// <summary> The result of an exoneration (unbanning). </summary>
    public enum ExonerateResult
    {
        /// <summary> The user was succesfully exonerated. </summary>
        Success,

        /// <summary> The given user was invalid. </summary>
        InvalidUser,

        /// <summary> The given banner is not a registered reporter on the server. </summary>
        ReporterNotRegistered,

        /// <summary> The given reporter has unbanned too recently. </summary>
        ReporterCooldown,

        /// <summary> The given user is not banned. </summary>
        UserAlreadyUnbanned
    }

    /// <summary> Manages the database of banned and unbanned users. </summary>
    public class BlacklistManager
    {
        #region Dependencies
        private readonly ServerBotManager botManager;
        private readonly ReporterManager reporterManager;
        private readonly DataContext context;
        #endregion

        #region Constructors
        public BlacklistManager(ServerBotManager botManager, ReporterManager reporterManager, DataContext context)
        {
            this.botManager = botManager;
            this.reporterManager = reporterManager;
            this.context = context;
        }
        #endregion

        #region Ban Functions
        /// <summary> Gets the information about the given <paramref name="victim"/>'s ban. </summary>
        /// <param name="victim"> The user who was banned. </param>
        /// <returns> The ban information about the given user, or <c>null</c> if the user is not banned. </returns>
        public async Task<UserBan> GetBanInfoOfAsync(IUser victim)
            => await context.BannedUsers
            .Include(x => x.Reporter)
            .FirstOrDefaultAsync(x => x.BannedUserId == victim.Id);

        /// <summary> Blacklists the given <paramref name="victim"/> from the given <paramref name="banner"/> for the given <paramref name="banReason"/>. </summary>
        /// <param name="banner"> The user who is banning the <paramref name="victim"/>. </param>
        /// <param name="victim"> The user who is being banned. </param>
        /// <param name="banReason"> The reason for the ban. </param>
        /// <returns> The <see cref="BlacklistResult"/> of the ban. </returns>
        public async Task<BlacklistResult> BlacklistAsync(SocketGuildUser banner, IUser victim, BanReason banReason)
        {
            // Ensure the victim is valid.
            if (victim == null || victim.IsBot || victim.IsWebhook || victim.Id == banner.Id) return BlacklistResult.InvalidUser;
            if (await context.ScamReporters.AnyAsync(x => x.UserId == victim.Id)) return BlacklistResult.ReporterUser;

            // Get the reporter, ensuring they are registered.
            ScamReporter reporter = await reporterManager.GetScamReporterAsync(banner);
            if (reporter == null) return BlacklistResult.ReporterNotRegistered;

            // Ensure the reporter's ban cooldown is up.
            if (reporter.LastBanTime.HasValue && DateTime.UtcNow - reporter.LastBanTime.Value < Constants.BanWaitTime)
                return BlacklistResult.ReporterCooldown;

            // If the user was not successfully added to the database, do nothing.
            if (!await addBannedUserToDatabaseAsync(reporter, victim, banReason)) return BlacklistResult.UserAlreadyBanned;

            // Set the last time of the banner's ban to now, and increment their ban counter.
            reporter.LastBanTime = DateTime.UtcNow;
            reporter.BanCount++;
            context.ScamReporters.Update(reporter);
            await context.SaveChangesAsync();

            // Ban the user in every server.
            foreach (ServerBot serverBot in botManager.ServerInstancesByServerId.Values)
                await serverBot.BanUserAsync(banner, victim, banReason);

            // Return success.
            return BlacklistResult.Success;
        }

        /// <summary> Exonerates (unblacklists) the given <paramref name="victim"/> from the given <paramref name="unbanner"/> for the given <paramref name="reason"/>. </summary>
        /// <param name="unbanner"> The user who is unbanning the <paramref name="victim"/>. </param>
        /// <param name="victim"> The <paramref name="victim"/> who is being unbanned. </param>
        /// <param name="reason"> The reason for the unbanning. </param>
        /// <returns> The <see cref="ExonerateResult"/> of the unbanning. </returns>
        public async Task<ExonerateResult> ExonerateAsync(SocketGuildUser unbanner, IUser victim, string reason)
        {
            // Get the reporter, ensuring they are registered.
            ScamReporter reporter = await reporterManager.GetScamReporterAsync(unbanner);
            if (reporter == null) return ExonerateResult.ReporterNotRegistered;

            // Ensure the reporter's unban cooldown is up.
            if (reporter.LastUnbanTime.HasValue && reporter.LastUnbanTime - DateTime.UtcNow < Constants.UnbanWaitTime) return ExonerateResult.ReporterCooldown;

            // Ensure the user is banned.
            UserBan bannedUser = await GetBanInfoOfAsync(victim);
            if (bannedUser == null) return ExonerateResult.UserAlreadyUnbanned;

            // Set the last time of the banner's unban to now, and decrement their ban counter.
            reporter.LastUnbanTime = DateTime.UtcNow;
            reporter.BanCount--;
            context.ScamReporters.Update(reporter);
            await context.SaveChangesAsync();

            // Remove the banned user from the database.
            await exonerateUserInDatabaseAsync(bannedUser, reporter, reason);

            // Unban the user from every server.
            foreach (ServerBot serverBot in botManager.ServerInstancesByServerId.Values)
                await serverBot.ExonerateUserAsync(unbanner, victim, reason);

            // Return success.
            return ExonerateResult.Success;
        }

        /// <summary> Adds the given <paramref name="victim"/> to the <see cref="DataContext.BannedUsers"/> table. </summary>
        /// <param name="banner"> The reporter who is banning. </param>
        /// <param name="victim"> The user who is being banned. </param>
        /// <param name="banReason"> The reason for the ban. </param>
        /// <returns> <c>true</c> if the user was added to the database successfully; otherwise <c>false</c>. </returns>
        private async Task<bool> addBannedUserToDatabaseAsync(ScamReporter banner, IUser victim, BanReason banReason)
        {
            // Ensure the user is not already banned.
            if (await context.BannedUsers.AnyAsync(x => x.BannedUserId == victim.Id)) return false;

            // Add the user to the database.
            await context.AddAsync(new UserBan()
            {
                BannedUserId = victim.Id,
                BanReason = banReason,
                Reporter = banner,
                BanDate = DateTime.UtcNow,
            });

            // Save the changes and return true.
            await context.SaveChangesAsync();
            return true;
        }

        /// <summary> Removes the ban from the given <paramref name="userBan"/>, and adds to the <see cref="DataContext.PreviousBannedUsers"/> table. </summary>
        /// <param name="userBan"> The ban to remove. </param>
        /// <param name="unbanner"> The reporter who is unbanning. </param>
        /// <param name="reason"> The reason for the unbanning. </param>
        /// <returns> The status of the task. </returns>
        private async Task exonerateUserInDatabaseAsync(UserBan userBan, ScamReporter unbanner, string reason)
        {
            // Remove the ban.
            context.Remove(userBan);

            // Add the ban as a previous ban.
            UserPreviousBan previousBan = new(userBan, unbanner, DateTime.UtcNow, reason);
            context.Add(previousBan);

            // Save the changes.
            await context.SaveChangesAsync();
        }
        #endregion
    }
}