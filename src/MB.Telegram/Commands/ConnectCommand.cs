using System.Threading.Tasks;
using MB.Telegram.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = MB.Telegram.Models.User;

namespace MB.Telegram.Commands
{
    public class ConnectCommand : BaseCommand
    {
        public override string CommandString => "/connect";

        public ConnectCommand() { }
        public ConnectCommand(ITelegramBotClient client) : base(client) { }

        public override async Task Process(User user, Update update, ILogger logger)
        {
            return;
        }
    }
}