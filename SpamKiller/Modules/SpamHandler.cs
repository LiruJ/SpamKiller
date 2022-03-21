using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SpamKiller.Bot;
using SpamKiller.Data;
using System;
using System.Threading.Tasks;

namespace SpamKiller.Modules
{
    [Group("blacklist", "Admins only 😠")]
    public class SpamHandler : InteractionBlacklistBase
    {
        #region Dependencies
        private readonly BlacklistManager blacklistManager;
        #endregion

        #region Constructors
        public SpamHandler(BlacklistManager blacklistManager, ServerBotManager botManager) : base(botManager)
        {
            this.blacklistManager = blacklistManager;
        }
        #endregion

        #region Commands
        [SlashCommand("exonerate", "Removes a user from the blacklist on all servers.")]
        [RequireRole(Constants.ReporterRoleName)]
        public async Task ExonerateUser(SocketUser user, string reason)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Ensure the server is whitelisted.
            if (!await checkIfWhitelisted()) return;

            // Try exonerate the user.
            await exonerateUser(user, reason);
        }

        [SlashCommand("backlog", "Bans all users in the blacklist.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task BlacklistBacklog()
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Get the server instance.
            ServerBot serverBot = await checkIfServerRegistered();
            if (!await checkIfWhitelisted(serverBot)) return;
            
            // Ban all users in the database.
            await serverBot.UpdateBanList();

            await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "Successfully banned all backlogged users.");
        }

        [SlashCommand("spammer", "Bans a spammer on all servers.")]
        [RequireRole(Constants.ReporterRoleName)]
        public async Task BlacklistSpammer(SocketUser spammer)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Ensure the server is whitelisted.
            if (!await checkIfWhitelisted()) return;

            // Blacklist the user.
            await blacklistUser(spammer, BanReason.Spammer);
        }

        [SlashCommand("spammer-id", "Bans a spammer on all servers by their Discord id or username#tag combo.")]
        [RequireRole(Constants.ReporterRoleName)]
        public async Task BlacklistSpammerId(string spammerId)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Ensure the server is whitelisted.
            if (!await checkIfWhitelisted()) return;

            // Get the spammer.
            IUser spammer = await getUserFromString(spammerId);
            if (spammer == null) return;

            // Blacklist the user.
            await blacklistUser(spammer, BanReason.Spammer);
        }

        [SlashCommand("scammer", "Bans a scammer on all servers.")]
        [RequireRole(Constants.ReporterRoleName)]
        public async Task BlacklistScammer(SocketUser scammer)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Ensure the server is whitelisted.
            if (!await checkIfWhitelisted()) return;

            // Blacklist the user.
            await blacklistUser(scammer, BanReason.Scammer);
        }

        [SlashCommand("scammer-id", "Bans a scammer on all servers by their Discord id or username#tag combo.")]
        [RequireRole(Constants.ReporterRoleName)]
        public async Task BlacklistScammerId(string scammerId)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Ensure the server is whitelisted.
            if (!await checkIfWhitelisted()) return;

            // Get the scammer.
            IUser scammer = await getUserFromString(scammerId);
            if (scammer == null) return;

            // Blacklist the user.
            await blacklistUser(scammer, BanReason.Scammer);
        }

        [SlashCommand("exonerate-id", "Removes a user from the blacklist on all servers by their Discord id or username#tag combo.")]
        [RequireRole(Constants.ReporterRoleName)]
        public async Task ExonerateUserId(string userId, string reason)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Ensure the server is whitelisted.
            if (!await checkIfWhitelisted()) return;

            // Get the user.
            IUser user = await getUserFromString(userId);
            if (user == null) return;

            // Try exonerate the user.
            await exonerateUser(user, reason);
        }

        private async Task blacklistUser(IUser victim, BanReason banReason)
        {
            if (victim == null)
            {
                await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "Invalid user.");
                return;
            }

            // Blacklist the user.
            BlacklistResult result = await blacklistManager.Blacklist(Context.User as SocketGuildUser, victim, banReason);

            switch (result)
            {
                case BlacklistResult.Success:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "User was successfully blacklisted on all servers.");
                    break;
                case BlacklistResult.ReporterNotRegistered:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "You are not registered as a scam reporter. Please contact your server admin.");
                    break;
                case BlacklistResult.ReporterCooldown:

                    // Get the reporter.
                    ScamReporter reporter = await blacklistManager.GetScamReporterAsync(Context.User as SocketGuildUser);

                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"You can ban again <t:{(long)Math.Floor((reporter.LastBanTime.Value + Constants.BanWaitTime - DateTime.UnixEpoch).TotalSeconds)}:R>.");
                    break;
                case BlacklistResult.UserAlreadyBanned:

                    // Get the info about the ban.
                    UserBan bannedUser = await blacklistManager.GetInfoOf(victim);
                    IUser banner = await Context.Client.GetUserAsync(bannedUser.BannedUserId);
                    IGuild bannedServer = await Context.Client.GetGuildAsync(bannedUser.Reporter.ServerId);

                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"This user was already banned by {banner.Mention} on the \"{bannedServer.Name}\" server <t:{bannedUser.BanDate.Ticks - DateTime.UnixEpoch.Ticks}:R>.");
                    break;
                case BlacklistResult.InvalidUser:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"Invalid user. Cannot ban bots and such.");
                    break;
                case BlacklistResult.ReporterUser:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"Cannot ban another reporter.");
                    break;
                default:
                    break;
            }
        }

        private async Task exonerateUser(IUser user, string reason)
        {
            // Try exonerate the user.
            ExonerateResult result = await blacklistManager.Exonerate(Context.User as SocketGuildUser, user, reason);
            switch (result)
            {
                case ExonerateResult.Success:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "User was successfully exonerated on all servers.");
                    break;
                case ExonerateResult.ReporterNotRegistered:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "You are not registered as a scam reporter. Please contact your server admin.");
                    break;
                case ExonerateResult.ReporterCooldown:

                    // Get the reporter.
                    ScamReporter reporter = await blacklistManager.GetScamReporterAsync(Context.User as SocketGuildUser);

                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"You can unban again <t:{(long)Math.Floor((reporter.LastUnbanTime.Value + Constants.UnbanWaitTime - DateTime.UnixEpoch).TotalSeconds)}:R>.");
                    break;
                case ExonerateResult.UserAlreadyUnbanned:
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "This user is not banned.");
                    break;
                case ExonerateResult.InvalidUser:
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}
