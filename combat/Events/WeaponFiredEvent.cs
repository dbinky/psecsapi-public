using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Records a weapon being fired. Includes direction vector and target info.
/// TargetId is the enemy ship ID for targeted fire; TargetX/TargetY for coordinate fire.
/// For energy weapons this is the hitscan ray direction; for kinetic, the projectile launch direction.
/// </summary>
public class WeaponFiredEvent : CombatEvent
{
    public override CombatEventType EventType => CombatEventType.WeaponFired;

    public string ShipId { get; set; } = string.Empty;
    public string WeaponId { get; set; } = string.Empty;
    public double DirX { get; set; }
    public double DirY { get; set; }
    /// <summary>Target ship ID for targeted fire. Empty string for coordinate fire.</summary>
    public string TargetId { get; set; } = string.Empty;
    public double TargetX { get; set; }
    public double TargetY { get; set; }
    public int Tick { get; set; }

    public override byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        WriteString(output, 1, ShipId);
        WriteString(output, 2, WeaponId);
        WriteDouble(output, 3, DirX);
        WriteDouble(output, 4, DirY);
        WriteString(output, 5, TargetId);
        WriteDouble(output, 6, TargetX);
        WriteDouble(output, 7, TargetY);
        WriteInt32(output, 8, Tick);

        output.Flush();
        return memoryStream.ToArray();
    }

    public static WeaponFiredEvent Deserialize(byte[] data)
    {
        var result = new WeaponFiredEvent();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.ShipId = input.ReadString(); break;
                case 2: result.WeaponId = input.ReadString(); break;
                case 3: result.DirX = input.ReadDouble(); break;
                case 4: result.DirY = input.ReadDouble(); break;
                case 5: result.TargetId = input.ReadString(); break;
                case 6: result.TargetX = input.ReadDouble(); break;
                case 7: result.TargetY = input.ReadDouble(); break;
                case 8: result.Tick = input.ReadInt32(); break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
