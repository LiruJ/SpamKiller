using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SpamKiller.Blacklist;
using SpamKiller.Bot;
using SpamKiller.Data;
using SpamKiller.Reporting;
using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SpamKiller
{
    /// <summary> The main bot class, handling the entire bot's presence in Discord. </summary>
    public class MainBot
    {
        #region Constants
        /// <summary> How long (in minutes) it takes for the bot's status to tick. </summary>
        private const float statusUpdateInvervalMinutes = 1f;
        #endregion

        #region Fields
        /// <summary> The bot's token. </summary>
        private readonly string token;

        /// <summary> The services. </summary>
        private IServiceProvider serviceProvider;

        /// <summary> The timer used to switch the status. Note that even though this is never referred to, it is needed as a field to prevent it going out of scope. </summary>
#pragma warning disable IDE0052 // Remove unread private members
        private readonly Timer statusTimer;
#pragma warning restore IDE0052 // Remove unread private members

        /// <summary> The value used to determine what status to show next. </summary>
        private int statusTicker = 0;
        #endregion

        #region Properties
        /// <summary> The service used to handle commands. </summary>
        public CommandService CommandService { get; }

        /// <summary> The service that handles routing slash commands and other interactions. </summary>
        public InteractionService InteractionService { get; }

        /// <summary> The connection to the Discord servers. </summary>
        public DiscordSocketClient Client { get; }

        /// <summary> The connection to the local database. </summary>
        public DataContext Context { get; }

        /// <summary> The manager that handles the instances of this bot on every server. </summary>
        public ServerBotManager ServerBotManager { get; }

        /// <summary> The manager that handles scam reporters. </summary>
        public ReporterManager ReporterManager { get; }

        /// <summary> The manager that handles banning and unbanning users. </summary>
        public BlacklistManager BlacklistManager { get; }
        #endregion

        #region Constructors
        public MainBot(string token)
        {
            // Don't even start the bot if the token is invalid.
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException($"'{nameof(token)}' cannot be null or whitespace.", nameof(token));

            // Set the token.
            this.token = token;

            // Create the client and context.
            Client = new DiscordSocketClient();
            Context = new DataContext();

            // Create any services that rely on the client.
            CommandService = new CommandService();
            InteractionService = new InteractionService(Client);

            // Create the data managers.
            ServerBotManager = new ServerBotManager(Context, Client, InteractionService);
            ReporterManager = new ReporterManager(Context);
            BlacklistManager = new BlacklistManager(ServerBotManager, ReporterManager, Context);

            // Bind the log function.
            Client.Log += Log;

            // Start the timer for the status.
            statusTimer = new((state) => updateStatus().Wait(), null, 0, (int)MathF.Round(statusUpdateInvervalMinutes * 60000));

            // Log the start.
            LogConsole($"DeFi Shield version {ConfigurationManager.AppSettings["version"]} started.");
        }
        #endregion

        #region Initialisation Functions
        /// <summary> Initialses commands, logs the bot in, and starts it. </summary>
        /// <returns> <c>true</c> if the initialisation was a success; otherwise <c>false</c>. </returns>
        public async Task<bool> LoginAndStartAsync()
        {
            // Initialise the commands.
            if (!await initialiseCommandsAsync()) return false;

            // Login and start.
            if (!await LoginAsync()) return false;
            if (!await StartAsync()) return false;

            // Since everything went okay, return true.
            return true;
        }

        /// <summary> Logs the bot in. </summary>
        /// <returns> <c>true</c> if the login was a success; otherwise <c>false</c>. </returns>
        public async Task<bool> LoginAsync()
        {
            // Try to log in. If it fails, log why and return false; otherwise return true.
            try { await Client.LoginAsync(TokenType.Bot, token); }
            catch (Exception e) { await LogConsole(e.ToString()); return false; }
            return true;
        }

        /// <summary> Starts the bot. </summary>
        /// <returns> <c>true</c> if the start was a success; otherwise <c>false</c>. </returns>
        public async Task<bool> StartAsync()
        {
            // Try to start. If it fails, log why and return false; otherwise return true.
            try { await Client.StartAsync(); }
            catch (Exception e) { await LogConsole(e.ToString()); return false; }
            return true;
        }

        /// <summary> Logs out and stops the bot. </summary>
        /// <returns> The status of the task. </returns>
        public async Task LogoutAndStopAsync()
        {
            await Client.LogoutAsync();
            await Client.StopAsync();
        }

        /// <summary> Initialises all modules and commands internally. </summary>
        /// <returns> <c>true</c> if initialisation was a success; otherwise <c>false</c>. </returns>
        private async Task<bool> initialiseCommandsAsync()
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
            try
            {
                await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
                await InteractionService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
            }
            catch (Exception e) { await LogConsole(e.ToString()); return false; }

            // Handle receiving messages.
            Client.MessageReceived += handleCommandAsync;

            // Handle commands and interactions.
            Client.SlashCommandExecuted += executeInteractionAsync;
            Client.ButtonExecuted += executeInteractionAsync;
            Client.MessageCommandExecuted += executeInteractionAsync;
            Client.SelectMenuExecuted += executeInteractionAsync;
            Client.UserCommandExecuted += executeInteractionAsync;

            // The commands were initialised successfully, so return true.
            return true;
        }
        #endregion

        #region Status Functions
        /// <summary> Updates the bot's status in Discord. </summary>
        /// <returns> The status of the task. </returns>
        private async Task updateStatus()
        {
            // Handle the value of the ticker so far.
            switch (statusTicker)
            {
                // Show the number of banned users.
                case 0:
                    int bannedUserCount = await Context.BannedUsers.CountAsync();
                    await Client.SetGameAsync($"Banned {bannedUserCount} users", null, ActivityType.Watching);
                    break;

                // Show the number of whitelisted servers.
                case 1:
                    int whitelistedServerCount = await Context.ServerSettings.CountAsync(x => x.IsWhitelisted);
                    await Client.SetGameAsync($"Protected by {whitelistedServerCount} servers", null, ActivityType.Watching);
                    break;

                // Show the number of total servers.
                case 2:
                    await Client.SetGameAsync($"Protecting {ServerBotManager.Count} total servers", null, ActivityType.Watching);
                    break;
            }

            // Increment the ticker, and reset it if it's too big.
            statusTicker++;
            if (statusTicker > 2) statusTicker = 0;
        }
        #endregion

        #region Log Functions
        /// <summary> Logs the given <paramref name="message"/> in the console. </summary>
        /// <param name="message"> The message to log. </param>
        /// <returns> The status of the task. </returns>
        public static Task Log(LogMessage message) => LogConsole(message.ToString());

        /// <summary> Logs the given <paramref name="message"/> in the console. </summary>
        /// <param name="message"> The message to log. </param>
        /// <returns> The status of the task. </returns>
        public static Task LogConsole(string message)
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        }
        #endregion

        #region Command Functions
        /// <summary> Handles receiving standard commands. </summary>
        /// <param name="message"> The sent message. </param>
        /// <returns> The status of the task. </returns>
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

        /// <summary> Executes the given <paramref name="interaction"/>. </summary>
        /// <param name="interaction"> The interaction to execute. </param>
        /// <returns> The status of the task. </returns>
        private async Task executeInteractionAsync(SocketInteraction interaction)
            => await InteractionService.ExecuteCommandAsync(new InteractionContext(Client, interaction, interaction.Channel), serviceProvider);
        #endregion
    }
}