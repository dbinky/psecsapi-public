using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Module snapshot as recorded in ShipLoadoutRecord for replay.
/// Contains the module name, primary capability type, and starting condition.
/// </summary>
public class ModuleLoadoutRecord
{
    public string ModuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>Primary capability type as string (e.g., "EnergyResistance", "Speed", "EnergyDamage").</summary>
    public string Capability { get; set; } = string.Empty;
    public double Condition { get; set; } = 100.0;

    public byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        if (!string.IsNullOrEmpty(ModuleId)) { output.WriteTag(1, WireFormat.WireType.LengthDelimited); output.WriteString(ModuleId); }
        if (!string.IsNullOrEmpty(Name)) { output.WriteTag(2, WireFormat.WireType.LengthDelimited); output.WriteString(Name); }
        if (!string.IsNullOrEmpty(Capability)) { output.WriteTag(3, WireFormat.WireType.LengthDelimited); output.WriteString(Capability); }
        if (Condition != 0.0) { output.WriteTag(4, WireFormat.WireType.Fixed64); output.WriteDouble(Condition); }

        output.Flush();
        return memoryStream.ToArray();
    }

    public static ModuleLoadoutRecord Deserialize(byte[] data)
    {
        var result = new ModuleLoadoutRecord();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.ModuleId = input.ReadString(); break;
                case 2: result.Name = input.ReadString(); break;
                case 3: result.Capability = input.ReadString(); break;
                case 4: result.Condition = input.ReadDouble(); break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
