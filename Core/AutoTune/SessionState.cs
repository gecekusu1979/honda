using System;
using System.Text.Json.Serialization;

namespace HondaTuner.Core.AutoTune
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SessionState
    {
        Initializing,
        Running,
        Paused,
        Stopped,
        Error
    }
}
