using Discord;
using Discord.Rest;
using Discord.WebSocket;
using SpamKiller.Blacklist;
using SpamKiller.Data;
using SpamKiller.Reporting;
using SpamKiller.Settings;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SpamKiller.Bot
{
    /// <summary> Represents the bot as it exists on a server. </summary>
    public class ServerBot
    {
        #region Constants
        /// <summary> The icon used in logs to represent scammers. </summary>
        private const char scammerIcon = '⛔';

        /// <summary> The icon used in logs to represent spammers. </summary>
        private const char spammerIcon = '⚠';
        #endregion

        #region Dependencies
        private readonly DataContext context;
        private readonly DiscordSocketClient client;
        #endregion

        #region Properties
        /// <summary> The server of this instance. </summary>
        public SocketGuild Server { get; }

        /// <summary> The text channel used to log events. </summary>
        public SocketTextChannel LogChannel { get; private set; }

        /// <summary> The role in this server for reporters. </summary>
        public SocketRole ReporterRole { get; private set; }

        /// <summary> The settings of this server. </summary>
        public ServerSettings Settings { get; }
        #endregion

        #region Constructors
        private ServerBot(DataContext context, DiscordSocketClient client, SocketGuild server, ServerSettings settings, SocketTextChannel logChannel = null)
        {
            // Set the dependencies.
            this.context = context;
            this.client = client;

            // Set the server data.
            Server = server;
            Settings = settings;
            LogChannel = logChannel;
        }
        #endregion

        #region Ban Functions
        /// <summary> Ban the given <paramref name="victim"/> from the given <paramref name="banner"/> for the given <paramref name="banReason"/>. </summary>
        /// <param name="banner"> The user who is banning the <paramref name="victim"/>. </param>
        /// <param name="victim"> The user who is being banned. </param>
        /// <param name="banReason"> The reason for the ban. </param>
        /// <returns> The result of the ban. </returns>
        public async Task<bool> BanUserAsync(SocketGuildUser banner, IUser victim, BanReason banReason)
        {
            // Get any existing ban for the user.
            RestBan existingBan = await Server.GetBanAsync(victim);

            // If the user is already banned, just log.
            if (existingBan != null)
            {
                // Log the existing ban.
                await LogInChannelAsync($"{victim.Mention} was blacklisted by {banner.DisplayName}, but was already banned on this server.");

                // Return false, as the user was technically not banned.
                return false;
            }

            // Ban the user.
            try { await Server.AddBanAsync(victim, 0, $"DeFi Shield: {banReason}"); }
            catch (Exception) { await LogInChannelAsync($"Could not ban {victim.Mention}, do I have ban permissions?"); return false; }

            // Log the ban.
            await LogInChannelAsync($"{(banReason == BanReason.Scammer ? scammerIcon : spammerIcon)} {victim.Mention} was banned by {banner.DisplayName} from {banner.Guild.Name}.");

            // Return true, as the user was banned.
            return true;
        }

        /// <summary> Exonerates (unblacklists) the given <paramref name="victim"/> from the given <paramref name="unbanner"/> for the given <paramref name="reason"/>. </summary>
        /// <param name="unbanner"> The user who is unbanning the <paramref name="victim"/>. </param>
        /// <param name="victim"> The <paramref name="victim"/> who is being unbanned. </param>
        /// <param name="reason"> The reason for the unbanning. </param>
        /// <returns> The <see cref="ExonerateResult"/> of the unbanning. </returns>
        public async Task<bool> ExonerateUserAsync(SocketGuildUser unbanner, IUser victim, string reason)
        {
            // Get any existing ban for the user.
            RestBan existingBan = await Server.GetBanAsync(victim);

            // If the user is not banned, just log.
            if (existingBan == null)
            {
                // Log the lack of existing ban.
                await LogInChannelAsync($"{victim.Mention} was exonerated by {unbanner.DisplayName}, but was not banned on this server.");

                // Return false, as the user was technically not exonerated.
                return false;
            }

            // Remove the ban.
            try { await Server.RemoveBanAsync(victim); }
            catch (Exception) { await LogInChannelAsync($"Could not exonerate {victim.Mention}, do I have unban permissions?"); return false; }

            // Log the exoneration.
            await LogInChannelAsync($"{victim.Mention} was exonerated by {unbanner.DisplayName} from server \"{unbanner.Guild.Name}\": {reason}");

            // Return true, as the user was exonerated.
            return true;
        }

        /// <summary> Goes through every globally blacklisted user and bans them in the server. </summary>
        /// <returns> The status of the task. </returns>
        public async Task BanBacklogAsync()
        {
            // Go over each banned user and ban them.
            await context.BannedUsers.ForEachAsync(async ban =>
            {
                // Get the user to ban.
                IUser victim = await client.GetUserAsync(ban.BannedUserId);

                // Get any existing ban for the user.
                RestBan existingBan = await Server.GetBanAsync(victim);

                // If the user is already banned, just log.
                if (existingBan != null)
                    await LogInChannelAsync($"{existingBan.User.Mention} was blacklisted but was already banned on this server.");
                // Otherwise; ban the user.
                else
                {
                    // Ban the user on the server.
                    await Server.AddBanAsync(victim, 0, $"DeFi Shield: {ban.BanReason}");

                    // Log the ban.
                    await LogInChannelAsync($"{(ban.BanReason == BanReason.Scammer ? scammerIcon : spammerIcon)} {victim.Mention} was banned from the backlog of banned users.");
                }
            });

            // Log that the banning is complete.
            await LogInChannelAsync("Successfully banned all backlogged users.");
        }
        #endregion

        #region Role Functions
        /// <summary> Gets or creates the reporter role for this server. </summary>
        /// <returns> The gotten or created role. </returns>
        public async Task<SocketRole> GetOrCreateScamReporterRoleAsync()
        {
            // If the role id is null, create the role.
            if (Settings.ReporterRoleId == null)
            {
                // Create the role.
                RestRole restRole;
                try { restRole = await Server.CreateRoleAsync(Constants.ReporterRoleName); }
                catch (Exception) { await LogInChannelAsync("Could not create reporter role."); return null; }

                // Save the id in the settings.
                Settings.ReporterRoleId = restRole.Id;
                context.ServerSettings.Update(Settings);
                await context.SaveChangesAsync();
            }

            // Return the created role.
            SocketRole role = Server.GetRole(Settings.ReporterRoleId.Value);
            ReporterRole = role;
            return role;
        }

        /// <summary> Registers the given user as a reporter on this server. </summary>
        /// <param name="user"> The user to register. </param>
        /// <returns> <c>true</c> if the user was successfully registered; otherwise <c>false</c>. </returns>
        public async Task<bool> RegisterReporterAsync(SocketGuildUser user)
        {
            // Ensure the given user is part of this server.
            if (user.Guild.Id != Server.Id) return false;

            // Don't do anything if the user is already a reporter on this server.
            if (await context.ScamReporters.AnyAsync(x => x.UserId == user.Id && x.ServerId == user.Guild.Id)) return false;

            // Get the role to assign.
            SocketRole role = await GetOrCreateScamReporterRoleAsync();

            // Ensure the role was created.
            if (role == null) return false;

            // Try assign the role to the user.
            try { await user.AddRoleAsync(role.Id); }
            catch (Exception)
            {
                await LogInChannelAsync($"Could not assign role to {user.Mention}, do I have permissions to manage users?");
                return false;
            }

            // Create the reporter.
            ScamReporter reporter = new()
            {
                UserId = user.Id,
                ServerId = user.Guild.Id,
            };

            // Save the database changes.
            await context.ScamReporters.AddAsync(reporter);
            await context.SaveChangesAsync();

            // Return true as the registration was a success.
            return true;
        }
        #endregion

        #region Log Functions
        /// <summary> Logs the given <paramref name="message"/> in the <see cref="LogChannel"/>, if it exists. </summary>
        /// <param name="message"> The message to log. </param>
        /// <returns> The status of the task. </returns>
        public async Task LogInChannelAsync(string message)
        {
            try { await LogChannel?.SendMessageAsync(message); }
            catch (Exception e) { Console.WriteLine($"Log channel with id {LogChannel.Id} on server {Server.Name} could not be written in:\n{e}"); }
        }
        #endregion

        #region Setting Functions
        /// <summary> Changes the channel used to log events. </summary>
        /// <param name="logChannel"> The new channel to use. Must be part of this <see cref="Server"/>. </param>
        /// <returns> <c>true</c> if the change was successful; otherwise <c>false</c>. </returns>
        public async Task<bool> ChangeLogChannelAsync(SocketTextChannel logChannel)
        {
            // Ensure the log channel is valid.
            if (logChannel == null || logChannel.Guild != Server) return false;

            // Check to see if a message can be sent.
            try { await logChannel.SendMessageAsync($"Now using {logChannel.Mention} for logging."); }
            catch (Exception) { await LogInChannelAsync($"Could not use {logChannel.Mention} for logging. Do I have permissions to read and write?"); return false; }

            // Set the log channel.
            LogChannel = logChannel;

            // Save the settings.
            Settings.LogChannelId = logChannel.Id;
            context.ServerSettings.Update(Settings);
            await context.SaveChangesAsync();

            // Return true, as the changes were made successfully.
            return true;
        }

        /// <summary> Changes the whitelist status of this server. </summary>
        /// <param name="status"> The new status. </param>
        /// <returns> <c>true</c> if the change was successful; otherwise <c>false</c>. </returns>
        public async Task<bool> ChangeWhitelistStatusAsync(bool status)
        {
            // Ensure a change will be made.
            if (status == Settings.IsWhitelisted) return false;

            // Set the whitelist status of the server.
            Settings.IsWhitelisted = status;

            // Save the settings.
            context.ServerSettings.Update(Settings);
            await context.SaveChangesAsync();

            // Return true, as the changes were made successfully.
            return true;
        }

        /// <summary> Attempts to get the settings for the given <paramref name="serverId"/>, if none exist then they are created. </summary>
        /// <param name="context"> The database context to load/save from/to. </param>
        /// <param name="serverId"> The Discord id of the server. </param>
        /// <returns> The loaded or created settings. </returns>
        private static async Task<ServerSettings> getOrCreateSettingsAsync(DataContext context, ulong serverId)
        {
            // Find the settings for this server.
            ServerSettings serverSettings = await context.ServerSettings.FirstOrDefaultAsync(x => x.ServerId == serverId);

            // If the settings do not exist, then create them.
            if (serverSettings == null)
            {
                // Create the server settings.
                serverSettings = new()
                {
                    ServerId = serverId,
                    IsWhitelisted = false,
                };

                // Save the database.
                await context.ServerSettings.AddAsync(serverSettings);
                await context.SaveChangesAsync();
            }

            // Return the server settings.
            return serverSettings;
        }
        #endregion

        #region Creation Functions
        /// <summary> Creates a bot instance, loading or creating the settings for this server. </summary>
        /// <param name="context"> The database context. </param>
        /// <param name="client"> The bot's main connection to the Discord server. </param>
        /// <param name="server"> The server this instance will represent. </param>
        /// <returns> The created bot instance. </returns>
        public static async Task<ServerBot> CreateAsync(DataContext context, DiscordSocketClient client, SocketGuild server)
        {
            // Load the settings for the server.
            ServerSettings settings = await getOrCreateSettingsAsync(context, server.Id);

            // Fetch the log channel, if one was defined.
            SocketTextChannel logChannel = settings.LogChannelId.HasValue ? server.GetTextChannel(settings.LogChannelId.Value) : null;

            // Create the server.
            return new ServerBot(context, client, server, settings, logChannel);
        }
        #endregion
    }
}