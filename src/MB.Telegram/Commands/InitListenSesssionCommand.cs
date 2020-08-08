using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MB.Telegram.Commands
{
    public class InitListenSessionCommand : BaseListenSessionCommand
    {
        public override string Command => "/init";

        public override string Description => "Setup this group for a listening session";

        public override string[] ScopesRequired
        {
            get
            {
                var result = new List<string>(base.ScopesRequired);

                result.AddRange(new[] { Scopes.PlaylistReadCollaborative, Scopes.PlaylistModifyPrivate }); // So we can store the current playlist and manage it

                return result.ToArray();
            }
        }

        public InitListenSessionCommand()
        {

        }

        public InitListenSessionCommand(IListenSessionService listenSessionService, ITelegramBotClient telegramClient, ISpotifyService spotifyService, Config config)
            : base(listenSessionService, telegramClient, spotifyService, config)
        {
        }

        protected override async Task ProcessListenSessionCommandInternalAsync(MBUser user, Message message, ILogger logger, bool isAuthorizationCallback = false)
        {
            if (ListenGroup != null)
            {
                logger.LogInformation("Group {group} is already setup for listen session", ListenGroup);
                await TelegramClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "This group is already setup for listening.");
                return;
            }

            var spotifyClient = await SpotifyService.GetClientAsync(user);
            var playlist = await spotifyClient.Playlists.Create(user.SpotifyId, new PlaylistCreateRequest($"{message.Chat.Title} Playlist")
            {
                Description = $"Playlist managed by Music Bot. For Telegram group chat '{message.Chat.Title}'",
                Collaborative = true,
                Public = false
            });

            var group = new ListenGroup()
            {
                Id = ListenGroup.GetId(ChatServices.Telegram, message.Chat.Id.ToString()),
                ServiceId = message.Chat.Id.ToString(),
                OwnerMBDisplayName = user.DisplayName,
                OwnerMBUserId = user.Id,
                OwnerSpotifyUserId = user.SpotifyId,
                ActiveListenerIds = new string[] { },
                LastListened = DateTimeOffset.UtcNow,
                SpotifyPlaylistDisplayName = playlist.Name,
                SpotifyPlaylistId = playlist.Id,
                Created = DateTimeOffset.UtcNow
            };

            await ListenSessionService.CreateGroupAsync(group);

            await TelegramClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Setup Complete. " + playlist.ExternalUrls.FirstOrDefault().Value);

            return;
        }

        protected override Task ProcessInternalAsync(MBUser user, CallbackQuery callback, ILogger logger)
        {
            // Nothing to do here
            return Task.CompletedTask;
        }
    }
}