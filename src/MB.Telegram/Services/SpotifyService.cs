using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using MB.Telegram.Models;
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
        Task<PrivateUser> RedeemAuthorizationCode(MBUser user, string authorizationCode);
        string SerializeState(AuthorizationState state);
        AuthorizationState DeserializeState(string state);
        Task<ISpotifyClient> GetClientAsync(MBUser user);
    }

    public class SpotifyService : ISpotifyService
    {
        private readonly IUserService userService;
        private readonly Config config;
        private readonly SecretClient secretClient;
        private readonly ILogger log;

        private const string SpotifySecretKeyFormat = "spotify-{0}";

        public SpotifyService(IUserService userService, Config config, SecretClient secretClient, ILogger log)
        {
            this.userService = userService;
            this.config = config;
            this.secretClient = secretClient;
            this.log = log;
        }

        public Uri RedirectUri => new Uri(config.BaseUrl + "/auth/spotify");

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
                config.SpotifyClientId,
                LoginRequest.ResponseType.Code
            )
            {
                Scope = scopes,
                State = SerializeState(state)
            }.ToUri();
        }

        public async Task<PrivateUser> RedeemAuthorizationCode(MBUser user, string authorizationCode)
        {
            var response = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(
                    config.SpotifyClientId,
                    config.SpotifyClientSecret,
                    authorizationCode,
                    RedirectUri)
            );

            var spotify = new SpotifyClient(response.AccessToken);
            var profile = await spotify.UserProfile.Current();

            await userService.UpdateSpotifyDetails(user, response.Scope, profile.Id);
            await SaveTokenAsync(user, response);

            return profile;
        }

        private async Task SaveTokenAsync(MBUser user, AuthorizationCodeTokenResponse response)
        {
            var secret = new KeyVaultSecret(
                                name: GetUserTokenKey(user),
                                value: JsonConvert.SerializeObject(response));
            secret.Properties.ContentType = "application/json";
            secret.Properties.NotBefore = response.CreatedAt.ToUniversalTime();
            secret.Properties.ExpiresOn = response.CreatedAt.AddSeconds(response.ExpiresIn).ToUniversalTime();
            await secretClient.SetSecretAsync(secret);
        }

        private static string GetUserTokenKey(MBUser user)
        {
            return string.Format(SpotifySecretKeyFormat, user.Id.Replace('|','-'));
        }

        public async Task<ISpotifyClient> GetClientAsync(MBUser user)
        {
            var token = await GetTokenAsync(user);
            var authenticator = new AuthorizationCodeAuthenticator(config.SpotifyClientId, config.SpotifyClientSecret, token);
            authenticator.TokenRefreshed += delegate(object o, AuthorizationCodeTokenResponse token) 
            {
                // TODO: Logging via constructor - this value of log is currently null
                // log.LogInformation("Refreshing spotify token for user {user}", user);
                Task.Run(async () => {
                    await SaveTokenAsync(user, token);
                }).Wait();
            };

            var spotifyConfig = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(authenticator);

            return new SpotifyClient(spotifyConfig);
        }

        private async Task<AuthorizationCodeTokenResponse> GetTokenAsync(MBUser user)
        {
            var response = await secretClient.GetSecretAsync(GetUserTokenKey(user));

            if (response.Value == null)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(response.Value.Value);
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
        public Message Message { get; set; }
        // TODO: Security Token
    }
}