using MB.Telegram.Services;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MB.Telegram.Commands
{
    public abstract class BaseListenSessionCommand : BaseCommand
    {
        protected BaseListenSessionCommand()
        {

        }
        
        protected BaseListenSessionCommand(ITelegramBotClient client, ISpotifyService spotifyService, Config config)
            : base(client, spotifyService, config)
        {
        }
        public override ChatType[] SupportedChatTypes => new[] { ChatType.Group };

        public override string[] ScopesRequired => new[] {
            Scopes.Streaming, // So we can manage their player and sync them with
        };

        public override bool RequiresBotConnection => true;

        public override bool RequiresSpotify => true;

        public override bool RequiresSpotifyPremium => true;
    }
}