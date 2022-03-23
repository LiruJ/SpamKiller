using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SpamKiller.Blacklist;
using SpamKiller.Bot;
using SpamKiller.Data;
using SpamKiller.Reporting;
using System;
using System.Threading.Tasks;

namespace SpamKiller.Modules
{
    [Group("blacklist", "Admins only 😠")]
    public class SpamHandler : InteractionBlacklistBase
    {
        #region Dependencies
        private readonly BlacklistManager blacklistManager;
        private readonly ReporterManager reporterManager;
        #endregion

        #region Constructors
        public SpamHandler(BlacklistManager blacklistManager, ReporterManager reporterManager, ServerBotManager botManager) : base(botManager)
        {
            this.blacklistManager = blacklistManager;
            this.reporterManager = reporterManager;
        }
        #endregion

        #region Commands
        [SlashCommand("exonerate", "Removes a user from the blacklist on all servers.")]
        [RequireRole(Constants.ReporterRoleName)]
        public async Task ExonerateUserAsync(SocketUser user, string reason)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Ensure the server is whitelisted.
            if (!await CheckIfWhitelistedAsync()) return;

            // Try exonerate the user.
            await exonerateUserAsync(user, reason);
        }

        [SlashCommand("backlog", "Bans all users in the blacklist.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task BlacklistBacklogAsync()
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Get the server instance.
            ServerBot serverBot = await CheckIfServerRegisteredAsync();

            // Ban all users in the database.
            await serverBot.BanBacklogAsync();

            // Tell the user that the backlog banning has been completed.
            await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "Successfully banned all backlogged users.");
        }

        [SlashCommand("spammer", "Bans a spammer on all servers.")]
        [RequireRole(Constants.ReporterRoleName)]
        public async Task BlacklistSpammerAsync(SocketUser spammer)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Ensure the server is whitelisted.
            if (!await CheckIfWhitelistedAsync()) return;

            // Blacklist the user.
            await blacklistUserAsync(spammer, BanReason.Spammer);
        }

        [SlashCommand("spammer-id", "Bans a spammer on all servers by their Discord id or username#tag combo.")]
        [RequireRole(Constants.ReporterRoleName)]
        public async Task BlacklistSpammerIdAsync(string spammerId)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Ensure the server is whitelisted.
            if (!await CheckIfWhitelistedAsync()) return;

            // Get the spammer.
            IUser spammer = await GetUserFromStringAsync(spammerId);
            if (spammer == null) return;

            // Blacklist the user.
            await blacklistUserAsync(spammer, BanReason.Spammer);
        }

        [SlashCommand("scammer", "Bans a scammer on all servers.")]
        [RequireRole(Constants.ReporterRoleName)]
        public async Task BlacklistScammerAsync(SocketUser scammer)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Ensure the server is whitelisted.
            if (!await CheckIfWhitelistedAsync()) return;

            // Blacklist the user.
            await blacklistUserAsync(scammer, BanReason.Scammer);
        }

        [SlashCommand("scammer-id", "Bans a scammer on all servers by their Discord id or username#tag combo.")]
        [RequireRole(Constants.ReporterRoleName)]
        public async Task BlacklistScammerIdAsync(string scammerId)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Ensure the server is whitelisted.
            if (!await CheckIfWhitelistedAsync()) return;

            // Get the scammer.
            IUser scammer = await GetUserFromStringAsync(scammerId);
            if (scammer == null) return;

            // Blacklist the user.
            await blacklistUserAsync(scammer, BanReason.Scammer);
        }

        [SlashCommand("exonerate-id", "Removes a user from the blacklist on all servers by their Discord id or username#tag combo.")]
        [RequireRole(Constants.ReporterRoleName)]
        public async Task ExonerateUserIdAsync(string userId, string reason)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Ensure the server is whitelisted.
            if (!await CheckIfWhitelistedAsync()) return;

            // Get the user.
            IUser user = await GetUserFromStringAsync(userId);
            if (user == null) return;

            // Try exonerate the user.
            await exonerateUserAsync(user, reason);
        }

        /// <summary> Attempts to blacklist the given <paramref name="victim"/> and modifies the original response to update the user on the status. </summary>
        /// <param name="victim"> The user who is being banned. </param>
        /// <param name="banReason"> The reason for the ban. </param>
        /// <returns> The status of the task. </returns>
        private async Task blacklistUserAsync(IUser victim, BanReason banReason)
        {
            if (victim == null)
            {
                await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "Invalid user.");
                return;
            }

            // Blacklist the user.
            BlacklistResult result = await blacklistManager.BlacklistAsync(Context.User as SocketGuildUser, victim, banReason);
            switch (result)
            {
                // The user was blacklisted successfully.
                case BlacklistResult.Success:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "User was successfully blacklisted on all servers.");
                    break;

                // The reporter was not registered.
                case BlacklistResult.ReporterNotRegistered:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "You are not registered as a scam reporter. Please contact your server admin.");
                    break;

                // The reporter has banned too recently.
                case BlacklistResult.ReporterCooldown:

                    // Get the reporter.
                    ScamReporter reporter = await reporterManager.GetScamReporterAsync(Context.User as SocketGuildUser);
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"You can ban again <t:{(long)Math.Floor((reporter.LastBanTime.Value + Constants.BanWaitTime - DateTime.UnixEpoch).TotalSeconds)}:R>.");
                    break;

                // The user is already banned.
                case BlacklistResult.UserAlreadyBanned:

                    // Get the info about the ban.
                    UserBan bannedUser = await blacklistManager.GetBanInfoOfAsync(victim);
                    IUser banner = await Context.Client.GetUserAsync(bannedUser.BannedUserId);
                    IGuild bannedServer = await Context.Client.GetGuildAsync(bannedUser.Reporter.ServerId);
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"This user was already banned by {banner.Mention} on the \"{bannedServer.Name}\" server <t:{bannedUser.BanDate.Ticks - DateTime.UnixEpoch.Ticks}:R>.");
                    break;

                // The user was invalid.
                case BlacklistResult.InvalidUser:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"Invalid user. Cannot ban bots and such.");
                    break;

                // The user is unbannable.
                case BlacklistResult.ReporterUser:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"Cannot ban another reporter.");
                    break;
                default:
                    break;
            }
        }

        /// <summary> Exonerates (unbans) the given <paramref name="user"/> for the given <paramref name="reason"/>. </summary>
        /// <param name="user"> The user to unban. </param>
        /// <param name="reason"> The reason for the unbanning. </param>
        /// <returns> The status of the task. </returns>
        private async Task exonerateUserAsync(IUser user, string reason)
        {
            // Try exonerate the user.
            ExonerateResult result = await blacklistManager.ExonerateAsync(Context.User as SocketGuildUser, user, reason);
            switch (result)
            {
                // The user was successfully unbanned.
                case ExonerateResult.Success:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "User was successfully exonerated on all servers.");
                    break;

                // The reporter was not registered.
                case ExonerateResult.ReporterNotRegistered:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "You are not registered as a scam reporter. Please contact your server admin.");
                    break;

                // The reporter unbanned too recently.
                case ExonerateResult.ReporterCooldown:

                    // Get the reporter.
                    ScamReporter reporter = await reporterManager.GetScamReporterAsync(Context.User as SocketGuildUser);
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"You can unban again <t:{(long)Math.Floor((reporter.LastUnbanTime.Value + Constants.UnbanWaitTime - DateTime.UnixEpoch).TotalSeconds)}:R>.");
                    break;

                // The user was not banned.
                case ExonerateResult.UserAlreadyUnbanned:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "This user is not banned.");
                    break;

                // The user was invalid.
                case ExonerateResult.InvalidUser:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"Invalid user. Cannot unban bots and such.");
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}