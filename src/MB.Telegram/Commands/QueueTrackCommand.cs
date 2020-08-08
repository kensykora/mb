using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MB.Telegram.Commands
{
    public class QueueTrackCommand : BaseListenSessionCommand
    {
        public QueueTrackCommand()
        {
        }

        public QueueTrackCommand(IListenSessionService listenSessionService, ITelegramBotClient telegramClient, ISpotifyService spotifyService, Config config)
            : base(listenSessionService, telegramClient, spotifyService, config)
        {
        }

        private const string UrlQualifier = "https://open.spotify.com/";
        private const string UriQualifier = "spotify:";
        private const string TrackUriQualifier = "spotify:track:";
        public override string Command => "/queue";

        public override string Description => "[Spotify Url] Add a track to the end of this room's playlist queue";

        protected override async Task ProcessListenSessionCommandInternalAsync(MBUser user, Message message, ILogger logger, bool isAuthorizationCallback = false)
        {
            var splits = new List<string>(message.Text.Trim().Split(' '));
            splits.RemoveAll(x => string.IsNullOrWhiteSpace(x.Trim()));

            if (splits.Count == 1)
            {
                await TelegramClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "You didn't provide a song, dummy.");
                return;
            }

            var resource = splits[1];

            // TODO: Allow Playlist, Album
            // TODO: Artist??

            // URL: https://open.spotify.com/track/2wJDK6Epha7t2rewssvELD?si=iYD2K3e-Sd-RGQEAaDHWfg
            if (resource.StartsWith(UrlQualifier, StringComparison.OrdinalIgnoreCase))
            {
                await Queue(resource.Substring(UrlQualifier.Length, resource.IndexOf('?') - UrlQualifier.Length), '/', logger);
            }
            // URI: spotify:track:2wJDK6Epha7t2rewssvELD
            else if (resource.StartsWith(UriQualifier, StringComparison.OrdinalIgnoreCase))
            {
                await Queue(resource.Substring(UriQualifier.Length), ':', logger);
            }
            else
            {
                // TODO: Search???
                await TelegramClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "I'm not sure how to handle that");
                return;
            }
        }

        public async Task Queue(string path, char split, ILogger logger)
        {
            var splits = path.Split(split);

            if (splits.Length < 2)
            {
                await TelegramClient.SendTextMessageAsync(
                        this.ListenGroup.ServiceId,
                        "I'm not sure how to handle that.");
                return;
            }

            switch (splits[0].ToLower())
            {
                case "track": // spotify:track:2wJDK6Epha7t2rewssvELD
                    await OwnerSpotifyClient.Playlists.AddItems(CurrentGroupPlaylist.Id,
                    new PlaylistAddItemsRequest(
                        new[] { $"spotify:track:{splits[1]}" }
                    ));
                    await TelegramClient.SendTextMessageAsync(
                        this.ListenGroup.ServiceId,
                        "Added!");
                    // TODO: Tell them when it's coming up?
                    break;
                default:
                    // TODO: Handle unknown qualifier
                    logger.LogError("Unexpected qualifier {type}", splits[0]);
                    await TelegramClient.SendTextMessageAsync(
                        this.ListenGroup.ServiceId,
                        "I'm not sure how to handle that.");
                    break;
            }
        }

        protected override Task ProcessInternalAsync(MBUser user, CallbackQuery callback, ILogger logger)
        {
            // Nothing to do
            return Task.CompletedTask;
        }
    }
}