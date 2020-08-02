using System;
using System.Text;
using MB.Telegram.Models;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace MB.Telegram
{
    public static class Util
    {
        public static string ToTelegramUserLink(this MBUser user)
        {
            return $"<a href=\"tg://user?id={user.ServiceId}\">{user.DisplayName}</a>";
        }

        public static Update DeserializeState(string update)
        {
            return JsonConvert.DeserializeObject<Update>(Encoding.UTF8.GetString(Convert.FromBase64String(update)));
        }

        public static string Base64Encode(this Update update)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(update)));
        }
    }
}