using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Ship loadout snapshot as recorded in the CombatStartedEvent for replay.
/// Contains everything a replay client needs to render the ship's starting state.
/// </summary>
public class ShipLoadoutRecord
{
    public string ShipId { get; set; } = string.Empty;
    /// <summary>0 = attacker, 1 = defender</summary>
    public int FleetSide { get; set; }
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double MaxSpeed { get; set; }
    public double MaxAcceleration { get; set; }
    public double StructurePoints { get; set; }
    public double ShieldEffectiveness { get; set; }
    public double ArmorEffectiveness { get; set; }
    public double Mass { get; set; }
    public double ComputeCapacity { get; set; }

    /// <summary>Module loadout for replay visualization (module condition tracking).</summary>
    public List<ModuleLoadoutRecord> Modules { get; set; } = new();

    public byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        if (!string.IsNullOrEmpty(ShipId)) { output.WriteTag(1, WireFormat.WireType.LengthDelimited); output.WriteString(ShipId); }
        if (FleetSide != 0) { output.WriteTag(2, WireFormat.WireType.Varint); output.WriteInt32(FleetSide); }
        if (StartX != 0.0) { output.WriteTag(3, WireFormat.WireType.Fixed64); output.WriteDouble(StartX); }
        if (StartY != 0.0) { output.WriteTag(4, WireFormat.WireType.Fixed64); output.WriteDouble(StartY); }
        if (MaxSpeed != 0.0) { output.WriteTag(5, WireFormat.WireType.Fixed64); output.WriteDouble(MaxSpeed); }
        if (MaxAcceleration != 0.0) { output.WriteTag(6, WireFormat.WireType.Fixed64); output.WriteDouble(MaxAcceleration); }
        if (StructurePoints != 0.0) { output.WriteTag(7, WireFormat.WireType.Fixed64); output.WriteDouble(StructurePoints); }
        if (ShieldEffectiveness != 0.0) { output.WriteTag(8, WireFormat.WireType.Fixed64); output.WriteDouble(ShieldEffectiveness); }
        if (ArmorEffectiveness != 0.0) { output.WriteTag(9, WireFormat.WireType.Fixed64); output.WriteDouble(ArmorEffectiveness); }
        if (Mass != 0.0) { output.WriteTag(10, WireFormat.WireType.Fixed64); output.WriteDouble(Mass); }
        if (ComputeCapacity != 0.0) { output.WriteTag(11, WireFormat.WireType.Fixed64); output.WriteDouble(ComputeCapacity); }
        foreach (var module in Modules)
        {
            var moduleData = module.Serialize();
            output.WriteTag(12, WireFormat.WireType.LengthDelimited);
            output.WriteBytes(ByteString.CopyFrom(moduleData));
        }

        output.Flush();
        return memoryStream.ToArray();
    }

    public static ShipLoadoutRecord Deserialize(byte[] data)
    {
        var result = new ShipLoadoutRecord();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.ShipId = input.ReadString(); break;
                case 2: result.FleetSide = input.ReadInt32(); break;
                case 3: result.StartX = input.ReadDouble(); break;
                case 4: result.StartY = input.ReadDouble(); break;
                case 5: result.MaxSpeed = input.ReadDouble(); break;
                case 6: result.MaxAcceleration = input.ReadDouble(); break;
                case 7: result.StructurePoints = input.ReadDouble(); break;
                case 8: result.ShieldEffectiveness = input.ReadDouble(); break;
                case 9: result.ArmorEffectiveness = input.ReadDouble(); break;
                case 10: result.Mass = input.ReadDouble(); break;
                case 11: result.ComputeCapacity = input.ReadDouble(); break;
                case 12:
                    var moduleData = input.ReadBytes().ToByteArray();
                    result.Modules.Add(ModuleLoadoutRecord.Deserialize(moduleData));
                    break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
