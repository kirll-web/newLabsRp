using ProtoKey.Core;

namespace ProtoKey.Services;

using System.Text.Json;
using System.Threading.Channels;


public class PersistenceWorker : BackgroundService
{
    private readonly Channel<Command> _persistChannel;
    private readonly string _dataPath = "ProtoKey.data";
    private readonly List<Command> _buffer = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));

    public PersistenceWorker(IPersistChannel persistChannel)
    {
        _persistChannel = persistChannel.Channel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            while (_persistChannel.Reader.TryRead(out var cmd))
            {
                _buffer.Add(cmd);
            }

            await _timer.WaitForNextTickAsync(stoppingToken);
            

            if (_buffer.Count > 0)
            {
                try
                {
                    await using var fs = new FileStream(_dataPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                    foreach (var cmd in _buffer)
                    {
                        try
                        {
                            Console.WriteLine($"[INFO] Saving to disk");
                            var json = JsonSerializer.Serialize(new JsonCommand
                            {
                                Type = cmd.Type,
                                Key = cmd.Key,
                                Value = cmd.Value
                            });
                            
                            Console.WriteLine($"[INFO] Saving to disk: {json}");
                            await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes(json + "\n"), stoppingToken);
                        } catch { Console.WriteLine("Error writing to disk");}
                    }
                    _buffer.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Saving to disk failed: {ex.Message}");
                }
            }
        }
    }
    
    public static void ReplayFromDisk(Dictionary<string, int> store)
    {
        const string path = "ProtoKey.data";
        if (!File.Exists(path)) return;

        foreach (var line in File.ReadLines(path))
        {
            var cmd = JsonSerializer.Deserialize<JsonCommand>(line);
            if (cmd is null) continue;

            if (cmd.Type == CommandType.Set && cmd.Value.HasValue)
                store[cmd.Key] = cmd.Value.Value;
        }
    }
}
