using System;
using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

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

        protected override async Task ProcessInternalAsync(Models.MBUser user, Message message, ILogger logger, bool isAuthorizationCallback = false)
        {
            var listenSession = await listenSessionService.GetGroupAsync(ChatServices.Telegram, message.Chat.Id.ToString());

            if (listenSession == null)
            {
                await TelegramClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "This group isn't setup for listening. Run /init first.");
            }
        }

        protected override async Task ProcessInternalAsync(MBUser user, CallbackQuery callback, ILogger logger)
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