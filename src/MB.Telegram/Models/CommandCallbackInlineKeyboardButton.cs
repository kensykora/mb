using MB.Telegram.Commands;
using Telegram.Bot.Types.ReplyMarkups;

namespace MB.Telegram.Models
{
    public class CommandCallbackInlineKeyboardButton<T> : InlineKeyboardButton where T : BaseCallback
    {
        public T CallbackObject
        {
            get
            {
                return BaseCallback.Deserialize<T>(this.CallbackData);
            }
            set
            {
                this.CallbackData = value.Serialize();
            }
        }
    }
}