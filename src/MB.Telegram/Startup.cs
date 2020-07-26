using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

[assembly: FunctionsStartup(typeof(MB.Telegram.Startup))]


namespace MB.Telegram
{
    public class Startup : FunctionsStartup
    {
        public const string WebhookCallbackPath = "EXxFeRY05OUBueJyHhXu";
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var cb = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            var config = cb.Build();

            builder.Services.AddSingleton<ITelegramBotClient>(x => new TelegramBotClient(config.GetValue<string>("telegramApiKey")));
            builder.Services.AddSingleton<IConfiguration>(x => config);
        }
    }
}