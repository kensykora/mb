using System;
using System.Threading.Tasks;
using MB.Telegram.Models;
using Microsoft.Extensions.Logging;

namespace MB.Telegram.Commands
{
    public abstract class BaseCommand : IChatCommand
    {
        public abstract string CommandString { get; }
        public virtual bool CanHandle(string message)
        {
            return message.StartsWith(CommandString, StringComparison.CurrentCultureIgnoreCase);
        }
        public abstract Task Process(User user, string message, ILogger logger);
    }
}