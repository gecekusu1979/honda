using System;
using System.Text.Json.Serialization;

namespace HondaTuner.Core.AutoTune
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TuneDecisionStatus
    {
        Suggested,
        PendingApproval,
        Approved,
        Applied,
        Rejected
    }
}
