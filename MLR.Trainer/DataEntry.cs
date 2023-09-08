using Microsoft.ML.Data;

namespace MLR.Domain.DataRows;

public class DataEntry
{
    [KeyType(count : 58)]
    public uint Position { get; set; }

    [KeyType(count : 281)]
    public uint Skill { get; set; }
}