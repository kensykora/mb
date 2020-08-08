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
using System.Linq;
using MB.Telegram.Models;

namespace MB.Telegram.Functions
{
    public class TelegramUpdateWebhook
    {
        // Telegram Update Types
        // message
        // edited_message
        // channel_post
        // edited_channel_post
        // inline_query
        // chosen_inline_result
        // callback_query
        // shipping_query
        // pre_checkout_query
        // poll
        // poll_answer
        public static UpdateType[] SupportedUpdateTypes => new UpdateType[] { UpdateType.Message, UpdateType.CallbackQuery };

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
            // TODO: Joined group without permissions
            // TODO: CHat member joined
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

            switch (update.Type)
            {
                case UpdateType.Message:
                    return await HandleMessage(log, update);
                case UpdateType.CallbackQuery:
                    return await HandleCallback(log, update);
                default:
                    log.LogError("Unable to handle update type {type}: {raw}", update.Type, request);
                    return new OkResult();
            }
        }

        private async Task<IActionResult> HandleCallback(ILogger log, Update update)
        {
            /*{"update_id":257364106,
"callback_query":{"id":"4559785438211274145","from":{"id":1061657778,"is_bot":false,"first_name":"Ken","last_name":"Sykora","username":"kensykora",
"language_code":"en"},"message":{"message_id":335,"from":{"id":1394272461,"is_bot":true,"first_name":"mbot testing","username":"emmmmm_bot"},
"chat":{"id":-460508819,"title":"vnkd","type":"group","all_members_are_administrators":true},"date":1596907414,"text":"OK. Since this the first time setting up, 
a listen session, we need to select an owner. The owner will have a playlist created and managed by me. The first person to click this button becomes the owner. 
READY, GO.","reply_markup":{"inline_keyboard":[[{"text":"Make me the owner!","callback_data":"own"}]]}},"chat_instance":"6467729790075545641","data":"own"}}
*/
            var user = await CreateOrUpdateUser(update.CallbackQuery.From);

            var command = commandService.GetCommandForCallback(update);

            if (command == null)
            {
                log.LogCritical("Invalid callback received: " + update.CallbackQuery.Data);
                return new OkResult();
            }

            await command.Process(user, update.CallbackQuery, log);

            return new OkResult();
        }

        private async Task<IActionResult> HandleMessage(ILogger log, Update update)
        {
            var message = update.EditedMessage ?? update.Message;

            var user = await CreateOrUpdateUser(message.From);

            switch (message.Type)
            {
                // TODO: Handle channel join -- initiate channel
                // TODO: Handle Group Join
                /* Group Post:
                    {"update_id":257364050,
                    "message":{"message_id":218,"from":{"id":1061657778,"is_bot":false,"first_name":"Ken","last_name":"Sykora","username":"kensykora","language_code":"en"},"chat":{"id":-497302716,"title":"music bot test group","type":"group","all_members_are_administrators":true},"date":1596397848,"group_chat_created":true}}
                    */
                case MessageType.Text:
                    return await HandleTextMessage(message, user, log);
                case MessageType.GroupCreated:
                    return await HandleGroupCreatedMessage(message, user, log);
                default:
                    log.LogError("Unhandled Message Type: Ignoring -- Don't know how to deal with message type {type}", message.Type);
                    return new OkResult();
            }
        }

        private async Task<MBUser> CreateOrUpdateUser(User telegramUser)
        {
            var user = mapper.Map<MBUser>(telegramUser); ;

            var search = await userService.GetUser(user.Id);

            if (search == null)
            {
                user.CreatedOn = DateTimeOffset.UtcNow;
                user.LastSeen = DateTimeOffset.UtcNow;
                user.ServiceAuthDate = DateTimeOffset.UtcNow;

                await userService.CreateUser(user);
            }
            else
            {
                await userService.SetLastSeenUser(search);
                user = search;
            }

            return user;
        }

        private async Task<IActionResult> HandleGroupCreatedMessage(Message message, MBUser user, ILogger log)
        {
            log.LogDebug("Invited to group by {user} group created message {chat}", user, message.Chat.Title);

            // TODO: Onboard channel

            return new OkResult();
        }

        private async Task<IActionResult> HandleTextMessage(Message message, MBUser user, ILogger log)
        {
            log.LogDebug("Handling text message {message}", message.Text);

            if (!(message.Entities?.Any(x => x.Type == MessageEntityType.BotCommand) ?? false))
            {
                log.LogInformation("Non-Command received");
                return new OkResult();
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

            return new OkResult();
        }
    }
}
