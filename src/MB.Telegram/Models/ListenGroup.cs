using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace MB.Telegram.Models
{
    public class ListenGroup : TableEntity
    {
        public static string GetId(ChatServices service, string serviceId)
        {
            switch (service)
            {
                case ChatServices.Telegram:
                    return $"{Prefix.Telegram}-{serviceId}";
                default: throw new NotSupportedException();
            }
        }

        public ChatServices Service { get; set; }
        public string ServiceId { get; set; } // Channel/Group they are in

        public string Id
        {
            get => RowKey;
            set
            {
                RowKey = value;
                PartitionKey = GetPartitionKey(value);
                Service = value.Split("-")[0] switch
                {
                    Prefix.Telegram => ChatServices.Telegram,
                    _ => throw new NotSupportedException(),
                };
                ServiceId = value.Split("-")[1];
            }
        }
        public string OwnerMBUserId { get; set; }
        public string OwnerMBDisplayName { get; set; }

        public string OwnerSpotifyUserId { get; set; }
        public string SpotifyPlaylistId { get; set; }
        public string SpotifyPlaylistDisplayName { get; set; }

        public override string ToString()
        {
            return $"{SpotifyPlaylistDisplayName} @ {OwnerMBDisplayName} ({Id})";
        }

        public static string GetPartitionKey(string id)
        {
            return id.Length > 5 ? id.Substring(id.Length - 5) : id;
        }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset LastListened { get; set; }

        public string[] ActiveListenerIds { get; set; }
    }
}