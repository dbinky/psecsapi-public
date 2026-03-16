using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Records a ship being destroyed (StructurePoints reached 0).
/// Includes position at death for cargo drop placement and the ID of the ship that landed the killing blow.
/// </summary>
public class ShipDestroyedEvent : CombatEvent
{
    public override CombatEventType EventType => CombatEventType.ShipDestroyed;

    public string ShipId { get; set; } = string.Empty;
    public string DestroyerShipId { get; set; } = string.Empty;
    public double PosX { get; set; }
    public double PosY { get; set; }
    /// <summary>IDs of cargo items dropped at the destruction position.</summary>
    public List<string> CargoDropped { get; set; } = new();
    public int Tick { get; set; }

    public override byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        WriteString(output, 1, ShipId);
        WriteString(output, 2, DestroyerShipId);
        WriteDouble(output, 3, PosX);
        WriteDouble(output, 4, PosY);
        WriteRepeatedString(output, 5, CargoDropped);
        WriteInt32(output, 6, Tick);

        output.Flush();
        return memoryStream.ToArray();
    }

    public static ShipDestroyedEvent Deserialize(byte[] data)
    {
        var result = new ShipDestroyedEvent();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.ShipId = input.ReadString(); break;
                case 2: result.DestroyerShipId = input.ReadString(); break;
                case 3: result.PosX = input.ReadDouble(); break;
                case 4: result.PosY = input.ReadDouble(); break;
                case 5: result.CargoDropped.Add(input.ReadString()); break;
                case 6: result.Tick = input.ReadInt32(); break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
