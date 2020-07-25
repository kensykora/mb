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

namespace MB.Telegram
{
    public static class Webhook
    {
        private static IConfiguration Configuration { set; get; }

        static Webhook()
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            
        }

        [FunctionName("Webhook")]
        public static async Task<IActionResult> WebhookCallback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EXxFeRY05OUBueJyHhXu")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation(Configuration.GetValue<string>("hostname"));
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            log.LogInformation(requestBody);

            return new OkResult();
        }
    }
}
