using System;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace MB.Telegram.Models
{
    public class User : TableEntity
    {
        public ChatServices Service { get; set; }
        public string ServiceId {get;set;}
        public DateTimeOffset LastSeen { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SpotifyScopes { get; set; }
        public string Id
        {
            get => RowKey;
            set
            {
                RowKey = value;
                PartitionKey = GetPartitionKey(value);
            }
        }
        public string SpotifyId { get; set; }

        public override string ToString()
        {
            return $"{UserName ?? FullName} ({Id})";
        }

        public string DisplayName => FullName ?? $"@{UserName}";

        public static string GetPartitionKey(string id)
        {
            return id.Length > 5 ? id.Substring(id.Length - 5) : id;
        }
    }
}