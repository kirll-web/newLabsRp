using System.Net.Http.Json;

class Program
{
    private const string ProtoKeyHost = "https://localhost:44355";

    static async Task Main()
    {
        using var http = new HttpClient();

        Console.WriteLine("ProtoCli started. Type 'exit' to quit.");

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Trim().ToLowerInvariant() == "exit")
                break;

            var args = ParseArgs(input);

            if (args.Length == 0)
                continue;

            var command = args[0].ToLowerInvariant();

            try
            {
                switch (command)
                {
                    case "set":
                        if (args.Length != 3)
                        {
                            Console.WriteLine("Usage: set <key> <value>");
                            continue;
                        }

                        var key = args[1];
                        if (!int.TryParse(args[2], out var value))
                        {
                            Console.WriteLine("Value must be a 32-bit integer.");
                            continue;
                        }

                        var setResponse = await http.PostAsJsonAsync($"{ProtoKeyHost}/set", new { key, value });

                        if (setResponse.IsSuccessStatusCode)
                            Console.WriteLine("OK");
                        else
                            Console.WriteLine($"{(int)setResponse.StatusCode} {setResponse.ReasonPhrase}");
                        break;

                    case "get":
                        if (args.Length != 2)
                        {
                            Console.WriteLine("Usage: get <key>");
                            continue;
                        }

                        var getKey = Uri.EscapeDataString(args[1]);
                        var getResponse = await http.GetAsync($"{ProtoKeyHost}/get?key={getKey}");

                        if (getResponse.IsSuccessStatusCode)
                        {
                            var result = await getResponse.Content.ReadAsStringAsync();
                            Console.WriteLine(result);
                        }
                        else
                        {
                            Console.WriteLine($"{(int)getResponse.StatusCode} {getResponse.ReasonPhrase}");
                        }
                        break;

                    case "keys":
                        if (args.Length > 2)
                        {
                            Console.WriteLine("Usage: keys <prefix>");
                            continue;
                        }

                        var prefix = "";
                        try
                        {
                            prefix = Uri.EscapeDataString(args[1]);
                        } catch (Exception ex) {}

                        var url =  $"{ProtoKeyHost}/keys" + (prefix == "" ? prefix : $"?prefix={prefix}");
                        var keysResponse = await http.GetAsync(url);

                        if (keysResponse.IsSuccessStatusCode)
                        {
                            var keys = await keysResponse.Content.ReadFromJsonAsync<string[]>();
                            if (keys!.Length == 0)
                                Console.WriteLine("(no keys)");
                            else
                                foreach (var k in keys)
                                    Console.WriteLine(k);
                        }
                        else
                        {
                            Console.WriteLine($"{(int)keysResponse.StatusCode} {keysResponse.ReasonPhrase}");
                        }

                        break;

                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] {ex.Message}");
            }
        }
    }

    // Упрощённый парсер с поддержкой кавычек
    static string[] ParseArgs(string input)
    {
        var inQuotes = false;
        var current = new System.Text.StringBuilder();
        var result = new List<string>();

        foreach (var ch in input)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
            result.Add(current.ToString());

        return result.ToArray();
    }
}
