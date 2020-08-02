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
using System.Web.Http;
using MB.Telegram.Models;

namespace MB.Telegram.Functions
{
    public class Webhook
    {
        private readonly ITelegramBotClient telegramClient;
        private readonly IUserService userService;
        private readonly TelegramLoginVerify widget;
        private readonly ISpotifyService spotifyService;
        private readonly IMapper mapper;
        private readonly IConfiguration config;
        private readonly ICommandService commandService;

        public Webhook(ITelegramBotClient telegramClient, Services.IUserService userService, TelegramLoginVerify widget, ISpotifyService spotifyService, IMapper mapper, IConfiguration config, ICommandService commandService)
        {
            this.telegramClient = telegramClient;
            this.userService = userService;
            this.widget = widget;
            this.spotifyService = spotifyService;
            this.mapper = mapper;
            this.config = config;
            this.commandService = commandService;
        }

        [FunctionName("Telegram")]
        public async Task<IActionResult> TelegramCallback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "telegram")] HttpRequest req,
            ILogger log)
        {
            // TODO: Handle User Revoke Access / Disconnect from Telegram 
            
            var authResult = widget.CheckAuthorization(req.Query.ToDictionary(p => p.Key, p => p.Value.FirstOrDefault()));
            switch (authResult)
            {
                case Authorization.InvalidAuthDateFormat: // TODO: Send them a note to retry with a new link.
                case Authorization.MissingFields: // TODO: Send them a note to retry with a new link.
                    log.LogError("Invalid auth result: {authResult}", authResult);
                    return new BadRequestObjectResult(authResult.ToString());
                case Authorization.InvalidHash:
                log.LogError("Invalid auth result: {authResult}", authResult);
                    return new UnauthorizedResult();
                case Authorization.TooOld: // TODO: Handle TooOld result - Send them a note to retry with a new link.
                    log.LogError("Invalid auth result: {authResult}", authResult);
                    return new BadRequestObjectResult(authResult.ToString());
                case Authorization.Valid:
                    break;
                default:
                    log.LogCritical("Unhandled result type " + authResult);
                    return new InternalServerErrorResult();
            }

            var telegramUser = new TelegramUser(req.Query);
            var user = await userService.GetUser($"{Prefix.Telegram}-{telegramUser.Id}");

            if (user == null)
            {
                // TODO: Handle user we don't know about clicking the link (for themselves)
                log.LogCritical("User not found in telegram callback: {user}", telegramUser);
                return new BadRequestObjectResult("haven't written this part yet...");
            }

            mapper.Map(telegramUser, user);
            await userService.UpdateUser(user);

            if(!req.Query.ContainsKey("state"))
            {
                return new BadRequestObjectResult("Missing state");
            }

            var update = Util.DeserializeState(req.Query["state"].FirstOrDefault());

            var cmd = commandService.GetCommand(update.Message.Text);

            await telegramClient.SendTextMessageAsync(
                telegramUser.Id,
                "Dang, it's official! We're connected! So... I gotta send this to you privately so it's just us talking... super secret stuff."
            );

            await cmd.Process(user, update, log, true);

            // TODO: Redirect to bot user
            return new RedirectResult("tg://");
        }

        [FunctionName("Spotify")]
        public async Task<IActionResult> SpotifyCallback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "spotify")] HttpRequest req,
            ILogger log)
        {
            // TODO: Validate Spotify Premium account

            // // TODO: Read secure identifier from table
            // code=AQDzFkjIyKjAmwaH7Xmrb61_2RoEjnq2TfPZE9U4SdnsTO4cwpIADStoYniAziBf1vSjY7of6eSSlvQUl-KrgFCw_sTz184M_YUUV0eaH3uhTqE4VTmF1MPr-UnD1hlkYfxGfRiP2TYdeG8CA24XOz4ic3F9PUpOrAYSz1YlseMsIaESRHBz5occhFP0
            // error=
            // state=

            // TODO: Verify the spotify account came from the user that authorized it

            if (!string.IsNullOrWhiteSpace(req.Query["error"]))
            {
                log.LogInformation("User spotify error: {error}", req.Query["error"]);
                return new BadRequestObjectResult(req.Query["error"]); // TODO: Redirect to telegram - handle denied auth
            }
            else if (string.IsNullOrWhiteSpace(req.Query["code"]))
            {
                log.LogCritical("wtf, code missing and error not found.", req.Query);

                // TODO: Handle weird state that shouldn't happen
                return new BadRequestObjectResult("Something went wrong. I'll look into it.");
            }

            var stateString = req.Query["state"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(stateString))
            {
                // TODO: Handle bad state error
                log.LogError("No state");
                return new BadRequestObjectResult("No State");
            }

            AuthorizationState state = null;

            try
            {
                state = spotifyService.DeserializeState(stateString);
            }
            catch (Exception ex)
            {
                // TODO: Handle bad state error
                log.LogError(ex, "Unexpected error deserializing state");
                return new BadRequestObjectResult("Bad State");
            }

            // TODO: Validate security token

            var user = await userService.GetUser(state.UserId);

            if (user == null)
            {
                // TODO: Handle not found
                return new BadRequestObjectResult("Couldn't find the originating request");
            }

            await spotifyService.RedeemAuthorizationCode(user, req.Query["code"].FirstOrDefault());

            var command = commandService.GetCommand(state.Update.Message.Text);
            if (command == null)
            {
                log.LogCritical("No command callback for message! This shouldn't ever happen {message} {user}", state.Update.Message.Text, state.UserId);
                return new InternalServerErrorResult();
            }

            await command.Process(user, state.Update, log, isAuthorizationCallback: true);

            // TODO: Figure out how to redirect to specific chat
            //return new RedirectResult($"https://tg.me/{config.GetValue<string>("telegramBotUsername")}");
            return new RedirectResult("tg://");
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
                    return await HandleTextMessage(update, log);
                default:
                    log.LogInformation("Ignoring -- Don't know how to deal with message type {type} {message}", message.Type, request);
                    return new OkResult();
            }
        }

        private async Task<IActionResult> HandleTextMessage(Update update, ILogger log)
        {
            log.LogDebug("Handling text message {update}", update);

            // Check to see if it's from a user
            MB.Telegram.Models.MBUser user = null;

            if (update?.Message?.From != null)
            {
                user = mapper.Map<MB.Telegram.Models.MBUser>(update);
            }

            using (log.BeginScope("User {user} sent {message}", user.UserName, update?.Message?.Text))
            {
                var search = await userService.GetUser(user.Id);

                if (search == null)
                {
                    user.CreatedOn = DateTimeOffset.UtcNow;
                    user.LastSeen = DateTimeOffset.UtcNow;

                    if (update.Message.Chat.Type == ChatType.Private)
                    {
                        // Private chat implies they have authorized us to send them messages
                        // We can skip the telegram auth step.
                        user.ChatServiceAuthDate = DateTimeOffset.UtcNow;
                    }

                    await userService.CreateUser(user);
                }
                else
                {
                    await userService.SetLastSeenUser(search);
                    user = search;
                }

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
