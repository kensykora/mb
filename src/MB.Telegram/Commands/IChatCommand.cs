using System.Threading.Tasks;
using MB.Telegram.Models;
using Microsoft.Extensions.Logging;

namespace MB.Telegram.Commands
{
    public interface IChatCommand
    {
        Task Process(User user, string message, ILogger logger);

        bool CanHandle(string message);
    }
}