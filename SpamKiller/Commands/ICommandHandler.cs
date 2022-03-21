using Discord;
using System.Collections.Generic;

namespace SpamKiller.Commands
{
    public interface ICommandHandler
    {
        IReadOnlyList<SlashCommandProperties> CommandProperties { get; }

        void RegisterCommands(CommandManager commandManager);
    }
}