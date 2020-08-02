using System;
using System.Threading.Tasks;
using System.Web;
using MB.Telegram.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = MB.Telegram.Models.User;

namespace MB.Telegram.Commands
{
    public class ConnectCommand : BaseCommand
    {
        public override string CommandString => "/connect";

        public override string[] ScopesRequired => new string[] { };

        public const string SpotifyAuthorizeUrl = "https://accounts.spotify.com/authorize?response_type=code&client_id={0}&scope={1}&redirect_uri={2}&state={3}";

        private const string Spotify_Credential_Key_Format = "spotify_{0}";
        private readonly IUserService userService;
        private readonly IConfiguration config;

        public ConnectCommand() { }
        public ConnectCommand(ITelegramBotClient telegramClient, IUserService userService, ISpotifyService spotifyService, IConfiguration config) : base(telegramClient, spotifyService)
        {
            this.userService = userService;
            this.config = config;
        }

        protected override async Task ProcessInternal(User user, Update update, ILogger logger)
        {
            var record = await userService.GetUser(user.Id);

            if (update.Message.Text.Contains("again", StringComparison.CurrentCultureIgnoreCase) || user == null || string.IsNullOrWhiteSpace(user.SpotifyId))
            {
                var state = new AuthorizationState()
                {
                    ChatId = update.Message.Chat.Id.ToString(),
                    TelegramUpdateId = update.Id,
                    UserId = user.Id,
                };
                await TelegramClient.SendTextMessageAsync(
                    update.Message.Chat.Id, 
                    $"Click here to sign up: {SpotifyService.GetAuthorizationUri(user, state)}");
            }
            else
            {
                await TelegramClient.SendTextMessageAsync(update.Message.Chat.Id, $"{user.ToTelegramUserLink()} you're all set!", ParseMode.Html);
            }
        }
    }
}