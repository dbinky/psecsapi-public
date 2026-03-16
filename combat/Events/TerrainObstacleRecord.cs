using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Terrain obstacle as recorded in the CombatStartedEvent for replay.
/// Serialized as an embedded message within the event stream.
/// </summary>
public class TerrainObstacleRecord
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Radius { get; set; }
    public string ObstacleType { get; set; } = string.Empty;

    public byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        if (X != 0.0) { output.WriteTag(1, WireFormat.WireType.Fixed64); output.WriteDouble(X); }
        if (Y != 0.0) { output.WriteTag(2, WireFormat.WireType.Fixed64); output.WriteDouble(Y); }
        if (Radius != 0.0) { output.WriteTag(3, WireFormat.WireType.Fixed64); output.WriteDouble(Radius); }
        if (!string.IsNullOrEmpty(ObstacleType)) { output.WriteTag(4, WireFormat.WireType.LengthDelimited); output.WriteString(ObstacleType); }

        output.Flush();
        return memoryStream.ToArray();
    }

    public static TerrainObstacleRecord Deserialize(byte[] data)
    {
        var result = new TerrainObstacleRecord();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.X = input.ReadDouble(); break;
                case 2: result.Y = input.ReadDouble(); break;
                case 3: result.Radius = input.ReadDouble(); break;
                case 4: result.ObstacleType = input.ReadString(); break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
