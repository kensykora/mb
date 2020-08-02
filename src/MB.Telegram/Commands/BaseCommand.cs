using System;
using System.Linq;
using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MBUser = MB.Telegram.Models.MBUser;

namespace MB.Telegram.Commands
{
    public abstract class BaseCommand : IChatCommand
    {
        protected ISpotifyService SpotifyService { get; }

        protected ITelegramBotClient TelegramClient { get; }

        public abstract string[] ScopesRequired { get; }

        protected BaseCommand() { }

        protected BaseCommand(ITelegramBotClient client, ISpotifyService spotifyService)
        {
            TelegramClient = client;
            SpotifyService = spotifyService;
        }

        public abstract string CommandString { get; }
        public virtual bool CanHandle(string message)
        {
            return message.StartsWith(CommandString, StringComparison.CurrentCultureIgnoreCase);
        }
        public async Task Process(MBUser user, Update update, ILogger logger, bool isAuthorizationCallback = false)
        {
            if (!ScopesRequired.All(scope => user.SpotifyScopes?.Contains(scope) ?? false))
            {
                if (isAuthorizationCallback)
                {
                    await TelegramClient.SendTextMessageAsync(
                        update.Message.Chat.Id,
                        $@"Sorry {user.ToTelegramUserLink()} but you need to grant additional permissions in order for us to run this command.",
                        ParseMode.Html);
                }
                else
                {
                    var state = new AuthorizationState()
                    {
                        Update = update,
                        UserId = user.Id
                    };

                    var message = string.IsNullOrWhiteSpace(user.SpotifyId)
                        ? $"Sure! lets get you connected first. Click this link and authorize me to manage your spotify player."
                        : $"Sorry {user.ToTelegramUserLink()} but we need additional permissions from you to do that. Please click this link and we'll get that sorted";

                    // TODO: Make this a button?
                    await TelegramClient.SendTextMessageAsync(
                        update.Message.Chat.Id,
                        message + "\n"
                        + "\n"
                        + SpotifyService.GetAuthorizationUri(user, state, ScopesRequired),
                        ParseMode.Html
                    );
                }
                return;
            }

            await ProcessInternal(user, update, logger, isAuthorizationCallback);
        }

        protected abstract Task ProcessInternal(MBUser user, Update update, ILogger logger, bool isAuthorizationCallback = false);
    }
}