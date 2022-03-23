using Discord;
using Discord.Interactions;
using SpamKiller.Bot;
using SpamKiller.Data;
using System.Configuration;
using System.Threading.Tasks;

namespace SpamKiller.Modules
{
    [Group("info", "Gets information about things")]
    public class InfoHandler : InteractionBlacklistBase
    {
        #region Constructors
        public InfoHandler(ServerBotManager botManager) : base(botManager)
        {
        }
        #endregion

        #region Commands
        [SlashCommand("bot", "Gets information about the bot.")]
        public async Task GetInfoAsync()
        {
            // Send the info message.
            await RespondAsync($"DeFi Shield version {ConfigurationManager.AppSettings["version"]} by Liru\n" +
                               $"Donations keep the bot running!\n" +
                               $"Eth: {ConfigurationManager.AppSettings["donationsWalletEth"]}\n" +
                               $"Sol: {ConfigurationManager.AppSettings["donationsWalletSol"]}", ephemeral: true);
        }

        [SlashCommand("setup", "Gets instructions for setting up the bot, users with admin permissions only.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task GetSetupInfoAsync()
        {
            // Get Liru.
            IUser liru = await Context.Client.GetUserAsync(Constants.LiruId);

            // Send the help message.
            await RespondAsync($"1. Set a log channel with \"/settings set-log-channel\", this is where all global bans will be logged.\n" +
                               $"2. People with administrator permission can right click on a user, Apps>Register as Reporter.\n" +
                               $"This allows them to globally ban users, so please only give it to trusted members. Even the admins need to do this.\n" +
                               $"3. {liru.Username}#{liru.Discriminator} must manually whitelist your server before you can ban users. Please DM her.\n" +
                               $"This prevents random people from adding the bot to private servers and banning/unbanning people.\n" +
                               $"4. Once the server has been whitelisted, the \"/blacklist backlog\" command will ban all users on the blacklist.\n" +
                               $"5. Scam reporters can then globally ban users with the \"/blacklist spammer/scammer\" commands, and unban with the \"/blacklist exonerate\" command.\n" +
                               $"Banning has a short cooldown, and unbanning has quite a long cooldown, this is to prevent abuse.", ephemeral: true);
        }
        #endregion
    }
}