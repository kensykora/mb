using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using MBUser = MB.Telegram.Models.MBUser;

namespace MB.Telegram.Commands
{
    public abstract class BaseCommand : IChatCommand
    {
        protected Config Config { get; }

        protected ISpotifyService SpotifyService { get; }

        protected ITelegramBotClient TelegramClient { get; }

        public abstract string[] ScopesRequired { get; }

        public virtual ChatType[] SupportedChatTypes => new[] { ChatType.Channel, ChatType.Group, ChatType.Private, ChatType.Supergroup };

        protected BaseCommand() { }

        protected BaseCommand(ITelegramBotClient client, ISpotifyService spotifyService, Config config)
        {
            TelegramClient = client;
            SpotifyService = spotifyService;
            Config = config;
        }

        public abstract string Command { get; }
        public abstract string Description { get; }
        public virtual bool Publish => true;
        public virtual bool RequiresSpotifyPremium => false;

        public virtual bool RequiresSpotify => false;
        public virtual bool RequiresBotConnection => false;

        public virtual bool CanHandle(UnknownCallback callback)
        {
            return callback.CommandType == this.GetType().Name;
        }

        public virtual bool CanHandle(string message)
        {
            return message.StartsWith(Command, StringComparison.CurrentCultureIgnoreCase);
        }

        public async Task Process(MBUser user, CallbackQuery callback, ILogger logger)
        {
            if (await TryHandleRequiresBotConnection(user, logger, callback.Message)) { return; }
            if (await TryHandleRequiresSpotifyConnection(user, logger, callback.Message)) { return; }

            await ProcessInternalAsync(user, callback, logger);
        }

        public async Task Process(MBUser user, Message message, ILogger logger, bool isAuthorizationCallback = false)
        {
            if (!SupportedChatTypes.Contains(message.Chat.Type))
            {
                logger.LogDebug("Command {command} does not support chat type {type}", this, message.Chat.Type);
                await TelegramClient.SendTextMessageAsync(
                    message.Chat.Id,
                    $"Sorry, that command isn't supported in this type of chat. That command can only be used in {string.Join(", ", SupportedChatTypes.Select(x => $"{x}s".ToLower()))}");
                return;
            }

            if (await TryHandleRequiresBotConnection(user, logger, message)) { return; }
            if (await TryHandleRequiresSpotifyConnection(user, logger, message, isAuthorizationCallback)) { return; }

            await ProcessInternalAsync(user, message, logger, isAuthorizationCallback);
        }

        private async Task<bool> TryHandleRequiresBotConnection(MBUser user, ILogger logger, Message message)
        {
            if (RequiresBotConnection && UserIsNotConnected(user))
            {
                logger.LogInformation("User {user} wasn't connected, starting connection process", user);
                await RequestTelegramAuthForUser(user, message, logger);
                return true;
            }

            return false;
        }

        private async Task<bool> TryHandleRequiresSpotifyConnection(MBUser user, ILogger logger, Message message, bool isAuthorizationCallback = false)
        {
            if (RequiresSpotify && UserIsMissingSpotify(user))
            {
                logger.LogInformation("User {user} was missing spotify authorization for command {command}", user, this);
                await RequestSpotifyAuthForUser(user, message, logger);

                return true;
            }

            if (RequiresSpotify && UserIsMissingScopes(user))
            {
                logger.LogInformation("User {user} was missing scopes {scopes} for command {command}", user, string.Join(" ", ScopesRequired), this);
                await ProcessMissingScopes(user, message, isAuthorizationCallback, logger);

                return true;
            }

            if (RequiresSpotify && RequiresSpotifyPremium)
            {
                var client = await SpotifyService.GetClientAsync(user);
                try
                {
                    var profile = await client.UserProfile.Current();
                    if (profile.Product != "premium")
                    {
                        logger.LogInformation("User {user} tried to use command requiring spotify premium, but found {product}", user, profile.Product);
                        await TelegramClient.SendTextMessageAsync(
                            message.Chat.Id,
                            $"Sorry {user.ToTelegramUserLink()} but you need spotify premium to use this command. Consider upgrading your account and try again.",
                            ParseMode.Html);
                        return true;
                    }
                }
                catch (SpotifyAPI.Web.APIException ex)
                {
                    if (ex.Response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var error = JsonConvert.DeserializeObject<SpotifyError>(ex.Response.Body as string);
                        if (error.ErrorDescription == "Refresh token revoked")
                        {
                            // TODO: Make this more language natural.. currently acts like a new user
                            logger.LogWarning("User {user} revoked Spotify credentials", user);
                            await RequestSpotifyAuthForUser(user, message, logger);
                            return true;
                        }
                    }

                    throw;
                }
            }

            return false;
        }

        private bool UserIsMissingSpotify(MBUser user)
        {
            return string.IsNullOrWhiteSpace(user.SpotifyId);
        }

        private string[] GetNetSpotifyScopesRequired(MBUser user)
        {
            var result = user.SpotifyScopesList ?? new List<string>();
            foreach (var scope in ScopesRequired)
            {
                if (!result.Contains(scope))
                {
                    result.Add(scope);
                }
            }

            if (RequiresSpotifyPremium && !result.Contains(Scopes.UserReadPrivate))
            {
                // So we can verify they have spotify premium
                // see Product property: 
                // https://developer.spotify.com/documentation/web-api/reference/users-profile/get-current-users-profile/#user-object-private
                result.Add(Scopes.UserReadPrivate);
            }

            return result.ToArray();
        }

        private async Task RequestSpotifyAuthForUser(MBUser user, Message message, ILogger logger)
        {
            if (message.Chat.Type != ChatType.Private)
            {
                await TelegramClient.SendTextMessageAsync(
                    message.Chat.Id,
                    $"Sure thing... Sending you a message shortly to get us connected to Spotify");
            }

            var state = new AuthorizationState()
            {
                Message = message,
                UserId = user.Id,
            };

            await TelegramClient.SendTextMessageAsync(
                user.ServiceId,
                $"OK, in order to continue, I need to connect to your Spotify so I can add you to the fun. Click the button below to get started.",
                replyMarkup: new InlineKeyboardMarkup(
                new InlineKeyboardButton()
                {
                    Text = "Connect Spotify Account",
                    Url = SpotifyService.GetAuthorizationUri(user, state, GetNetSpotifyScopesRequired(user)).ToString()
                }));
        }

        private async Task RequestTelegramAuthForUser(MBUser user, Message message, ILogger logger)
        {
            logger.LogInformation("Having user {user} auth with Telegram (chat: {chatid})", user, message.Chat.Id);
            var state = message.Base64Encode();
            await TelegramClient.SendTextMessageAsync(
                message.Chat.Id,
                $"Sure thing... First click this link to allow me to chat with you directly.",
                replyMarkup: new InlineKeyboardMarkup(
                new InlineKeyboardButton()
                {
                    Text = "Connect Account",
                    LoginUrl = new LoginUrl()
                    {
                        RequestWriteAccess = true,
                        Url = $"{Config.BaseUrl}/auth/telegram?state={state}"
                    }
                }));
            return;
        }

        private bool UserIsNotConnected(MBUser user)
        {
            return !user.ServiceAuthDate.HasValue;
        }

        private async Task ProcessMissingScopes(MBUser user, Message message, bool isAuthorizationCallback, ILogger log)
        {
            var scopesRequierd = GetNetSpotifyScopesRequired(user);
            var missingScopes = scopesRequierd.Except(user.SpotifyScopesList);

            if (isAuthorizationCallback)
            {
                // User already setup, this is an auth callback.. they must have denied scopes?
                // TODO: Send through error state from auth call

                log.LogWarning("User {user} denied scopes {scopes} for commmand {command}", user, string.Join(" ", missingScopes), this);

                await TelegramClient.SendTextMessageAsync(
                    message.Chat.Id,
                    $@"Sorry {user.ToTelegramUserLink()} but you need to grant additional permissions in order for us to run this command.",
                    ParseMode.Html);
                return;
            }

            // Request additional scopes
            var state = new AuthorizationState()
            {
                Message = message,
                UserId = user.Id
            };

            log.LogInformation("User {user} needs to grant additional scopes for {command} ({misingScopes})", user, this, string.Join(' ', missingScopes));

            if (message.Chat.Id != message.From.Id)
            {
                await TelegramClient.SendTextMessageAsync(
                    message.Chat.Id,
                    $"Sure thing {user.ToTelegramUserLink()}... we need a few more permissions from you first. Check your private messages.",
                    ParseMode.Html
                );
            }

            var response = string.IsNullOrWhiteSpace(user.SpotifyId)
                ? $"Sure! lets get you connected first. Click this link and authorize me to manage your Spotify player."
                : $"We need additional Spotify permissions from you to run the command {this.Command}. Please click this link and we'll get that sorted";

            await TelegramClient.SendTextMessageAsync(
                message.From.Id,
                response,
                ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(
                    new InlineKeyboardButton()
                    {
                        Text = "Connect Account",
                        Url = SpotifyService.GetAuthorizationUri(user, state, GetNetSpotifyScopesRequired(user)).ToString()
                    })
            );
        }

        private bool UserIsMissingScopes(MBUser user)
        {
            return !GetNetSpotifyScopesRequired(user)
                .All(scope => user.SpotifyScopes?.Contains(scope) ?? false);
        }

        protected abstract Task ProcessInternalAsync(MBUser user, Message message, ILogger logger, bool isAuthorizationCallback = false);

        protected abstract Task ProcessInternalAsync(MBUser user, CallbackQuery callback, ILogger logger);

        public override string ToString()
        {
            return this.GetType().Name;
        }
    }
}