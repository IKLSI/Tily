using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tily.Core.Session;

namespace Tily.Core.Agents;

public static class AgentHookInput
{
    public const int TranscriptTailLines = 200;

    private const string AssistantMarker = "\"type\":\"assistant\"";
    private const int ChunkSize = 64 * 1024;
    private static readonly string[] DetailProperties = ["command", "file_path", "notebook_path", "url", "query", "pattern", "description"];
    private static readonly JsonSerializerOptions CompactOptions = new() { Encoder = SessionRepository.JsonOptions.Encoder };

    public static string? Text(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    public static string? ToolDetail(JsonNode? toolInput)
    {
        if (toolInput is null)
        {
            return null;
        }

        if (toolInput is JsonObject input)
        {
            if (input["questions"] is JsonArray { Count: > 0 } questions)
            {
                return ScalarText(questions[0]?["question"]) ?? string.Empty;
            }

            foreach (var name in DetailProperties)
            {
                var text = ScalarText(input[name]);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        return toolInput.ToJsonString(CompactOptions);
    }

    public static string? LastAssistantText(JsonObject hook)
    {
        var last = Text(hook["last_assistant_message"]);
        if (!string.IsNullOrWhiteSpace(last))
        {
            return last;
        }

        var transcript = Text(hook["transcript_path"]);
        if (string.IsNullOrWhiteSpace(transcript) || !File.Exists(transcript))
        {
            return null;
        }

        IReadOnlyList<string> lines;
        try
        {
            lines = TailLines(transcript, TranscriptTailLines);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        foreach (var line in lines.Where(line => line.Contains(AssistantMarker, StringComparison.Ordinal)).Reverse())
        {
            var texts = AssistantTexts(line);
            if (texts.Count > 0)
            {
                return string.Join(' ', texts);
            }
        }

        return null;
    }

    private static List<string> AssistantTexts(string line)
    {
        JsonObject? entry;
        try
        {
            entry = JsonNode.Parse(line) as JsonObject;
        }
        catch (JsonException)
        {
            return [];
        }

        if (entry is null || Text(entry["type"]) != "assistant" || entry["isSidechain"] is JsonValue sidechain && sidechain.TryGetValue<bool>(out var isSidechain) && isSidechain)
        {
            return [];
        }

        return entry["message"]?["content"] is JsonArray content
            ? content.OfType<JsonObject>().Where(part => Text(part["type"]) == "text").Select(part => Text(part["text"])).Where(text => !string.IsNullOrEmpty(text)).Select(text => text!).ToList()
            : [];
    }

    private static string? ScalarText(JsonNode? node) => node switch
    {
        null => null,
        JsonValue value when value.TryGetValue<string>(out var text) => text,
        JsonValue value => value.ToJsonString(),
        _ => node.ToJsonString(CompactOptions)
    };

    public static IReadOnlyList<string> TailLines(string path, int count)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var position = stream.Length;
        var collected = new List<byte[]>();
        var newlines = 0;
        while (position > 0 && newlines <= count)
        {
            var size = (int)Math.Min(ChunkSize, position);
            position -= size;
            var chunk = new byte[size];
            stream.Position = position;
            stream.ReadExactly(chunk);
            newlines += chunk.Count(value => value == (byte)'\n');
            collected.Insert(0, chunk);
        }

        var text = Encoding.UTF8.GetString(collected.SelectMany(chunk => chunk).ToArray());
        var lines = text.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        if (lines.Count > 0 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        if (position > 0 && lines.Count > 0)
        {
            lines.RemoveAt(0);
        }

        return lines.Count <= count ? lines : lines.GetRange(lines.Count - count, count);
    }
}
