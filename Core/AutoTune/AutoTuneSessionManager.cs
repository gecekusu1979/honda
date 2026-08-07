using System;
using System.Collections.Concurrent;

namespace HondaTuner.Core.AutoTune
{
    public class AutoTuneSessionManager : IAutoTuneSessionManager
    {
        private class SessionLockInfo
        {
            public string SessionId { get; set; }
            public string EcuIdentifier { get; set; }
            public string User { get; set; }
            public DateTime Timestamp { get; set; }
            public AutoTuneOperatingMode Mode { get; set; }
            public string State { get; set; }
        }

        private readonly ConcurrentDictionary<string, SessionLockInfo> _activeSessions = new ConcurrentDictionary<string, SessionLockInfo>();

        public bool AcquireSessionLock(string ecuid, string userId, AutoTuneOperatingMode mode, string state, out string existingSessionOwner)
        {
            existingSessionOwner = null;
            if (string.IsNullOrEmpty(ecuid)) return false;

            var lockInfo = new SessionLockInfo
            {
                SessionId = Guid.NewGuid().ToString(),
                EcuIdentifier = ecuid,
                User = userId ?? "Unknown",
                Timestamp = DateTime.Now,
                Mode = mode,
                State = state ?? "Created"
            };

            if (_activeSessions.TryAdd(ecuid, lockInfo))
            {
                return true;
            }

            if (_activeSessions.TryGetValue(ecuid, out var active))
            {
                existingSessionOwner = active.User;
            }
            return false;
        }

        public void ReleaseSessionLock(string ecuid)
        {
            if (string.IsNullOrEmpty(ecuid)) return;
            _activeSessions.TryRemove(ecuid, out _);
        }

        public bool IsECULocked(string ecuid, out string userId)
        {
            userId = null;
            if (string.IsNullOrEmpty(ecuid)) return false;

            if (_activeSessions.TryGetValue(ecuid, out var active))
            {
                userId = active.User;
                return true;
            }
            return false;
        }

        public void UpdateSessionState(string ecuid, string state)
        {
            if (string.IsNullOrEmpty(ecuid) || string.IsNullOrEmpty(state)) return;
            if (_activeSessions.TryGetValue(ecuid, out var active))
            {
                active.State = state;
            }
        }
    }
}
