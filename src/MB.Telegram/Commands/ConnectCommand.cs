using System.Threading.Tasks;
using MB.Telegram.Models;
using Microsoft.Extensions.Logging;

namespace MB.Telegram.Commands
{
    public class ConnectCommand : BaseCommand
    {
        public override string CommandString => "/connect";

        public override Task Process(User user, string message, ILogger logger)
        {
            
            return Task.CompletedTask;
        }
    }
}