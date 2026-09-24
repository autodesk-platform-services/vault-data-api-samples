namespace VaultDataAPISampleApp.Models
{
    public class ItemResponse
    {
        public required string Id { get; set; }
        public required string Url { get; set; }
    }

    public class ItemVersionResponse
    {
        public string? Id { get; set; }
        public string? Number { get; set; }
        public string? Revision { get; set; }
        public string? Title { get; set; }
        public string? State { get; set; }
        public string? Category { get; set; }
        public string? EntityType { get; set; }
        public ItemResponse? Item { get; set; }
        public string? Url { get; set; }
    }
}
