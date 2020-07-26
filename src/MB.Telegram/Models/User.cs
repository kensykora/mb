using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace MB.Telegram.Models
{
    public class User : TableEntity
    {
        public ChatServices Service { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ServiceId
        {
            get => RowKey;
            set
            {
                RowKey = value;
                PartitionKey = value.Length > 5 ? value.Substring(value.Length - 5) : value;
            }
        }
        public string SpotifyId { get; set; }
    }
}