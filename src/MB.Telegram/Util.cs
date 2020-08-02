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

        public static Message DeserializeState(string message)
        {
            return JsonConvert.DeserializeObject<Message>(Encoding.UTF8.GetString(Convert.FromBase64String(message)));
        }

        public static string Base64Encode(this Message message)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
        }
    }
}