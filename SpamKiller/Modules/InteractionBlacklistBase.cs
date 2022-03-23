using Discord;
using Discord.Interactions;
using SpamKiller.Bot;
using SpamKiller.Data;
using System.Threading.Tasks;

namespace SpamKiller.Modules
{
    /// <summary> Allows easy checking of server and user info. </summary>
    public class InteractionBlacklistBase : InteractionModuleBase
    {
        #region Dependencies
        protected readonly ServerBotManager botManager;
        #endregion

        #region Constructors
        public InteractionBlacklistBase(ServerBotManager botManager) => this.botManager = botManager;
        #endregion

        #region Check Functions
        /// <summary> 
        /// Gets the server instance for the current server and checks to see if it exists. 
        /// Modifies the original response if the server is not registered.
        /// </summary>
        /// <returns> The server instance if the server is registered; otherwise <c>null</c>. </returns>
        protected async Task<ServerBot> CheckIfServerRegisteredAsync()
        {
            // Get the server instance. If it does not exist then modify the original response.
            if (!botManager.ServerInstancesByServerId.TryGetValue(Context.Guild.Id, out ServerBot serverBot))
                await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "This server has not been registered.");

            // Return the instance. This will be null if it does not exist.
            return serverBot;
        }

        /// <summary> 
        /// Gets the server instance for the current server via <see cref="CheckIfServerRegisteredAsync"/> and checks to see if it is whitelisted. 
        /// Modifies the original response if the server is not whitelisted or registered.
        /// </summary>
        /// <returns> <c>true</c> if the server is registered and whitelisted; otherwise <c>false</c>. </returns>
        protected async Task<bool> CheckIfWhitelistedAsync() => await CheckIfWhitelistedAsync(await CheckIfServerRegisteredAsync());

        /// <summary> 
        /// Checks to see if the given <paramref name="serverBot"/> is registered.
        /// Modifies the original response if the server is not whitelisted or registered.
        /// </summary>
        /// <param name="serverBot"> The server instance to check. </param>
        /// <returns> <c>true</c> if the server is registered and whitelisted; otherwise <c>false</c>. </returns>
        protected async Task<bool> CheckIfWhitelistedAsync(ServerBot serverBot)
        {
            // If the server instance is null, return false.
            if (serverBot == null) return false;

            // Ensure the server is whitelisted.
            if (!serverBot.Settings.IsWhitelisted)
            {
                // Get Liru and tell the user to contact her about being whitelisted.
                IUser liru = await Context.Client.GetUserAsync(Constants.LiruId);
                await ModifyOriginalResponseAsync((messageProperties) 
                    => messageProperties.Content = $"This server has not been whitelisted to globally ban users, please contact {liru.Username}#{liru.Discriminator} if you believe this to be a mistake.");
            }

            // Return the server's whitelist status.
            return serverBot.Settings.IsWhitelisted;
        }
        #endregion

        #region User Functions
        /// <summary> 
        /// Gets the user with the given <paramref name="userIdOrName"/>.
        /// Modifies the original response if the user could not be found.
        /// </summary>
        /// <param name="userIdOrName"> The Discord id or username#tag. </param>
        /// <returns> The found user, or <c>null</c> if none exists. </returns>
        protected async Task<IUser> GetUserFromStringAsync(string userIdOrName)
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
                IUser user = await Context.Client.GetUserAsync(username, discriminator);

                // If the user does not exist, modify the original response to say that.
                if (user == null)
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "User with that username/tag does not exist.\nIt is also possible that the name has invisible garbage letters that prevent them from being found, try their id instead.");

                // Return the user.
                return user;
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
                IUser user = await Context.Client.GetUserAsync(spammerIdLong);

                // If the user does not exist, modify the original response to say that.
                if (user == null)
                    await ModifyOriginalResponseAsync((messageProperties) => messageProperties.Content = "User with that id does not exist.");
                
                // Return the user.
                return user;
            }
        }
        #endregion
    }
}