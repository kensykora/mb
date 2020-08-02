using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using MBUser = MB.Telegram.Models.MBUser;

namespace MB.Telegram.Commands
{
    public class UnknownCommand : BaseCommand
    {
        public UnknownCommand() { }
        public UnknownCommand(ITelegramBotClient client, ISpotifyService spotifyService, IConfiguration config) : base(client, spotifyService, config) { }
        public override string Command => null;
        public override string Description => null;
        public override bool Publish => false;

        public override string[] ScopesRequired => new string[] { };

        public override bool CanHandle(string message)
        {
            return true;
        }

        protected override async Task ProcessInternal(MBUser user, Message message, ILogger logger, bool isAuthorizationCallback = false)
        {
            await TelegramClient.SendTextMessageAsync(message.Chat.Id, $"I'm not sure how to deal with this: '{message.Text}'");
        }
    }
}