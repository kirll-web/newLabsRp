using System.Threading.Channels;
using ProtoKey.Core;
using ProtoKey.Services;
using Command = ProtoKey.Core.Command;

public class KeyValueWorker : BackgroundService
{
    private readonly Channel<Command> _channel;
    private readonly Dictionary<string, int> _store = new();

    public KeyValueWorker(Channel<Command> channel)
    {
        _channel = channel;
    }

    public KeyValueWorker(ICommandChannel persistChannel)
    {
        _channel = persistChannel.Channel;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PersistenceWorker.ReplayFromDisk(_store);
        while (await _channel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (_channel.Reader.TryRead(out var command))
            {
                try
                {
                    switch (command.Type)
                    {
                        case CommandType.Set:
                            _store[command.Key] = command.Value!.Value;
                            command.Completion.SetResult(true);
                            break;

                        case CommandType.Get:
                            if (_store.TryGetValue(command.Key, out var value))
                                command.Completion.SetResult(value);
                            else
                                command.Completion.SetResult(0); // по заданию — если нет ключа, вернуть 0
                            break;

                        case CommandType.Keys:
                            var result = _store.Keys
                                .Where(k => k.StartsWith(command.Prefix!))
                                .ToArray();
                            command.Completion.SetResult(result);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    command.Completion.SetException(ex);
                }
            }
        }
    }
}
