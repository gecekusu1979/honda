using System;

namespace HondaTuner.Core.Rtp
{
    public static class RtpConfigValidator
    {
        public static void Validate(RtpConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config), "Configuration cannot be null.");

            if (config.RetryCount < 0 || config.RetryCount > 10)
                throw new ArgumentOutOfRangeException(nameof(config.RetryCount), "Retry count must be between 0 and 10.");

            if (config.WriteTimeoutMs < 10 || config.WriteTimeoutMs > 5000)
                throw new ArgumentOutOfRangeException(nameof(config.WriteTimeoutMs), "Write timeout ms must be between 10 and 5000.");

            if (config.PacketSize != 16 && config.PacketSize != 32 && config.PacketSize != 64 && config.PacketSize != 128 && config.PacketSize != 256)
                throw new ArgumentException("Packet size must be 16, 32, 64, 128, or 256 bytes.", nameof(config.PacketSize));

            if (config.SyncIntervalMs < 1 || config.SyncIntervalMs > 2000)
                throw new ArgumentOutOfRangeException(nameof(config.SyncIntervalMs), "Sync interval ms must be between 1 and 2000.");

            if (string.IsNullOrWhiteSpace(config.BatchingPolicy))
                throw new ArgumentException("Batching policy cannot be null or empty.", nameof(config.BatchingPolicy));

            if (config.BatchingPolicy != "None" && config.BatchingPolicy != "CoalesceConsecutive")
                throw new ArgumentException("Invalid batching policy. Allowed values: None, CoalesceConsecutive", nameof(config.BatchingPolicy));

            if (config.QueueLimit < 1 || config.QueueLimit > 50000)
                throw new ArgumentOutOfRangeException(nameof(config.QueueLimit), "Queue limit must be between 1 and 50000.");

            if (string.IsNullOrWhiteSpace(config.BackpressurePolicy))
                throw new ArgumentException("Backpressure policy cannot be null or empty.", nameof(config.BackpressurePolicy));

            if (config.BackpressurePolicy != "DropOldest" && config.BackpressurePolicy != "RejectNewest" && config.BackpressurePolicy != "BlockProducer")
                throw new ArgumentException("Invalid backpressure policy. Allowed values: DropOldest, RejectNewest, BlockProducer", nameof(config.BackpressurePolicy));
        }
    }
}
