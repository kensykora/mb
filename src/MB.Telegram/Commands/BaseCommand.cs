using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using MBUser = MB.Telegram.Models.MBUser;

namespace MB.Telegram.Commands
{
    public abstract class BaseCommand : IChatCommand
    {
        protected IConfiguration Config { get; }

        protected ISpotifyService SpotifyService { get; }

        protected ITelegramBotClient TelegramClient { get; }

        public abstract string[] ScopesRequired { get; }

        protected BaseCommand() { }

        protected BaseCommand(ITelegramBotClient client, ISpotifyService spotifyService, IConfiguration config)
        {
            TelegramClient = client;
            SpotifyService = spotifyService;
            Config = config;
        }

        public abstract string Command { get; }
        public abstract string Description { get; }
        public virtual bool Publish => true;

        public virtual bool RequiresSpotify => false;
        public virtual bool RequiresBotConnection => false;
        public virtual bool CanHandle(string message)
        {
            return message.StartsWith(Command, StringComparison.CurrentCultureIgnoreCase);
        }
        public async Task Process(MBUser user, Update update, ILogger logger, bool isAuthorizationCallback = false)
        {
            if (RequiresBotConnection && UserIsNotConnected(user))
            {
                logger.LogInformation("User {user} wasn't connected, starting connection process", user);
                await RequestTelegramAuthForUser(user, update, logger);
                return;
            }

            if (RequiresSpotify && UserIsMissingSpotify(user))
            {
                logger.LogInformation("User {user} was missing spotify authorization for command {command}", user, this);
                await RequestSpotifyAuthForUser(user, update, logger);

                return;
            }

            if (UserIsMissingScopes(user))
            {
                logger.LogInformation("User {user} was missing scopes {scopes} for command {command}", user, string.Join(" ", ScopesRequired), this);
                await ProcessMissingScopes(user, update, isAuthorizationCallback, logger);

                return;
            }

            await ProcessInternal(user, update, logger, isAuthorizationCallback);
        }

        private bool UserIsMissingSpotify(MBUser user)
        {
            return string.IsNullOrWhiteSpace(user.SpotifyId);
        }

        private async Task RequestSpotifyAuthForUser(MBUser user, Update update, ILogger logger)
        {
            if (update.Message.Chat.Type != ChatType.Private)
            {
                await TelegramClient.SendTextMessageAsync(
                    update.Message.Chat.Id,
                    $"Sure thing... Sending you a message shortly to get us connected to Spotify");
            }

            var state = new AuthorizationState()
            {
                Update = update,
                UserId = user.Id,
            };

            await TelegramClient.SendTextMessageAsync(
                user.ServiceId,
                $"OK, in order to continue, I need to connect to your Spotify so I can add you to the fun. Click the button below to get started.",
                replyMarkup: new InlineKeyboardMarkup(
                new InlineKeyboardButton()
                {
                    Text = "Connect Spotify Account",
                    Url = SpotifyService.GetAuthorizationUri(user, state, ScopesRequired).ToString()
                }));
        }

        private async Task RequestTelegramAuthForUser(MBUser user, Update update, ILogger logger)
        {
            logger.LogInformation("Having user {user} auth with Telegram (chat: {chatid})", user, update.Message.Chat.Id);
            var state = update.Base64Encode();
            await TelegramClient.SendTextMessageAsync(
                update.Message.Chat.Id,
                $"Sure thing... First, let's get connected.... in private 😉👅",
                replyMarkup: new InlineKeyboardMarkup(
                new InlineKeyboardButton()
                {
                    Text = "Connect Account",
                    LoginUrl = new LoginUrl()
                    {
                        RequestWriteAccess = true,
                        Url = $"{Config.GetValue<string>("baseUrl")}/telegram?state={state}"
                    }
                }));
            return;
        }

        private bool UserIsNotConnected(MBUser user)
        {
            return !user.ChatServiceAuthDate.HasValue;
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
                    new InlineKeyboardButton()
                    {
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