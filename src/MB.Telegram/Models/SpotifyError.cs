using Newtonsoft.Json;

namespace MB.Telegram.Models
{
    public class SpotifyError
    {
        [JsonProperty("error")]
        public string Error { get; set; }
        [JsonProperty("error_description")]
        public string ErrorDescription { get; set; }
    }
}