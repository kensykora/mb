using System.Threading.Tasks;
using MB.Telegram.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = MB.Telegram.Models.User;

namespace MB.Telegram.Commands
{
    public class UnknownCommand : BaseCommand
    {
        public UnknownCommand() { }
        public UnknownCommand(ITelegramBotClient client) : base(client) { }
        public override string CommandString => null;

        public override bool CanHandle(string message)
        {
            return true;
        }

        public override async Task Process(User user, Update update, ILogger logger)
        {
            await Client.SendTextMessageAsync(update.Message.Chat.Id, $"I'm not sure how to deal with this: '{update.Message.Text}'");
        }
    }
}