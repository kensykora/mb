using System;
using System.Text;
using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using MBUser = MB.Telegram.Models.MBUser;

namespace MB.Telegram.Commands
{
    public class UnknownCommand : BaseCommand
    {
        public UnknownCommand() { }
        public UnknownCommand(ITelegramBotClient client, ISpotifyService spotifyService, Config config) : base(client, spotifyService, config) { }
        public override string Command => null;
        public override string Description => null;
        public override bool Publish => false;

        public override string[] ScopesRequired => new string[] { };

        public override bool CanHandle(string message)
        {
            return true;
        }

        public override bool CanHandle(UnknownCallback callback)
        {
            return true;
        }

        protected override async Task ProcessInternalAsync(MBUser user, Message message, ILogger logger, bool isAuthorizationCallback = false)
        {
            await TelegramClient.SendTextMessageAsync(message.Chat.Id, $"I'm not sure how to deal with this: '{message.Text}'");
        }

        protected override Task ProcessInternalAsync(MBUser user, CallbackQuery callback, ILogger logger)
        {
            // Shouldn't be anything to do here... ever
            return Task.CompletedTask;
        }
    }

    public class UnknownCallback : BaseCallback
    {
        public UnknownCallback(IChatCommand command) : base(command)
        {
        }
    }
}