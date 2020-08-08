namespace MB.Telegram
{
    public class Config
    {
        public string TelegramApiKey { get; set; }
        public string TelegramBotUsername { get; set; }
        public bool UseLocalStorage { get; set; }
        public string StorageAccountName { get; set; }
        public string StorageAccountKey { get; set; }
        public string SpotifyClientId { get; set; }
        public string SpotifyClientSecret { get; set; }
        public string KeyVaultName { get; set; }
        public string BaseUrl { get; set; }
    }
}