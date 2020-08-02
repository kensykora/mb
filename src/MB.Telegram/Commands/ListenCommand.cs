using System.Threading.Tasks;
using MB.Telegram.Services;
using Microsoft.Extensions.Configuration;
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
        public ListenCommand(ITelegramBotClient telegramClient, ISpotifyService spotifyService, IConfiguration config) : base(telegramClient, spotifyService, config)
        {
        }

        public override string Command => "/listen";
        public override string Description => "Join the listen session";

        public override string[] ScopesRequired => new[] { Scopes.Streaming };

        public override bool RequiresBotConnection => true;

        public override bool RequiresSpotify => true;

        protected override async Task ProcessInternal(Models.MBUser user, Message message, ILogger logger, bool isAuthorizationCallback = false)
        {
            await TelegramClient.SendTextMessageAsync(message.Chat.Id, "not done");
        }
    }
}