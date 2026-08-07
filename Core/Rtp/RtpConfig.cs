namespace HondaTuner.Core.Rtp
{
    public class RtpConfig
    {
        public int RetryCount { get; set; }
        public int WriteTimeoutMs { get; set; }
        public int PacketSize { get; set; }
        public int SyncIntervalMs { get; set; }
        public string BatchingPolicy { get; set; }
        public int QueueLimit { get; set; }
        public string BackpressurePolicy { get; set; }
    }
}
