using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MB.Telegram.Commands;
using Microsoft.Extensions.Logging;

namespace MB.Telegram.Services
{

    public class CommandService : ICommandService
    {
        private readonly IUserService userService;
        private readonly ILogger logger;

        public List<IChatCommand> commands;

        public CommandService(IUserService userService, ILogger<CommandService> logger, List<IChatCommand> commands = null)
        {
            this.userService = userService ?? throw new System.ArgumentNullException(nameof(userService));
            this.logger = logger;
            this.commands = commands;

            if (this.commands == null)
            {
                this.commands = new List<IChatCommand>();

                var types = FindDerivedTypes(Assembly.GetExecutingAssembly(), typeof(BaseCommand));

                foreach (var type in types)
                {
                    logger.LogInformation("Initializing with Command {command}", type.Name);
                    this.commands.Add((IChatCommand)Activator.CreateInstance(type));
                }
            }
        }

        public IEnumerable<Type> FindDerivedTypes(Assembly assembly, Type baseType)
        {
            return assembly.GetTypes().Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && t != baseType);
        }

        public IChatCommand GetCommand(string message)
        {
            IChatCommand result = commands.FirstOrDefault(x => x.CanHandle(message));

            if (result != null)
            {
                result = (IChatCommand)Activator.CreateInstance(result.GetType());
            }

            logger.LogDebug("Command {result} for message {message}", result?.GetType(), message);

            return result;
        }
    }
}