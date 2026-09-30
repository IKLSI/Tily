using System.Text.Json.Serialization;

namespace Tily.Core.StatusLog;

[JsonConverter(typeof(JsonStringEnumConverter<StatusLogLevel>))]
public enum StatusLogLevel
{
    [JsonStringEnumMemberName("info")] Info,
    [JsonStringEnumMemberName("warning")] Warning,
    [JsonStringEnumMemberName("error")] Error
}

public sealed record StatusLogEntryModel(DateTimeOffset At, StatusLogLevel Level, string Text);

public sealed class StatusLogFileModel
{
    public List<StatusLogEntryModel>? Entries { get; set; }
}
