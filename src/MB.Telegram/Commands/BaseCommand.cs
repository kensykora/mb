using System;
using System.Linq;
using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = MB.Telegram.Models.User;

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
        public async Task Process(User user, Update update, ILogger logger)
        {
            if (!ScopesRequired.All(scope => user.SpotifyScopes.Contains(scope)))
            {
                var state = new AuthorizationState()
                {
                    ChatId = update.Message.Chat.Id.ToString(),
                    TelegramUpdateId = update.Id,
                    UserId = user.Id
                };
                await TelegramClient.SendTextMessageAsync(
                    update.Message.Chat.Id,
                    $@"Sorry {user.ToTelegramUserLink()} but we need additional permissions from you to do that. 
                    Please click this link and we'll get that sorted:<br/>
                    <br/>
                    {SpotifyService.GetAuthorizationUri(user, state, ScopesRequired)}",
                    ParseMode.Html
                );
                return;
            }

            await ProcessInternal(user, update, logger);
        }

        protected abstract Task ProcessInternal(User user, Update update, ILogger logger);
    }
}