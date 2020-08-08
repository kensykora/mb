using System;
using System.Threading.Tasks;
using MB.Telegram.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MB.Telegram.Services
{
    public interface IListenSessionService
    {
        Task CreateGroupAsync(ListenGroup group);
        Task<ListenGroup> GetGroupAsync(ChatServices service, string id);
        Task SetLastListened(ListenGroup group);
    }

    public class ListenSessionService : IListenSessionService
    {
        public const string TableName = "ListenGroups";
        private readonly ISpotifyService spotifyService;
        private readonly CloudTableClient client;
        private readonly CloudTable table;

        public ListenSessionService(ISpotifyService spotifyService, CloudTableClient client)
        {
            this.spotifyService = spotifyService;
            this.client = client;

            this.table = client.GetTableReference(TableName);
            table.CreateIfNotExistsAsync().Wait();
        }

        public async Task<ListenGroup> GetGroupAsync(ChatServices service, string id)
        {
            var result = await table.ExecuteAsync(TableOperation.Retrieve<ListenGroup>(ListenGroup.GetPartitionKey(id), id));

            return result.Result as ListenGroup;
        }

        public async Task CreateGroupAsync(ListenGroup group)
        {
            await table.ExecuteAsync(TableOperation.Insert(group));
        }

        public async Task SetLastListened(ListenGroup group)
        {
            group.LastListened = DateTimeOffset.UtcNow;

            await table.ExecuteAsync(TableOperation.Merge(group));
        }
    }
}