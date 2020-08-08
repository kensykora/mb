using System;
using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MB.Telegram.Commands
{
    public class ListenCommand : BaseListenSessionCommand
    {
        private readonly IListenSessionService listenSessionService;

        public ListenCommand()
        {
        }

        public ListenCommand(IListenSessionService listenSessionService, ITelegramBotClient telegramClient, ISpotifyService spotifyService, Config config) : base(telegramClient, spotifyService, config)
        {
            this.listenSessionService = listenSessionService;
        }

        public override string Command => "/listen";
        public override string Description => "Join the listen session";

        protected override async Task ProcessInternal(Models.MBUser user, Message message, ILogger logger, bool isAuthorizationCallback = false)
        {
            // TODO : Check for init
            // Not implemented yet
        }

        protected override async Task ProcessInternal(MBUser user, CallbackQuery callback, ILogger logger)
        {
            // Nothing to respond to yet
        }
    }

    public class ListenCallback : BaseCallback
    {
        public const string ClaimOwnership = "c";
        public ListenCallback(IChatCommand command) : base(command)
        {
        }
    }
}