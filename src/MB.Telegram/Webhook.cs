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
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            log.LogInformation(requestBody);

            return new OkResult();
        }
    }
}
