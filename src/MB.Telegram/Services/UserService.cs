
using System.Threading.Tasks;
using MB.Telegram.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MB.Telegram.Services
{
    public interface IUserService
    {
        Task CreateOrSetLastSeenUser(User user);
    }

    public class UserService : IUserService
    {
        private const string TableName = "users";
        private readonly CloudTableClient client;
        private readonly CloudTable table;

        public UserService(CloudTableClient client)
        {
            this.client = client ?? throw new System.ArgumentNullException(nameof(client));
            this.table = client.GetTableReference(TableName);
            table.CreateIfNotExistsAsync();
        }

        public async Task CreateOrSetLastSeenUser(User user)
        {
            await table.ExecuteAsync(TableOperation.InsertOrMerge(user));
        }
    }
}   