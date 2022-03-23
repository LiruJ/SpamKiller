using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SpamKiller.Bot;
using System.Threading.Tasks;

namespace SpamKiller.Modules
{
    [Group("settings", "Admins only 😠")]
    public class SettingsHandler : InteractionBlacklistBase
    {
        #region Constructors
        public SettingsHandler(ServerBotManager botManager) : base(botManager) { }
        #endregion

        #region Commands
        [UserCommand("Register as Reporter")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RegisterAsReporterAsync(IUser user)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Get the server instance.
            ServerBot serverBot = await CheckIfServerRegisteredAsync();
            if (!await CheckIfWhitelistedAsync(serverBot)) return;

            // Register the user as a reporter.
            if (await serverBot.RegisterReporterAsync(user as SocketGuildUser)) await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"Successfully registered {user.Mention} as a reporter.");
            else await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"Could not register {user.Mention} as a reporter, they probably are already registered.");
        }

        [SlashCommand("set-log-channel", "Sets the channel used in this server to log blacklisted users.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetLogChannelAsync(SocketTextChannel logChannel)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Get the server instance.
            ServerBot serverBot = await CheckIfServerRegisteredAsync();
            if (serverBot == null) return;

            // Set the log channel.
            bool result = await serverBot.ChangeLogChannelAsync(logChannel);

            // Log the result of the change.
            if (result) await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "Successfully changed the log channel.");
            else await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "Could not change log channel. Ensure it is part of this server and the bot can read/write into it.");
        }

        public enum WhitelistAction { Add, Revoke }

        [SlashCommand("whitelist-server", "Used by Liru to whitelist the server for global bans.")]
        [RequireOwner]
        public async Task WhitelistServerAsync(WhitelistAction whitelistAction)
        {
            // Acknowledge the command.
            await DeferAsync(ephemeral: true);

            // Get the server instance.
            ServerBot serverBot = await CheckIfServerRegisteredAsync();
            if (serverBot == null) return;

            // Handle the action.
            switch (whitelistAction)
            {
                case WhitelistAction.Add:
                    if (await serverBot.ChangeWhitelistStatusAsync(true))
                        await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "Server was successfully whitelisted!");
                    else
                        await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "Server is already whitelisted.");
                    break;
                case WhitelistAction.Revoke:
                    if (await serverBot.ChangeWhitelistStatusAsync(false))
                        await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "Server's whitelist status was successfully revoked.");
                    else
                        await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "Server is already not whitelisted.");
                    break;
            }
        }
        #endregion
    }
}