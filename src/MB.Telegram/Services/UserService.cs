
using System;
using System.Threading.Tasks;
using MB.Telegram.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MB.Telegram.Services
{
    public interface IUserService
    {
        Task CreateUser(User user);
        Task SetLastSeenUser(User user);
        Task<User> GetUser(string id);
        Task UpdateSpotifyDetails(User user, string scopes, string spotifyId);
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

        public async Task CreateUser(User user)
        {
            await table.ExecuteAsync(TableOperation.Insert(user));
        }

        public async Task SetLastSeenUser(User user)
        {
            user.LastSeen = DateTimeOffset.UtcNow;

            await table.ExecuteAsync(TableOperation.InsertOrMerge(user));
        }

        public async Task<User> GetUser(string id)
        {
            var result = await table.ExecuteAsync(TableOperation.Retrieve<User>(User.GetPartitionKey(id), id));

            return result.Result as User;
        }

        public async Task UpdateSpotifyDetails(User user, string scopes, string spotifyId)
        {
            user.SpotifyScopes = scopes;
            user.SpotifyId = spotifyId;
            user.LastSeen = DateTimeOffset.UtcNow;

            await table.ExecuteAsync(TableOperation.Merge(user));
        }
    }
}   