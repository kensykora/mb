using MB.Telegram.Models;

namespace MB.Telegram
{
    public static class Util
    {
        public static string ToTelegramUserLink(this User user)
        {
            return $"<a href=\"tg://user?id={user.ServiceId}\">{user.DisplayName}</a>";
        }
    }
}