using Orleans;

namespace psecsapi.Combat;

[GenerateSerializer]
public class DamageResult
{
    [Id(0)] public double RawDamage { get; set; }
    [Id(1)] public double ShieldAbsorbed { get; set; }
    [Id(2)] public double ArmorAblated { get; set; }
    [Id(3)] public double StructureDamage { get; set; }
    [Id(4)] public Guid? ModuleHitId { get; set; }
    [Id(5)] public double ModuleConditionDamage { get; set; }
    [Id(6)] public DamageType Type { get; set; }
    [Id(7)] public ModuleHitResult? ModuleHit { get; set; }
    [Id(8)] public double ShieldEffectiveness { get; set; }
    [Id(9)] public double ArmorEffectiveness { get; set; }
    [Id(10)] public double PowerDeliveryFactor { get; set; }
}
