using System;
using System.Linq;
using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
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
            if (UserIsMissingScopes(user))
            {
                logger.LogInformation("User {user} was missing scopes {scopes} for command {command]", user, string.Join(" ", ScopesRequired), this);
                await ProcessMissingScopes(user, update, isAuthorizationCallback, logger);

                return;
            }

            await ProcessInternal(user, update, logger, isAuthorizationCallback);
        }

        private async Task ProcessMissingScopes(MBUser user, Update update, bool isAuthorizationCallback, ILogger log)
        {
            if (isAuthorizationCallback)
            {
                // User already setup, this is an auth callback.. they must have denied scopes?
                // TODO: Send through error state from auth call

                log.LogWarning("User {user} denied scopes {scopes} for commmand {command}", user, string.Join(" ", ScopesRequired), this);

                await TelegramClient.SendTextMessageAsync(
                    update.Message.Chat.Id,
                    $@"Sorry {user.ToTelegramUserLink()} but you need to grant additional permissions in order for us to run this command.",
                    ParseMode.Html);
                return;
            }

            // Request additional scopes
            var state = new AuthorizationState()
            {
                Update = update,
                UserId = user.Id
            };

            var message = string.IsNullOrWhiteSpace(user.SpotifyId)
                ? $"Sure! lets get you connected first. Click this link and authorize me to manage your spotify player."
                : $"Sorry {user.ToTelegramUserLink()} but we need additional permissions from you to do that. Please click this link and we'll get that sorted";

            await TelegramClient.SendTextMessageAsync(
                update.Message.Chat.Id,
                message,
                ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(
                    new InlineKeyboardButton() { 
                        Text = "Connect Account", 
                        Url = SpotifyService.GetAuthorizationUri(user, state, ScopesRequired).ToString() 
                    })
            );
        }

        private bool UserIsMissingScopes(MBUser user)
        {
            return !ScopesRequired.All(scope => user.SpotifyScopes?.Contains(scope) ?? false);
        }

        protected abstract Task ProcessInternal(MBUser user, Update update, ILogger logger, bool isAuthorizationCallback = false);

        public override string ToString()
        {
            return this.GetType().Name;
        }
    }
}