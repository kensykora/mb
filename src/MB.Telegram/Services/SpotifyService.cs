using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using MB.Telegram.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using Telegram.Bot.Types;

namespace MB.Telegram.Services
{
    public interface ISpotifyService
    {
        Uri RedirectUri { get; }

        Uri GetAuthorizationUri(MBUser user, AuthorizationState state, string[] additionalScopes = null);
        Task RedeemAuthorizationCode(MBUser user, string authorizationCode);
        string SerializeState(AuthorizationState state);
        AuthorizationState DeserializeState(string state);
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

        public Uri GetAuthorizationUri(MBUser user, AuthorizationState state, string[] additionalScopes = null)
        {
            var scopes = user.SpotifyScopesList ?? new List<string>();
            if (additionalScopes != null)
            {
                foreach (var scope in additionalScopes)
                {
                    if (!scopes.Contains(scope))
                    {
                        scopes.Add(scope);
                    }
                }
            }

            return new LoginRequest(
                RedirectUri,
                config.GetValue<string>("spotifyClientId"),
                LoginRequest.ResponseType.Code
            )
            {
                Scope = scopes,
                State = SerializeState(state)
            }.ToUri();
        }

        public async Task RedeemAuthorizationCode(MBUser user, string authorizationCode)
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

        public AuthorizationState DeserializeState(string state)
        {
            return JsonConvert.DeserializeObject<AuthorizationState>(Encoding.UTF8.GetString(Convert.FromBase64String(state)));
        }

        public string SerializeState(AuthorizationState state)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(state)));
        }
    }

    public class AuthorizationState
    {
        public string UserId { get; set; }
        public Update Update { get; set; }
        // TODO: Security Token
    }
}