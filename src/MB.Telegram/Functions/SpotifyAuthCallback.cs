using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using MB.Telegram.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace MB.Telegram.Functions
{
    public class SpotifyAuthCallback
    {
        private readonly ISpotifyService spotifyService;
        private readonly IUserService userService;
        private readonly ICommandService commandService;
        private readonly ITelegramBotClient telegramBotClient;

        public SpotifyAuthCallback(ISpotifyService spotifyService, IUserService userService,
                                   ICommandService commandService, ITelegramBotClient telegramBotClient)
        {
            this.spotifyService = spotifyService;
            this.userService = userService;
            this.commandService = commandService;
            this.telegramBotClient = telegramBotClient;
        }

        [FunctionName("Spotify")]
        public async Task<IActionResult> SpotifyCallback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/spotify")] HttpRequest req,
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

            var profile = await spotifyService.RedeemAuthorizationCode(user, req.Query["code"].FirstOrDefault());
            
            // TODO: differentiate message based on new connection or scope update
            await telegramBotClient.SendTextMessageAsync(
                user.ServiceId,
                "Done!" // TODO: use one of those sweet confirmation animations
            );

            var command = commandService.GetCommand(state.Message.Text);
            if (command == null)
            {
                log.LogCritical("No command callback for message! This shouldn't ever happen {message} {user}", state.Message.Text, state.UserId);
                return new InternalServerErrorResult();
            }

            await command.Process(user, state.Message, log, isAuthorizationCallback: true);

            // TODO: Figure out how to redirect to specific chat
            //return new RedirectResult($"https://tg.me/{config.TelegramBotUserName}");
            return new RedirectResult("tg://");
        }
    }
}