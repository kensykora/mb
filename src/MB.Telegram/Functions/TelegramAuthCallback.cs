using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using AutoMapper;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace MB.Telegram.Functions
{
    public class TelegramAuthCallback
    {
        private readonly TelegramLoginVerify widget;
        private readonly IUserService userService;
        private readonly ITelegramBotClient telegramClient;
        private readonly ICommandService commandService;
        private readonly IMapper mapper;

        public TelegramAuthCallback(TelegramLoginVerify widget, IUserService userService,
                                    ITelegramBotClient telegramClient, ICommandService commandService, IMapper mapper)
        {
            this.widget = widget;
            this.userService = userService;
            this.telegramClient = telegramClient;
            this.commandService = commandService;
            this.mapper = mapper;
        }

        [FunctionName("Telegram")]
        public async Task<IActionResult> TelegramCallback(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/telegram")] HttpRequest req,
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
            var user = await userService.GetUser($"{Prefix.Telegram}|{telegramUser.Id}");

            if (user == null)
            {
                // TODO: Handle user we don't know about clicking the link (for themselves)
                log.LogCritical("User not found in telegram callback: {user}", telegramUser);
                return new BadRequestObjectResult("haven't written this part yet...");
            }

            mapper.Map(telegramUser, user);
            user.ServiceAuthDate = DateTimeOffset.UtcNow;
            await userService.UpdateUser(user);

            if (!req.Query.ContainsKey("state"))
            {
                return new BadRequestObjectResult("Missing state");
            }

            var update = Util.DeserializeState(req.Query["state"].FirstOrDefault());

            var cmd = commandService.GetCommand(update.Text);

            await telegramClient.SendTextMessageAsync(
                telegramUser.Id,
                "Dang, it's official! We're connected! So... I gotta send this to you privately so it's just us talking... super secret stuff."
            );

            await cmd.Process(user, update, log, true);

            // TODO: Redirect to bot user
            return new RedirectResult("tg://");
        }
    }
}