using MB.Telegram.Commands;
using Telegram.Bot.Types;

namespace MB.Telegram.Services
{
    public interface ICommandService
    {
        IChatCommand GetCommand(string message);
        IChatCommand GetCommandForCallback(Update update);
    }
}