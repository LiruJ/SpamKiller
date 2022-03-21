using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SpamKiller.Bot;
using SpamKiller.Data;
using System.Threading.Tasks;

namespace SpamKiller.Modules
{
    public class InteractionBlacklistBase : InteractionModuleBase
    {
        #region Dependencies
        protected readonly ServerBotManager botManager;
        #endregion

        #region Constructors
        public InteractionBlacklistBase(ServerBotManager botManager)
        {
            this.botManager = botManager;
        }
        #endregion

        #region Check Functions
        protected async Task<ServerBot> checkIfServerRegistered()
        {
            // Get the server instance.
            if (!botManager.ServerInstancesByServerId.TryGetValue(Context.Guild.Id, out ServerBot serverBot))
            {
                await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "This server has not been registered.");
                return null;
            }

            return serverBot;
        }

        protected async Task<bool> checkIfWhitelisted() => await checkIfWhitelisted(await checkIfServerRegistered());

        protected async Task<bool> checkIfWhitelisted(ServerBot serverBot)
        {
            if (serverBot == null) return false;

            // Ensure the server is whitelisted.
            if (!serverBot.Settings.IsWhitelisted)
            {
                IUser liru = await Context.Client.GetUserAsync(Constants.LiruId);
                await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = $"This server has not been whitelisted to globally ban users, please contact {liru.Username}#{liru.Discriminator} if you believe this to be a mistake.");
                return false;
            }

            return true;
        }
        #endregion

        #region User Functions
        protected async Task<IUser> getUserFromString(string userIdOrName)
        {
            // Trim the string.
            userIdOrName = userIdOrName.Trim();

            // If the string contains a #, try find a user with the username/discriminator.
            int hashPosition = userIdOrName.IndexOf('#');
            if (hashPosition >= 0)
            {
                // Parse the name#tag.
                string username = userIdOrName[..hashPosition];
                string discriminator = userIdOrName[(hashPosition + 1)..];

                // Get the user.
                IUser spammer = await Context.Client.GetUserAsync(username, discriminator);
                
                // Ensure the user exists.☺
                if (spammer == null)
                {
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "User with that username/tag does not exist.\nIt is also possible that the name has invisible garbage letters that prevent them from being found, try their id instead.");
                    return null;
                }
                else return spammer;
            }
            // Otherwise; use it as an id.
            else
            {
                // Parse the id string. Discord doesn't recognise ulongs as integer types for some reason.
                if (!ulong.TryParse(userIdOrName, out var spammerIdLong))
                {
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "Invalid id, must be a 64 bit number or username/number pair (e.g. Liru#1022).");
                    return null;
                }

                // Get the user by their ID.
                IUser spammer = await Context.Client.GetUserAsync(spammerIdLong);

                // Ensure the user exists.
                if (spammer == null)
                {
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "User with that id does not exist.");
                    return null;
                }
                else return spammer;
            }
        }
        #endregion
    }
}
