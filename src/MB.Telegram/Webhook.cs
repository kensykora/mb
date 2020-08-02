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

namespace MB.Telegram
{
    public class Webhook
    {
        private readonly ITelegramBotClient telegramClient;
        private readonly IUserService userService;
        private readonly ISpotifyService spotifyService;
        private readonly IMapper mapper;
        private readonly IConfiguration config;
        private readonly ICommandService commandService;

        public Webhook(ITelegramBotClient telegramClient, Services.IUserService userService, ISpotifyService spotifyService, IMapper mapper, IConfiguration config, ICommandService commandService)
        {
            this.telegramClient = telegramClient;
            this.userService = userService;
            this.spotifyService = spotifyService;
            this.mapper = mapper;
            this.config = config;
            this.commandService = commandService;
        }

        [FunctionName("Spotify")]
        public async Task<IActionResult> SpotifyCallback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "spotify")] HttpRequest req,
            ILogger log)
        {
            // // TODO: Read secure identifier from table
            // code=AQDzFkjIyKjAmwaH7Xmrb61_2RoEjnq2TfPZE9U4SdnsTO4cwpIADStoYniAziBf1vSjY7of6eSSlvQUl-KrgFCw_sTz184M_YUUV0eaH3uhTqE4VTmF1MPr-UnD1hlkYfxGfRiP2TYdeG8CA24XOz4ic3F9PUpOrAYSz1YlseMsIaESRHBz5occhFP0
            // error=
            // state=

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

            var update = (await telegramClient.GetUpdatesAsync(offset: state.TelegramUpdateId, limit: 1)).First();

            var command = commandService.GetCommand(update.Message.Text);
            if (command == null)
            {
                log.LogCritical("No command callback for message! This shouldn't ever happen {message} {user} {chat}", update.Message.Text, state.UserId, state.ChatId);
                return new InternalServerErrorResult();
            }

            await command.Process(user, update, log);

            // TODO: Figure out how to redirect to specific chat
            //return new RedirectResult($"https://tg.me/{config.GetValue<string>("telegramBotUsername")}");
            return new RedirectResult("tg://");
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
                var search = await userService.GetUser(user.Id);

                if (search == null)
                {
                    user.CreatedOn = DateTimeOffset.UtcNow;
                    user.LastSeen = DateTimeOffset.UtcNow;
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
