using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Records a weapon hit (energy hitscan or kinetic projectile impact).
/// Includes the full damage pipeline breakdown: shield absorbed, armor ablated,
/// structure damage, and which module was hit with its condition damage.
/// </summary>
public class ProjectileHitEvent : CombatEvent
{
    public override CombatEventType EventType => CombatEventType.ProjectileHit;

    /// <summary>For kinetic: the projectile ID. For energy: the weapon ID used as beam identifier.</summary>
    public string ProjectileId { get; set; } = string.Empty;
    public string TargetShipId { get; set; } = string.Empty;
    public double DamageDealt { get; set; }
    public double ShieldAbsorbed { get; set; }
    public double ArmorAblated { get; set; }
    public double StructureDamage { get; set; }
    /// <summary>ID of the module that was hit. Empty string if no module was hit.</summary>
    public string ModuleHitId { get; set; } = string.Empty;
    public double ModuleConditionDamage { get; set; }
    public int Tick { get; set; }
    /// <summary>Name of the module that was hit. Used by the visualizer for module condition tracking.</summary>
    public string ModuleHitName { get; set; } = string.Empty;
    /// <summary>Module condition after the hit (0-100). Used by the visualizer for status display.</summary>
    public double ModuleConditionAfter { get; set; }

    public override byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        WriteString(output, 1, ProjectileId);
        WriteString(output, 2, TargetShipId);
        WriteDouble(output, 3, DamageDealt);
        WriteDouble(output, 4, ShieldAbsorbed);
        WriteDouble(output, 5, ArmorAblated);
        WriteDouble(output, 6, StructureDamage);
        WriteString(output, 7, ModuleHitId);
        WriteDouble(output, 8, ModuleConditionDamage);
        WriteInt32(output, 9, Tick);
        WriteString(output, 10, ModuleHitName);
        WriteDouble(output, 11, ModuleConditionAfter);

        output.Flush();
        return memoryStream.ToArray();
    }

    public static ProjectileHitEvent Deserialize(byte[] data)
    {
        var result = new ProjectileHitEvent();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.ProjectileId = input.ReadString(); break;
                case 2: result.TargetShipId = input.ReadString(); break;
                case 3: result.DamageDealt = input.ReadDouble(); break;
                case 4: result.ShieldAbsorbed = input.ReadDouble(); break;
                case 5: result.ArmorAblated = input.ReadDouble(); break;
                case 6: result.StructureDamage = input.ReadDouble(); break;
                case 7: result.ModuleHitId = input.ReadString(); break;
                case 8: result.ModuleConditionDamage = input.ReadDouble(); break;
                case 9: result.Tick = input.ReadInt32(); break;
                case 10: result.ModuleHitName = input.ReadString(); break;
                case 11: result.ModuleConditionAfter = input.ReadDouble(); break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
