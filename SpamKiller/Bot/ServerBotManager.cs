using Discord.Interactions;
using Discord.WebSocket;
using SpamKiller.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpamKiller.Bot
{
    public class ServerBotManager
    {
        #region Dependencies
        private readonly DataContext context;
        private readonly DiscordSocketClient client;
        private readonly InteractionService interactionService;
        #endregion

        #region Backing Fields
        private readonly Dictionary<ulong, ServerBot> serverInstancesByServerId = new();
        #endregion

        #region Properties
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
            client.JoinedGuild += onServerJoined;
            client.GuildAvailable += onServerAvailable;
        }
        #endregion

        #region Collection Functions
        private async Task<ServerBot> getOrCreateServerBot(SocketGuild server)
        {
            // Do nothing if the server is already registered.
            if (serverInstancesByServerId.TryGetValue(server.Id, out ServerBot serverBot)) return serverBot;

            // Register the commands.
            await interactionService.RegisterCommandsToGuildAsync(server.Id);

            // Create the server bot.
            serverBot = await ServerBot.Create(context, client, server);

            // Register the bot.
            serverInstancesByServerId.Add(server.Id, serverBot);

            // Return the bot.
            return serverBot;
        }

        private async Task onServerAvailable(SocketGuild server) => await getOrCreateServerBot(server);

        private async Task onServerJoined(SocketGuild server) => await getOrCreateServerBot(server);
        #endregion
    }
}