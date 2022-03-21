using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SpamKiller.Bot;
using SpamKiller.Data;
using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SpamKiller
{
    public class MainBot
    {
        #region Constants
        private const float statusUpdateInvervalMinutes = 1f;
        #endregion

        #region Fields
        /// <summary> The bot's token. </summary>
        private readonly string token;

        /// <summary> The services. </summary>
        private IServiceProvider serviceProvider;

        /// <summary> The timer used to switch the status. Note that even though this is never referred to, it is needed as a field to prevent it going out of scope. </summary>
        private readonly Timer statusTimer;

        private int statusTicker = 0;
        #endregion

        #region Properties
        /// <summary> The service used to handle commands. </summary>
        public CommandService CommandService { get; } = new CommandService();

        /// <summary> The service that handles routing slash commands. </summary>
        public InteractionService InteractionService { get; }

        /// <summary> The connection to the Discord servers. </summary>
        public DiscordSocketClient Client { get; }

        public DataContext Context { get; }

        public ServerBotManager ServerBotManager { get; }

        public BlacklistManager BlacklistManager { get; }
        #endregion

        #region Constructors
        public MainBot(string token)
        {
            // Set the token.
            this.token = token;

            // Create the client and context.
            Client = new DiscordSocketClient();
            Context = new DataContext();

            // Create any services that rely on the client.
            InteractionService = new InteractionService(Client);

            // Create the data managers.
            ServerBotManager = new ServerBotManager(Context, Client, InteractionService);
            BlacklistManager = new BlacklistManager(ServerBotManager, Context);

            // Bind the log function.
            Client.Log += Log;

            // Start the timer for the status.
            statusTimer = new ((state) => updateStatus().Wait(), null, 0, (int)MathF.Round(statusUpdateInvervalMinutes * 60000));

            // Log the start.
            LogConsole($"DeFi Shield version {ConfigurationManager.AppSettings["version"]} started.");
        }
        #endregion

        #region Initialisation Functions
        public async Task LoginAndStartAsync()
        {
            // Initialise the commands.
            await initialiseCommandsAsync();

            // Login and start.
            await LoginAsync();
            await StartAsync();
        }

        public async Task LoginAsync() => await Client.LoginAsync(TokenType.Bot, token);

        public async Task StartAsync() => await Client.StartAsync();

        public async Task LogoutAndStopAsync()
        {
            await Client.LogoutAsync();
            await Client.StopAsync();
        }

        private async Task initialiseCommandsAsync()
        {
            // Create the services.
            serviceProvider = new ServiceCollection()
                .AddSingleton(BlacklistManager)
                .AddSingleton(ServerBotManager)
                .AddSingleton(Context)
                .AddSingleton(CommandService)
                .AddSingleton(InteractionService)
                .BuildServiceProvider();

            // Add the commands.
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
            await InteractionService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);

            // Handle receiving messages.
            Client.MessageReceived += handleCommandAsync;
            Client.SlashCommandExecuted += async (command) => await InteractionService.ExecuteCommandAsync(new InteractionContext(Client, command, command.Channel), serviceProvider);
            Client.UserCommandExecuted += async (command) => await InteractionService.ExecuteCommandAsync(new InteractionContext(Client, command, command.Channel), serviceProvider);
        }
        #endregion

        #region Status Functions
        private async Task updateStatus()
        {
            switch (statusTicker)
            {
                case 0:
                    int count = await Context.BannedUsers.CountAsync();
                    await Client.SetGameAsync($"Banned {count} users", null, ActivityType.Watching);
                    break;
                case 1:
                    await Client.SetGameAsync($"Protecting {ServerBotManager.ServerInstancesByServerId.Count} servers", null, ActivityType.Watching);
                    break;
            }

            statusTicker++;
            if (statusTicker > 1) statusTicker = 0;
        }
        #endregion

        #region Log Functions
        public Task Log(LogMessage message) => LogConsole(message.ToString());

        public Task LogConsole(string message)
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        }
        #endregion

        #region Command Functions
        private async Task handleCommandAsync(SocketMessage message)
        {
            // Ensure it's a user message.
            if (message is not SocketUserMessage userMessage) return;

            // Don't respond to bots.
            if (userMessage.Author.IsBot) return;

            // Handle finding if the command is valid.
            int messageCharPos = 0;
            if (userMessage.HasCharPrefix('/', ref messageCharPos))
            {
                // Create the context for the command.
                SocketCommandContext context = new(Client, userMessage);

                // Execute the command.
                await CommandService.ExecuteAsync(context, messageCharPos, serviceProvider);
            }
        }

        private async Task handleSlashCommandAsync(SocketSlashCommand command)
        {
            ;
        }
        #endregion
    }
}