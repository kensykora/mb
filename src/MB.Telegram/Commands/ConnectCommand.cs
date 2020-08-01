using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = MB.Telegram.Models.User;

namespace MB.Telegram.Commands
{
    public class ConnectCommand : BaseCommand
    {
        public override string CommandString => "/connect";

        private const string Spotify_Credential_Key_Format = "spotify_{0}";
        private readonly CloudTableClient userTable;
        private readonly IUserService userService;

        public ConnectCommand() { }
        public ConnectCommand(ITelegramBotClient telegramClient, IUserService userService) : base(telegramClient)
        {
            this.userService = userService;
        }

        public override async Task Process(User user, Update update, ILogger logger)
        {
            var record = await userService.GetUser(user.Id);
            if (user == null || string.IsNullOrWhiteSpace(user.SpotifyId))
            {
                await Client.SendTextMessageAsync(update.Message.Chat.Id, "Click here to sign up: [inline URL](http://www.example.com/)", ParseMode.MarkdownV2);
            }
        }
    }
}