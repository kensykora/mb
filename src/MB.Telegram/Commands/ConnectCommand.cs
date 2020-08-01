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
using User = MB.Telegram.Models.User;

namespace MB.Telegram.Commands
{
    public class ConnectCommand : BaseCommand
    {
        public override string CommandString => "/connect";

        public const string SpotifyAuthorizeUrl = "https://accounts.spotify.com/authorize?response_type=code&client_id={0}&scope={1}&redirect_uri={2}&state={3}";

        private const string Spotify_Credential_Key_Format = "spotify_{0}";
        private readonly CloudTableClient userTable;
        private readonly IUserService userService;
        private readonly ISpotifyService spotifyService;
        private readonly IConfiguration config;

        public ConnectCommand() { }
        public ConnectCommand(ITelegramBotClient telegramClient, IUserService userService, ISpotifyService spotifyService, IConfiguration config) : base(telegramClient)
        {
            this.userService = userService;
            this.spotifyService = spotifyService;
            this.config = config;
        }

        public override async Task Process(User user, Update update, ILogger logger)
        {
            var record = await userService.GetUser(user.Id);
            
            if (user == null || string.IsNullOrWhiteSpace(user.SpotifyId))
            {
                await Client.SendTextMessageAsync(update.Message.Chat.Id, $"Click here to sign up: {spotifyService.GetAuthorizationUri(user.Id)}");
            }
        }
    }
}