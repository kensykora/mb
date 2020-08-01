using System;
using AutoMapper;
using MB.Telegram.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
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
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddSingleton<IUserService, UserService>();
            builder.Services.AddSingleton<ICommandService, CommandService>();
            builder.Services.AddSingleton<CloudTableClient>(x => GetCloudTableClient(
                config.GetValue<string>("storageAccountName"),
                config.GetValue<string>("storageAccountKey"),
                config.GetValue<bool?>("useLocalStorage")
            ));

            
        }

        private static CloudTableClient GetCloudTableClient(string storageAccountName, string key, bool? useLocal = false)
        {
            var storageCredentials = new StorageCredentials(storageAccountName, key);
            var cloudTableClient = new CloudTableClient(new StorageUri(
                (useLocal ?? false)
                ? new Uri($"http://127.0.0.1:10002/{storageAccountName}")
                : new Uri($"https://{storageAccountName}.table.core.windows.net")
                ), storageCredentials);

            return cloudTableClient;
        }
    }
}