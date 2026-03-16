using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Records a kinetic projectile being spawned. Energy weapons do not create projectiles
/// (they are instant hitscan and go directly to ProjectileHitEvent or miss).
/// </summary>
public class ProjectileCreatedEvent : CombatEvent
{
    public override CombatEventType EventType => CombatEventType.ProjectileCreated;

    public string ProjectileId { get; set; } = string.Empty;
    public double OriginX { get; set; }
    public double OriginY { get; set; }
    public double VelX { get; set; }
    public double VelY { get; set; }
    /// <summary>DamageType as int: 0 = Energy, 1 = Kinetic (cast from DamageType enum).</summary>
    public int DamageType { get; set; }
    public double Damage { get; set; }
    public int Tick { get; set; }

    public override byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        WriteString(output, 1, ProjectileId);
        WriteDouble(output, 2, OriginX);
        WriteDouble(output, 3, OriginY);
        WriteDouble(output, 4, VelX);
        WriteDouble(output, 5, VelY);
        WriteInt32(output, 6, DamageType);
        WriteDouble(output, 7, Damage);
        WriteInt32(output, 8, Tick);

        output.Flush();
        return memoryStream.ToArray();
    }

    public static ProjectileCreatedEvent Deserialize(byte[] data)
    {
        var result = new ProjectileCreatedEvent();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.ProjectileId = input.ReadString(); break;
                case 2: result.OriginX = input.ReadDouble(); break;
                case 3: result.OriginY = input.ReadDouble(); break;
                case 4: result.VelX = input.ReadDouble(); break;
                case 5: result.VelY = input.ReadDouble(); break;
                case 6: result.DamageType = input.ReadInt32(); break;
                case 7: result.Damage = input.ReadDouble(); break;
                case 8: result.Tick = input.ReadInt32(); break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
