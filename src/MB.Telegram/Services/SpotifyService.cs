using System;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using MB.Telegram.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SpotifyAPI.Web;

namespace MB.Telegram.Services
{
    public interface ISpotifyService
    {
        Uri RedirectUri { get; }

        Uri GetAuthorizationUri(string userId, long chatId);
        Task RedeemAuthorizationCode(User user, string authorizationCode);
    }

    public class SpotifyService : ISpotifyService
    {
        private readonly IUserService userService;
        private readonly IConfiguration config;
        private readonly SecretClient secretClient;
        private readonly ILogger log;

        private const string SpotifySecretKeyFormat = "spotify-{0}";

        public SpotifyService(IUserService userService, IConfiguration config, SecretClient secretClient, ILogger log)
        {
            this.userService = userService;
            this.config = config;
            this.secretClient = secretClient;
            this.log = log;
        }

        public Uri RedirectUri => new Uri(config.GetValue<string>("baseUrl") + "/spotify");

        public Uri GetAuthorizationUri(string userId, long chatId)
        {
            return new LoginRequest(
                RedirectUri,
                config.GetValue<string>("spotifyClientId"),
                LoginRequest.ResponseType.Code
            )
            {
                Scope = new string[] { }, // TODO: Scopes
                State = $"{userId}|{chatId}|abcdefg" // TODO: Security Token
            }.ToUri();
        }

        public async Task RedeemAuthorizationCode(User user, string authorizationCode)
        {
            var response = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(
                    config.GetValue<string>("spotifyClientId"),
                    config.GetValue<string>("spotifyClientSecret"),
                    authorizationCode,
                    RedirectUri)
            );

            var spotify = new SpotifyClient(response.AccessToken);
            var profile = await spotify.UserProfile.Current();

            await userService.UpdateSpotifyDetails(user, response.Scope, profile.Id);

            var secret = new KeyVaultSecret(
                    name: string.Format(SpotifySecretKeyFormat, user.Id), 
                    value: JsonConvert.SerializeObject(response));
            secret.Properties.ContentType = "application/json";
            secret.Properties.NotBefore = response.CreatedAt.ToUniversalTime();
            secret.Properties.ExpiresOn = response.CreatedAt.AddSeconds(response.ExpiresIn).ToUniversalTime();
            await secretClient.SetSecretAsync(secret);
        }
    }
}