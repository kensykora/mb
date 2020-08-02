using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace MB.Telegram.Models
{
    public class MBUser : TableEntity
    {
        public ChatServices Service { get; set; }
        public string ServiceId { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset? ChatServiceAuthDate { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhotoUrl { get; set; }
        public string SpotifyScopes { get; set; }

        [IgnoreProperty]
        public List<string> SpotifyScopesList
        {
            get => SpotifyScopes?.Split(' ').ToList();
            set => string.Join(' ', value ?? new List<string>());
        }
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