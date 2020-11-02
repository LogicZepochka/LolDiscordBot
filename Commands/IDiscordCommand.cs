using DSharpPlus.Entities;
using LoLDiscover.Commands.Classes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LoLDiscover.Commands
{
    public interface IDiscordCommand
    {
        string Description { get; }
        string UsageExample { get; }
        CommandDelegate Executor { get; }
    }
}
