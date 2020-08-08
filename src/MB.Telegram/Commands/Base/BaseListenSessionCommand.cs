using System;
using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MB.Telegram.Commands
{
    public abstract class BaseListenSessionCommand : BaseCommand
    {
        protected ListenGroup ListenGroup { get; private set; }
        protected IListenSessionService ListenSessionService { get; }
        protected ISpotifyClient OwnerSpotifyClient { get; private set; }
        protected ISpotifyClient MessageSenderSpotifyClient { get; private set; }
        protected FullPlaylist CurrentGroupPlaylist { get; private set; }
        protected BaseListenSessionCommand()
        {

        }

        protected BaseListenSessionCommand(IListenSessionService listenSessionService, ITelegramBotClient client, ISpotifyService spotifyService, Config config)
            : base(client, spotifyService, config)
        {
            ListenSessionService = listenSessionService;
        }
        public override ChatType[] SupportedChatTypes => new[] { ChatType.Group };

        public override string[] ScopesRequired => new[] {
            Scopes.Streaming, // So we can manage their player and sync them with
        };

        public override bool RequiresBotConnection => true;

        public override bool RequiresSpotify => true;

        public override bool RequiresSpotifyPremium => true;

        protected virtual bool RequiresActiveGroup => true;

        protected override async Task ProcessInternalAsync(MBUser user, Message message, ILogger logger, bool isAuthorizationCallback = false)
        {
            ListenGroup = await ListenSessionService.GetGroupAsync(ChatServices.Telegram, message.Chat.Id.ToString());

            if (ListenGroup != null)
            {
                OwnerSpotifyClient = await SpotifyService.GetClientAsync(ListenGroup.OwnerMBUserId);
                MessageSenderSpotifyClient = await SpotifyService.GetClientAsync(user);

                try
                {
                    CurrentGroupPlaylist = await OwnerSpotifyClient.Playlists.Get(ListenGroup.SpotifyPlaylistId);
                    if (CurrentGroupPlaylist == null)
                    {
                        // TODO: handle User deleted playlist
                        logger.LogError("Playlist wasn't found.. must have been deleted");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving playlist");
                }
            }
            else if (RequiresActiveGroup)
            {
                logger.LogInformation("Requires active group to run this command.");
                await TelegramClient.SendTextMessageAsync(
                   message.Chat.Id,
                   "Requires an active group to do this. Run /init");
                return;
            }

            await ProcessListenSessionCommandInternalAsync(user, message, logger, isAuthorizationCallback);
        }

        protected abstract Task ProcessListenSessionCommandInternalAsync(MBUser user, Message message, ILogger logger, bool isAuthorizationCallback = false);
    }
}