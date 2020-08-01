using System.Threading.Tasks;
using MB.Telegram.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using User = MB.Telegram.Models.User;

namespace MB.Telegram.Commands
{
    public interface IChatCommand
    {
        Task Process(User user, Update update, ILogger logger);

        bool CanHandle(string message);
    }
}