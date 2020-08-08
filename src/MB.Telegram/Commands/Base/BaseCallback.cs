using System;
using System.Text;
using Newtonsoft.Json;

namespace MB.Telegram.Commands
{
    public abstract class BaseCallback
    {
        public BaseCallback(IChatCommand command)
        {
            CommandType = command.GetType().Name;
        }
        [JsonProperty("t")]
        public string CommandType { get; set; }
        [JsonProperty("a")]
        public string Action { get; set; }

        public string Serialize()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this)));
        }
        
        public static T Deserialize<T>(string base64Data) where T : BaseCallback
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(Convert.FromBase64String(base64Data)));
        }
    }
}