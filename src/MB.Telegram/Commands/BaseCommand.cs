using System;
using System.Threading.Tasks;
using MB.Telegram.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = MB.Telegram.Models.User;

namespace MB.Telegram.Commands
{
    public abstract class BaseCommand : IChatCommand
    {
        protected ITelegramBotClient Client { get; }

        protected BaseCommand() { }

        protected BaseCommand(ITelegramBotClient client)
        {
            Client = client;
        }

        public abstract string CommandString { get; }
        public virtual bool CanHandle(string message)
        {
            return message.StartsWith(CommandString, StringComparison.CurrentCultureIgnoreCase);
        }
        public abstract Task Process(User user, Update update, ILogger logger);
    }
}