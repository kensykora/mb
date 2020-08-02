using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace MB.Telegram.Models
{
    public class TelegramUser
    {
        public TelegramUser() { }
        public TelegramUser(IQueryCollection query)
        {
            Id = long.Parse(query["id"].First());
            FirstName = query["first_name"].First();
            LastName = query["last_name"].First();
            UserName = query["username"].First();
            PhotoUrl = query["photo_url"].First();
            AuthDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(query["auth_date"].First()));
        }

        //  id, first_name, last_name, username, photo_url, auth_date and hash;
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhotoUrl { get; set; }
        public string UserName { get; set; }
        public DateTimeOffset AuthDate { get; set; }

        public override string ToString()
        {
            return $"{FirstName} {LastName} / @{UserName} - ({Id})";
        }
    }
}