using Discord;
using Discord.Rest;
using Discord.WebSocket;
using SpamKiller.Data;
using SpamKiller.Settings;
using System.Linq;
using System.Threading.Tasks;

namespace SpamKiller.Bot
{
    /// <summary> Represents the bot as it exists on a server. </summary>
    public class ServerBot
    {
        #region Dependencies
        private readonly DataContext context;
        private readonly DiscordSocketClient client;
        #endregion

        #region Properties
        public SocketGuild Server { get; }

        public SocketTextChannel LogChannel { get; private set; }

        public SocketRole ReporterRole { get; private set; }

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
        public async Task BanUser(SocketGuildUser banner, IUser victim, BanReason banReason)
        {
            await Server.AddBanAsync(victim, 0, $"DeFi Shield: {banReason}");

            if (LogChannel != null)
                await LogChannel.SendMessageAsync($"{victim.Mention} was banned by {banner.Mention} from server \"{banner.Guild.Name}\": {banReason}");
        }

        public async Task UpdateBanList()
        {
            // Go over each banned user and ban them.
            await context.BannedUsers.ForEachAsync(async ban =>
            {
                // Get the user to ban.
                IUser victim = await client.GetUserAsync(ban.BannedUserId);

                // Ban the user on the server.
                await Server.AddBanAsync(victim, 0, $"DeFi Shield: {ban.BanReason}");

                // Log the ban.
                if (LogChannel != null)
                    await LogChannel.SendMessageAsync($"{victim.Mention} was banned from the backlog of banned users.");
            });
        }

        public async Task ExonerateUser(SocketGuildUser unbanner, IUser victim, string reason)
        {
            await Server.RemoveBanAsync(victim);

            if (LogChannel != null)
                await LogChannel.SendMessageAsync($"{victim.Mention} was exonerated by {unbanner.Mention} from server \"{unbanner.Guild.Name}\": {reason}");
        }
        #endregion

        #region Role Functions
        public async Task<SocketRole> GetOrCreateScamReporterRoleAsync()
        {
            // If the role id is null, create the role.
            if (Settings.ReporterRoleId == null)
            {
                // Create the role.
                RestRole restRole = await Server.CreateRoleAsync(Constants.ReporterRoleName);

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

        public async Task<bool> RegisterReporterAsync(SocketGuildUser user)
        {
            // Don't do anything if the user is already a reporter on this server.
            if (await context.ScamReporters.AnyAsync(x => x.UserId == user.Id && x.ServerId == user.Guild.Id)) return false;

            // Get the role to assign.
            SocketRole role = await GetOrCreateScamReporterRoleAsync();

            // Create the reporter.
            ScamReporter reporter = new()
            {
                UserId = user.Id,
                ServerId = user.Guild.Id,
            };

            // Save the database changes.
            await context.ScamReporters.AddAsync(reporter);
            await context.SaveChangesAsync();

            // Assign the role to the caller.
            await user.AddRoleAsync(role.Id);

            // Return true as the registration was a success.
            return true;
        }
        #endregion

        #region Setting Functions
        public async Task<bool> ChangeLogChannel(SocketTextChannel logChannel)
        {
            // Ensure the log channel is valid.
            if (logChannel == null || logChannel.Guild != Server) return false;

            // Check to see if a message can be sent.
            try { await logChannel.SendMessageAsync($"Now using {logChannel.Mention} for logging.");}
            catch (System.Exception) { return false; }

            // Set the log channel.
            LogChannel = logChannel;

            // Save the settings.
            Settings.LogChannelId = logChannel.Id;
            context.ServerSettings.Update(Settings);
            await context.SaveChangesAsync();

            // Return true, as the changes were made successfully.
            return true;
        }

        public async Task<bool> ChangeWhitelistStatus(bool status)
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

        private static async Task<ServerSettings> initialiseSettings(DataContext context, ulong serverId)
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
        public static async Task<ServerBot> Create(DataContext context, DiscordSocketClient client, SocketGuild server)
        {
            // Load the settings for the server.
            ServerSettings settings = await initialiseSettings(context, server.Id);

            // Fetch the log channel, if one was defined.
            SocketTextChannel logChannel = settings.LogChannelId.HasValue ? server.GetTextChannel(settings.LogChannelId.Value) : null;

            // Create the server.
            return new ServerBot(context, client, server, settings, logChannel);
        }
        #endregion
    }
}