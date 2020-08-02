using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = MB.Telegram.Models.User;

namespace MB.Telegram.Commands
{
    public class UnknownCommand : BaseCommand
    {
        public UnknownCommand() { }
        public UnknownCommand(ITelegramBotClient client, ISpotifyService spotifyService) : base(client, spotifyService) { }
        public override string CommandString => null;

        public override string[] ScopesRequired => new string[] { };

        public override bool CanHandle(string message)
        {
            return true;
        }

        protected override async Task ProcessInternal(User user, Update update, ILogger logger)
        {
            await TelegramClient.SendTextMessageAsync(update.Message.Chat.Id, $"I'm not sure how to deal with this: '{update.Message.Text}'");
        }
    }
}