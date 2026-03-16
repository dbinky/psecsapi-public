using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Records a ship's position and velocity at a specific tick.
/// Emitted on compute ticks, significant velocity changes, damage, or collisions.
/// Clients interpolate between ShipMovedEvents using the velocity vector.
/// </summary>
public class ShipMovedEvent : CombatEvent
{
    public override CombatEventType EventType => CombatEventType.ShipMoved;

    public string ShipId { get; set; } = string.Empty;
    public double NewX { get; set; }
    public double NewY { get; set; }
    public double VelX { get; set; }
    public double VelY { get; set; }
    public int Tick { get; set; }

    public override byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        WriteString(output, 1, ShipId);
        WriteDouble(output, 2, NewX);
        WriteDouble(output, 3, NewY);
        WriteDouble(output, 4, VelX);
        WriteDouble(output, 5, VelY);
        WriteInt32(output, 6, Tick);

        output.Flush();
        return memoryStream.ToArray();
    }

    public static ShipMovedEvent Deserialize(byte[] data)
    {
        var result = new ShipMovedEvent();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.ShipId = input.ReadString(); break;
                case 2: result.NewX = input.ReadDouble(); break;
                case 3: result.NewY = input.ReadDouble(); break;
                case 4: result.VelX = input.ReadDouble(); break;
                case 5: result.VelY = input.ReadDouble(); break;
                case 6: result.Tick = input.ReadInt32(); break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
