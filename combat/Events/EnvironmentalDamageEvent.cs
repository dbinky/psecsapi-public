using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Records environmental damage to a ship (star heat, asteroid collision, black hole).
/// </summary>
public class EnvironmentalDamageEvent : CombatEvent
{
    public override CombatEventType EventType => CombatEventType.EnvironmentalDamage;

    public string ShipId { get; set; } = string.Empty;
    /// <summary>Source type: "Star", "Asteroid", "EventHorizon", "BlackHoleGravity".</summary>
    public string SourceType { get; set; } = string.Empty;
    public double Damage { get; set; }
    public int Tick { get; set; }

    public override byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        WriteString(output, 1, ShipId);
        WriteString(output, 2, SourceType);
        WriteDouble(output, 3, Damage);
        WriteInt32(output, 4, Tick);

        output.Flush();
        return memoryStream.ToArray();
    }

    public static EnvironmentalDamageEvent Deserialize(byte[] data)
    {
        var result = new EnvironmentalDamageEvent();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.ShipId = input.ReadString(); break;
                case 2: result.SourceType = input.ReadString(); break;
                case 3: result.Damage = input.ReadDouble(); break;
                case 4: result.Tick = input.ReadInt32(); break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
