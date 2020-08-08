using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MB.Telegram.Commands;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MB.Telegram.Services
{

    public class CommandService : ICommandService
    {
        private readonly IUserService userService;
        private readonly ILogger logger;
        private readonly ITelegramBotClient client;
        private readonly IServiceProvider serviceProvider;
        public List<IChatCommand> commands;

        public CommandService(IUserService userService, ILogger<CommandService> logger, ITelegramBotClient client, IServiceProvider serviceProvider, List<IChatCommand> commands = null)
        {
            this.userService = userService ?? throw new System.ArgumentNullException(nameof(userService));
            this.logger = logger;
            this.client = client;
            this.serviceProvider = serviceProvider;
            this.commands = commands;
        }

        public IChatCommand GetCommand(string message)
        {
            IChatCommand result = commands.FirstOrDefault(x => x.CanHandle(message));

            if (result != null)
            {
                result = (IChatCommand)serviceProvider.GetService(result.GetType());
            }

            logger.LogDebug("Command {result} for message {message}", result?.GetType(), message);

            return result;
        }

        public IChatCommand GetCommandForCallback(Update update)
        {
            var callbackData = BaseCallback.Deserialize<UnknownCallback>(update.CallbackQuery.Data);
            IChatCommand result = commands.FirstOrDefault(x => x.CanHandle(callbackData));

            if (result != null)
            {
                result = (IChatCommand)serviceProvider.GetService(result.GetType());
            }

            logger.LogDebug("Command {result} for message callback {message}", result?.GetType(), update.CallbackQuery.Message.Text);

            return result;
        }
    }
}