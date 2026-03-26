namespace VaultDataAPISampleApp.Models
{
    public class ItemMasterResponse
    {
        public string Id { get; set; }
        public string Url { get; set; }
    }

    public class ItemVersionResponse
    {
        public string Id { get; set; }
        public string Number { get; set; }
        public string Revision { get; set; }
        public string Title { get; set; }
        public string State { get; set; }
        public string Category { get; set; }
        public string EntityType { get; set; }
        public ItemMasterResponse Item { get; set; }
        public string Url { get; set; }
    }
}
