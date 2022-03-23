using Discord.Interactions;
using Discord.WebSocket;
using SpamKiller.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpamKiller.Bot
{
    /// <summary> Manages the bot's presence on each server it is part of. </summary>
    public class ServerBotManager
    {
        #region Dependencies
        private readonly DataContext context;
        private readonly DiscordSocketClient client;
        private readonly InteractionService interactionService;
        #endregion

        #region Backing Fields
        /// <inheritdoc cref="ServerBotManager.ServerInstancesByServerId"/>
        private readonly Dictionary<ulong, ServerBot> serverInstancesByServerId = new();
        #endregion

        #region Properties
        /// <summary> The number of total servers the bot is in. </summary>
        public int Count => serverInstancesByServerId.Count;

        /// <summary> The collection of server instances keyed by Discord server id. </summary>
        public IReadOnlyDictionary<ulong, ServerBot> ServerInstancesByServerId => serverInstancesByServerId;
        #endregion

        #region Constructors
        public ServerBotManager(DataContext context, DiscordSocketClient client, InteractionService interactionService)
        {
            // Set the dependencies.
            this.context = context;
            this.client = client;
            this.interactionService = interactionService;

            // Listen for join/leave events.
            client.JoinedGuild += onServerJoinedAsync;
            client.GuildAvailable += onServerAvailableAsync;
        }
        #endregion

        #region Collection Functions
        /// <summary> Tries to get the server instance associated with the given <paramref name="server"/> and creates one if none exists. </summary>
        /// <param name="server"> The Discord server. </param>
        /// <returns> The found or created <see cref="ServerBot"/>. </returns>
        private async Task<ServerBot> getOrCreateServerBotAsync(SocketGuild server)
        {
            // Do nothing if the server is already registered.
            if (serverInstancesByServerId.TryGetValue(server.Id, out ServerBot serverBot)) return serverBot;

            // Register the commands.
            await interactionService.RegisterCommandsToGuildAsync(server.Id);

            // Create the server bot.
            serverBot = await ServerBot.CreateAsync(context, client, server);

            // Register the bot.
            serverInstancesByServerId.Add(server.Id, serverBot);

            // Return the bot.
            return serverBot;
        }

        /// <summary> Is fired when the given <paramref name="server"/> becomes available to the bot. This means that the bot is already in the server. </summary>
        /// <param name="server"> The server that became available. </param>
        /// <returns> The status of the task. </returns>
        private async Task onServerAvailableAsync(SocketGuild server) => await getOrCreateServerBotAsync(server);

        /// <summary> Is fired when the given <paramref name="server"/> is joined by the bot via an invite or otherwise. </summary>
        /// <param name="server"> The server that was joined. </param>
        /// <returns> The status of the task. </returns>
        private async Task onServerJoinedAsync(SocketGuild server) => await getOrCreateServerBotAsync(server);
        #endregion
    }
}