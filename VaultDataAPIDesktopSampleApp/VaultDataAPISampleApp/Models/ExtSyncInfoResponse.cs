using System;

namespace VaultDataAPISampleApp.Models
{
    public class ExtSyncInfoResponse
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public DateTime? CreateDateTime { get; set; }
        public string ParentCollectionName { get; set; }

        // UI-only fields for displaying full value in tooltip popup.
        public string RawValue { get; set; }
        public string ValuePopupContent { get; set; }
        public bool HasValuePopup { get; set; }
        public string DisplayValue { get; set; }
        public bool HasStatusBadge { get; set; }
        public string StatusBadgeText { get; set; }
    }
}
