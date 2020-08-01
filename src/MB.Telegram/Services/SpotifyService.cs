using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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

        Uri GetAuthorizationUri(string userId);
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

        public Uri GetAuthorizationUri(string userId)
        {
            return new LoginRequest(
                RedirectUri,
                config.GetValue<string>("spotifyClientId"),
                LoginRequest.ResponseType.Code
            )
            {
                Scope = new string[] { }, // TODO: Scopes
                State = userId + "|abcdefg" // TODO: State
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
            await secretClient.SetSecretAsync(string.Format(SpotifySecretKeyFormat, user.Id), JsonConvert.SerializeObject(response));
        }
    }

    // public class SpotifyAuthorizationResponseMessage
    // {
    //     [JsonProperty("access_token")]
    //     public string AccessToken { get; set; }

    //     [JsonProperty("token_type")]
    //     public string TokenType { get; set; }

    //     [JsonProperty("scope")]
    //     public string Scope { get; set; }

    //     [JsonProperty("expires_in")]
    //     public int ExpiresIn { get; set; }

    //     [JsonProperty("refresh_token")]
    //     public string RefreshToken { get; set; }
    // }
}