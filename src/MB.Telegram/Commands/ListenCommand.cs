using System.Threading.Tasks;
using MB.Telegram.Services;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MB.Telegram.Commands
{
    public class ListenCommand : BaseCommand
    {
        public ListenCommand()
        {
            
        }
        public ListenCommand(ITelegramBotClient telegramClient, ISpotifyService spotifyService) : base(telegramClient, spotifyService)
        {
        }

        public override string CommandString => "/listen";

        public override string[] ScopesRequired => new[] { Scopes.Streaming };

        protected override async Task ProcessInternal(Models.User user, Update update, ILogger logger)
        {
            await TelegramClient.SendTextMessageAsync(update.Message.Chat.Id, "not done");
        }
    }
}