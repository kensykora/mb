using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using MB.Telegram.Services;
using AutoMapper;
using Telegram.Bot.Types.Enums;
using System.Linq;

namespace MB.Telegram
{
    public class Webhook
    {
        private readonly ITelegramBotClient telegramClient;
        private readonly IUserService userService;
        private readonly IMapper mapper;
        private readonly ICommandService commandService;

        public Webhook(ITelegramBotClient telegramClient, Services.IUserService userService, IMapper mapper, ICommandService commandService)
        {
            this.telegramClient = telegramClient;
            this.userService = userService;
            this.mapper = mapper;
            this.commandService = commandService;
        }
        static Webhook()
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            var config = builder.Build();
        }

        [FunctionName("Webhook")]
        public async Task<IActionResult> WebhookCallback(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "webhook")] HttpRequest req,
            ILogger log)
        {
            /*
            {"update_id":657184102, "message":{"message_id":9,"from":{"id":1061657778,"is_bot":false,"first_name":"Ken",
            "last_name":"Sykora","language_code":"en"},"chat":{"id":1061657778,"first_name":"Ken","last_name":"Sykora","type":"private"},
            "date":1595795188,"text":"hi"}}
            */
            var request = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogDebug(request);
            Update update = null;
            try
            {
                update = JsonConvert.DeserializeObject<Update>(request);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "error deserializing message");
                return new BadRequestResult();
            }

            // Check to see if it's from a user
            MB.Telegram.Models.User user = null;
            // TODO: Split this out?
            if (update?.Message?.From != null)
            {
                user = mapper.Map<MB.Telegram.Models.User>(update);
            }
            else
            {
                log.LogError("Update sent from non-user {message}", update);
                return new OkResult();
            }

            using (log.BeginScope("User {user} sent {message}", user.UserName, update?.Message?.Text))
            {

                await userService.CreateOrSetLastSeenUser(user);

                var command = commandService.GetCommand(update?.Message?.Text);
                if (command == null)
                {
                    log.LogInformation("Nothing to do for {message} from {user}",
                        update?.Message?.Text,
                        user);
                    return new OkResult();
                }

                log.LogInformation("Processing command {command} for {user} and message {message}",
                    command.GetType().Name,
                    user,
                    update?.Message?.Text
                );

                await command.Process(user, update, log);
            }

            return new OkResult();
        }
    }
}
