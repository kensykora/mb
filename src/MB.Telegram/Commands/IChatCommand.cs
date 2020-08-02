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
        string Command { get; }
        string Description { get; }
        bool Publish { get; }
        bool RequiresSpotify { get; }
        bool RequiresBotConnection { get; }
        Task Process(MBUser user, Message update, ILogger logger, bool isAuthorizationCallback = false);

        bool CanHandle(string message);
    }
}