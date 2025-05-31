namespace ProtoKey;

public class SetRequest
{
    public string? Key { get; set; }
    public int? Value { get; set; }
}

public class GetRequest
{
    public string? Key { get; set; }
}
