using MB.Telegram.Commands;

namespace MB.Telegram.Services
{
    public interface ICommandService
    {
        IChatCommand GetCommand(string message);
    }
}