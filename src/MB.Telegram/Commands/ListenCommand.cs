using System.Threading.Tasks;
using MB.Telegram.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MB.Telegram.Commands
{
    public class ListenCommand : BaseCommand
    {
        public ListenCommand()
        {
        }

        public ListenCommand(ITelegramBotClient telegramClient, ISpotifyService spotifyService, Config config) : base(telegramClient, spotifyService, config)
        {
        }

        public override string Command => "/listen";
        public override string Description => "Join the listen session";

        public override ChatType[] SupportedChatTypes => new[] { ChatType.Group };

        public override string[] ScopesRequired => new[] { 
            Scopes.Streaming, // So we can manage their player and sync them with
        };

        public override bool RequiresBotConnection => true;

        public override bool RequiresSpotify => true;
        public override bool RequiresSpotifyPremium => true;

        protected override async Task ProcessInternal(Models.MBUser user, Message message, ILogger logger, bool isAuthorizationCallback = false)
        {
            await TelegramClient.SendTextMessageAsync(message.Chat.Id, "not implemented");

            return;
        }
    }
}