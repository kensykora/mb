using System.Threading.Tasks;
using MB.Telegram.Models;
using MB.Telegram.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using MBUser = MB.Telegram.Models.MBUser;

namespace MB.Telegram.Commands
{
    public interface IChatCommand
    {
        Task Process(MBUser user, Update update, ILogger logger, bool isAuthorizationCallback = false);

        bool CanHandle(string message);
    }
}