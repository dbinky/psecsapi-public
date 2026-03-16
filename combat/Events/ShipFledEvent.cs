using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Records a ship fleeing the combat grid (crossing the boundary).
/// </summary>
public class ShipFledEvent : CombatEvent
{
    public override CombatEventType EventType => CombatEventType.ShipFled;

    public string ShipId { get; set; } = string.Empty;
    public double ExitX { get; set; }
    public double ExitY { get; set; }
    public int Tick { get; set; }

    public override byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        WriteString(output, 1, ShipId);
        WriteDouble(output, 2, ExitX);
        WriteDouble(output, 3, ExitY);
        WriteInt32(output, 4, Tick);

        output.Flush();
        return memoryStream.ToArray();
    }

    public static ShipFledEvent Deserialize(byte[] data)
    {
        var result = new ShipFledEvent();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.ShipId = input.ReadString(); break;
                case 2: result.ExitX = input.ReadDouble(); break;
                case 3: result.ExitY = input.ReadDouble(); break;
                case 4: result.Tick = input.ReadInt32(); break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
