using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SpamKiller.Bot;
using System;
using System.Threading.Tasks;

namespace SpamKiller.Data
{
    public enum BlacklistResult
    {
        Success,
        InvalidUser,
        ReporterUser,
        ReporterNotRegistered,
        ReporterCooldown,
        UserAlreadyBanned
    }
    
    public enum ExonerateResult
    {
        Success,
        InvalidUser,
        ReporterNotRegistered,
        ReporterCooldown,
        UserAlreadyUnbanned
    }

    public class BlacklistManager
    {
        #region Dependencies
        private readonly ServerBotManager botManager;
        private readonly DataContext context;
        #endregion

        #region Constructors
        public BlacklistManager(ServerBotManager botManager, DataContext context)
        {
            this.botManager = botManager;
            this.context = context;
        }
        #endregion

        #region Reporter Functions
        public async Task<ScamReporter> GetScamReporterAsync(SocketGuildUser user)
            => await context.ScamReporters.FirstOrDefaultAsync(x => x.UserId == user.Id && x.ServerId == user.Guild.Id);
        #endregion

        #region Ban Functions
        public async Task<UserBan> GetInfoOf(IUser victim) 
            => await context.BannedUsers
            .Include(x => x.Reporter)
            .FirstOrDefaultAsync(x => x.BannedUserId == victim.Id);

        public async Task<BlacklistResult> Blacklist(SocketGuildUser banner, IUser victim, BanReason banReason)
        {
            // Ensure the victim is valid.
            if (victim == null || victim.IsBot || victim.IsWebhook || victim.Id == banner.Id) return BlacklistResult.InvalidUser;
            if (await context.ScamReporters.AnyAsync(x => x.UserId == victim.Id)) return BlacklistResult.ReporterUser;

            // Get the reporter, ensuring they are registered.
            ScamReporter reporter = await GetScamReporterAsync(banner);
            if (reporter == null) return BlacklistResult.ReporterNotRegistered;

            // Ensure the reporter's ban cooldown is up.
            if (reporter.LastBanTime.HasValue && (DateTime.UtcNow - reporter.LastBanTime.Value) < Constants.BanWaitTime) 
                return BlacklistResult.ReporterCooldown;

            // If the user was not successfully added to the database, do nothing.
            if (!await addBannedUserToDatabase(reporter, victim, banReason)) return BlacklistResult.UserAlreadyBanned;

            // Set the last time of the banner's ban to now, and increment their ban counter.
            reporter.LastBanTime = DateTime.UtcNow;
            reporter.BanCount++;
            context.ScamReporters.Update(reporter);
            await context.SaveChangesAsync();

            // Ban the user in every server.
            foreach (ServerBot serverBot in botManager.ServerInstancesByServerId.Values)
                await serverBot.BanUser(banner, victim, banReason);

            // Return success.
            return BlacklistResult.Success;
        }

        public async Task<ExonerateResult> Exonerate(SocketGuildUser unbanner, IUser victim, string reason)
        {
            // Get the reporter, ensuring they are registered.
            ScamReporter reporter = await GetScamReporterAsync(unbanner);
            if (reporter == null) return ExonerateResult.ReporterNotRegistered;

            // Ensure the reporter's unban cooldown is up.
            if (reporter.LastUnbanTime.HasValue && (reporter.LastUnbanTime - DateTime.UtcNow) < Constants.UnbanWaitTime) return ExonerateResult.ReporterCooldown;

            // Ensure the user is banned.
            UserBan bannedUser = await GetInfoOf(victim);
            if (bannedUser == null) return ExonerateResult.UserAlreadyUnbanned;

            // Set the last time of the banner's unban to now, and decrement their ban counter.
            reporter.LastUnbanTime = DateTime.UtcNow;
            reporter.BanCount--;
            context.ScamReporters.Update(reporter);
            await context.SaveChangesAsync();

            // Remove the banned user from the database.
            await exonerateUserInDatabase(bannedUser, reporter, reason);

            // Unban the user from every server.
            foreach (ServerBot serverBot in botManager.ServerInstancesByServerId.Values)
                await serverBot.ExonerateUser(unbanner, victim, reason);

            // Return success.
            return ExonerateResult.Success;
        }

        private async Task<bool> addBannedUserToDatabase(ScamReporter banner, IUser spammer, BanReason banReason)
        {
            // Ensure the user is not already banned.
            if (await context.BannedUsers.AnyAsync(x => x.BannedUserId == spammer.Id)) return false;

            // Add the user to the database.
            await context.AddAsync(new UserBan()
            {
                BannedUserId = spammer.Id,
                BanReason = banReason,
                Reporter = banner,
                BanDate = DateTime.UtcNow,
            });

            // Save the changes and return true.
            await context.SaveChangesAsync();
            return true;
        }

        private async Task exonerateUserInDatabase(UserBan userBan, ScamReporter unbanner, string reason)
        {
            // Remove the ban.
            context.Remove(userBan);

            // Add the ban as a previous ban.
            UserPreviousBan previousBan = new (userBan, unbanner, DateTime.UtcNow, reason);
            context.Add(previousBan);

            // Save the changes.
            await context.SaveChangesAsync();
        }
        #endregion
    }
}
