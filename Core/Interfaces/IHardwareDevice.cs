using System;

namespace HondaTuner.Core.Interfaces
{
    /// <summary>
    /// Tüm donanım aygıtları için temel arayüz.
    /// </summary>
    public interface IHardwareDevice
    {
        string DeviceName { get; }
        ConnectionState State { get; }
        event EventHandler<ConnectionStateChangedEventArgs> StateChanged;
        void Connect();
        void Disconnect();
    }

    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
        TimedOut
    }

    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionState OldState { get; set; }
        public ConnectionState NewState { get; set; }
        public string Message { get; set; }
    }
}
