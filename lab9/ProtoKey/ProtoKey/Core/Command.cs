using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace ProtoKey.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommandType
{
    [EnumMember(Value = "set")]
    Set,

    [EnumMember(Value = "get")]
    Get,

    [EnumMember(Value = "keys")]
    Keys
}

public class Command
{
    public CommandType Type { get; init; }

    public string Key { get; init; } = string.Empty;

    public int? Value { get; init; }

    public string? Prefix { get; init; }

    public TaskCompletionSource<object> Completion { get; } = new();
}

public class JsonCommand
{
    public CommandType Type { get; init; }

    public string Key { get; init; } = string.Empty;

    public int? Value { get; init; }
}


public interface ICommandChannel
{
    Channel<Command> Channel { get; }
}

public class CommandChannelWrapper : ICommandChannel
{
    public Channel<Command> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<Command>();
}

public interface IPersistChannel
{
    Channel<Command> Channel { get; }
}

public class PersistChannelWrapper : IPersistChannel
{
    public Channel<Command> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<Command>();
}
