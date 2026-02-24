namespace LogoSync.Core.Entities
{
    public class SyncQueueItem
    {
        public long Id { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string OperationType { get; set; }
        public string Payload { get; set; }
        public int RetryCount { get; set; }
    }

    public enum SyncStatus
    {
        Pending = 0,
        Processing = 1,
        Success = 2,
        Failed = 3
    }
}