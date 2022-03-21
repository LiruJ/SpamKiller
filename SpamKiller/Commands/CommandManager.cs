using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpamKiller.Commands
{
    public class CommandManager
    {
        #region Constants
        public const char componentIdSeparator = '?';
        #endregion

        #region Delegates
        public delegate Task OnComponent(SocketMessageComponent component, params string[] arguments);
        #endregion

        #region Fields
        private readonly Dictionary<string, Func<SocketSlashCommand, Task>> commandListenersByWord = new();

        private readonly Dictionary<string, OnComponent> componentListenersByWord = new();

        private readonly List<ICommandHandler> handlers = new();
        #endregion

        #region Constructors
        public CommandManager(DiscordSocketClient client)
        {
            client.JoinedGuild += onServerJoined;
            client.GuildAvailable += onServerJoined;
            client.SlashCommandExecuted += onSlashCommand;
            client.ButtonExecuted += onComponentInteraction;
        }
        #endregion

        #region Registration Functions
        public CommandManager RegisterHandler(ICommandHandler handler)
        {
            // Add the handler to the list.
            handlers.Add(handler);

            // Register the handler.
            handler.RegisterCommands(this);

            // Return this, for fluent styled chains.
            return this;
        }
        #endregion

        #region Connection Functions
        public void AddCommandWord(string commandWord, Func<SocketSlashCommand, Task> listener) => commandListenersByWord.Add(commandWord, listener);

        public void AddComponentWord(string componentWord, OnComponent listener) => componentListenersByWord.Add(componentWord, listener);

        private async Task onServerJoined(SocketGuild server)
        {
            // Create a new list of registration tasks.
            List<Task> registrationTasks = new();
            
            // Add each command to the list.
            foreach (ICommandHandler handler in handlers)
                registrationTasks.AddRange(handler.CommandProperties.Select(x => server.CreateApplicationCommandAsync(x)));
            
            // Wait for each task to be handled.
            await Task.WhenAll(registrationTasks);
        }
        #endregion

        #region Command Functions
        private async Task onSlashCommand(SocketSlashCommand command)
        {
            // Try get the command associated with the name. Do nothing if there is no listener.
            if (!commandListenersByWord.TryGetValue(command.CommandName, out var listener))
                return;
            
            // Call the listener.
            await listener(command);
        }

        private async Task onComponentInteraction(SocketMessageComponent component)
        {
            // Handle splitting the id into its name and arguments.
            string[] componentValues = component.Data.CustomId.Split(componentIdSeparator);
            string componentName = componentValues[0];

            // Try get the component associated with the name. Do nothing if there is no listener.
            if (!componentListenersByWord.TryGetValue(componentName, out var listener))
                return;

            // Call the listener.
            await listener(component, componentValues[1..]);
        }
        #endregion
    }
}
