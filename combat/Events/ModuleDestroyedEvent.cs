using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Records a module being destroyed (condition reached 0%).
/// The module remains in its slot as dead weight but provides zero capability.
/// </summary>
public class ModuleDestroyedEvent : CombatEvent
{
    public override CombatEventType EventType => CombatEventType.ModuleDestroyed;

    public string ShipId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public int Tick { get; set; }
    /// <summary>Name of the destroyed module. Used by the visualizer for module condition tracking.</summary>
    public string ModuleName { get; set; } = string.Empty;

    public override byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        WriteString(output, 1, ShipId);
        WriteString(output, 2, ModuleId);
        WriteInt32(output, 3, Tick);
        WriteString(output, 4, ModuleName);

        output.Flush();
        return memoryStream.ToArray();
    }

    public static ModuleDestroyedEvent Deserialize(byte[] data)
    {
        var result = new ModuleDestroyedEvent();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.ShipId = input.ReadString(); break;
                case 2: result.ModuleId = input.ReadString(); break;
                case 3: result.Tick = input.ReadInt32(); break;
                case 4: result.ModuleName = input.ReadString(); break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
