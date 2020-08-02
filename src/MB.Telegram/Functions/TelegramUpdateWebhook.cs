using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MB.Telegram.Services;
using AutoMapper;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MB.Telegram.Functions
{
    public class TelegramUpdateWebhook
    {

        private readonly IUserService userService;
        private readonly IMapper mapper;
        private readonly ICommandService commandService;

        public TelegramUpdateWebhook(IUserService userService, IMapper mapper, ICommandService commandService)
        {
            this.userService = userService;
            this.mapper = mapper;
            this.commandService = commandService;
        }

        [FunctionName("Webhook")]
        public async Task<IActionResult> WebhookCallback(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "webhook")] HttpRequest req,
            ILogger log)
        {
            // TODO: ChatMemberLeft
            // TODO: ChatMemberLeft (Deleted Channel)
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

            if (update.ChannelPost != null || update.EditedChannelPost != null)
            {
                var post = update.EditedChannelPost ?? update.ChannelPost;
                // TODO: Handle channel stuff
                /*
                    [8/2/2020 7:48:50 PM] {"update_id":257364048,
                    "channel_post":{"message_id":2,"chat":{"id":-1001208267207,"title":"music bot test channel","type":"channel"},"date":1596397699,"text":"hi"}}
                */
                log.LogInformation("Channel post {channel}, ignoring.", post.Chat.Title);
                return new OkResult();
            }

            var message = update.EditedMessage ?? update.Message;
            switch (message.Type)
            {
                // TODO: Handle channel join -- initiate channel
                // TODO: Handle Group Join
                /* Group Post:
                    {"update_id":257364050,
                    "message":{"message_id":218,"from":{"id":1061657778,"is_bot":false,"first_name":"Ken","last_name":"Sykora","username":"kensykora","language_code":"en"},"chat":{"id":-497302716,"title":"music bot test group","type":"group","all_members_are_administrators":true},"date":1596397848,"group_chat_created":true}}
                    */
                case MessageType.Text:
                    return await HandleTextMessage(message, log);
                default:
                    log.LogInformation("Ignoring -- Don't know how to deal with message type {type} {message}", message.Type, request);
                    return new OkResult();
            }
        }

        private async Task<IActionResult> HandleTextMessage(Message message, ILogger log)
        {
            log.LogDebug("Handling text message {message}", message);

            // Check to see if it's from a user
            MB.Telegram.Models.MBUser user = null;

            user = mapper.Map<MB.Telegram.Models.MBUser>(message);

            using (log.BeginScope("User {user} sent {message}", user.UserName, message.Text))
            {
                var search = await userService.GetUser(user.Id);

                if (search == null)
                {
                    user.CreatedOn = DateTimeOffset.UtcNow;
                    user.LastSeen = DateTimeOffset.UtcNow;

                    if (message.Chat.Type == ChatType.Private)
                    {
                        // Private chat implies they have authorized us to send them messages
                        // We can skip the telegram auth step.
                        user.ServiceAuthDate = DateTimeOffset.UtcNow;
                    }

                    await userService.CreateUser(user);
                }
                else
                {
                    await userService.SetLastSeenUser(search);
                    user = search;
                }

                var command = commandService.GetCommand(message.Text);
                if (command == null)
                {
                    log.LogInformation("Nothing to do for {message} from {user}",
                        message.Text,
                        user);
                    return new OkResult();
                }

                log.LogInformation("Processing command {command} for {user} and message {message}",
                    command.GetType().Name,
                    user,
                    message.Text
                );

                await command.Process(user, message, log);
            }

            return new OkResult();
        }
    }
}
