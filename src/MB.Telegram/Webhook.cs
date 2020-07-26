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

namespace MB.Telegram
{
    public class Webhook
    {
        private readonly ITelegramBotClient telegramClient;

        public Webhook(ITelegramBotClient telegramClient)
        {
            this.telegramClient = telegramClient;
        }
        static Webhook()
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            var config = builder.Build();
        }

        [FunctionName("Webhook")]
        public async Task<IActionResult> WebhookCallback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EXxFeRY05OUBueJyHhXu")] HttpRequest req,
            ILogger log)
        {
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
                return new OkResult();
            }

            await telegramClient.SendTextMessageAsync(update.Message.Chat.Id, update.Message.Text);

            return new OkResult();
        }
    }
}
