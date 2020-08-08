using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MB.Telegram.Commands;
using MB.Telegram.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Telegram.Bot;
using Telegram.Bot.Types;

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
            var config = new Config();
            cb.Build().Bind(config);

            builder.Services.AddSingleton<ITelegramBotClient>(x => new TelegramBotClient(config.TelegramApiKey));
            builder.Services.AddSingleton<TelegramLoginVerify>(x => new TelegramLoginVerify(config.TelegramApiKey));
            builder.Services.AddSingleton<Config>(x => config);
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddSingleton<IUserService, UserService>();
            builder.Services.AddSingleton<ICommandService, CommandService>();
            builder.Services.AddSingleton<ISpotifyService, SpotifyService>();
            builder.Services.AddSingleton<CloudTableClient>(x => GetCloudTableClient(
                config.StorageAccountName,
                config.StorageAccountKey,
                config.UseLocalStorage
            ));
            builder.Services.AddSingleton<SecretClient>(x =>
                new SecretClient(
                    new Uri($"https://{config.KeyVaultName}.vault.azure.net"),
                    new DefaultAzureCredential(includeInteractiveCredentials: Debugger.IsAttached)));

            var commands = FindDerivedTypes(Assembly.GetExecutingAssembly(), typeof(BaseCommand))
                    .Select(x => (IChatCommand)Activator.CreateInstance(x))
                    .ToList();

            builder.Services.AddSingleton<List<IChatCommand>>(commands);
            List<BotCommand> telegramCommands = new List<BotCommand>();
            foreach (var cmd in commands)
            {
                builder.Services.AddScoped(cmd.GetType());
                if (cmd.Publish)
                {
                    telegramCommands.Add(new BotCommand()
                    {
                        Command = cmd.Command,
                        Description = cmd.Description
                    });
                }
            }

            var telegram = new TelegramBotClient(config.TelegramApiKey);
            telegram.SetMyCommandsAsync(telegramCommands);
            telegram.SetWebhookAsync(config.BaseUrl + "/webhook");
        }
        public IEnumerable<Type> FindDerivedTypes(Assembly assembly, Type baseType)
        {
            return assembly.GetTypes().Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && t != baseType);
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