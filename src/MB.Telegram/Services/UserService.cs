
using System;
using System.Threading.Tasks;
using MB.Telegram.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MB.Telegram.Services
{
    public interface IUserService
    {
        Task CreateUser(MBUser user);
        Task SetLastSeenUser(MBUser user);
        Task<MBUser> GetUser(string id);
        Task UpdateUser(MBUser user);
        Task UpdateSpotifyDetails(MBUser user, string scopes, string spotifyId);
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

        public async Task CreateUser(MBUser user)
        {
            await table.ExecuteAsync(TableOperation.Insert(user));
        }

        public async Task SetLastSeenUser(MBUser user)
        {
            user.LastSeen = DateTimeOffset.UtcNow;

            await table.ExecuteAsync(TableOperation.InsertOrMerge(user));
        }

        public async Task<MBUser> GetUser(string id)
        {
            var result = await table.ExecuteAsync(TableOperation.Retrieve<MBUser>(MBUser.GetPartitionKey(id), id));

            return result.Result as MBUser;
        }

        public async Task UpdateSpotifyDetails(MBUser user, string scopes, string spotifyId)
        {
            user.SpotifyScopes = scopes;
            user.SpotifyId = spotifyId;
            user.LastSeen = DateTimeOffset.UtcNow;

            await table.ExecuteAsync(TableOperation.Merge(user));
        }

        public async Task UpdateUser(MBUser user)
        {
            await table.ExecuteAsync(TableOperation.Merge(user));
        }
    }
}   