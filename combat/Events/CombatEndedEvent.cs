using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Final event in the replay stream. Records the combat outcome and surviving ships.
/// </summary>
public class CombatEndedEvent : CombatEvent
{
    public override CombatEventType EventType => CombatEventType.CombatEnded;

    /// <summary>CombatOutcome as int: 0=AttackerWon, 1=DefenderWon, 2=Draw, 3=TimedOut.</summary>
    public int Outcome { get; set; }
    /// <summary>Ship IDs that survived the combat.</summary>
    public List<string> SurvivingShips { get; set; } = new();
    public int TickCount { get; set; }
    public double DurationSeconds { get; set; }

    public override byte[] Serialize()
    {
        using var memoryStream = new MemoryStream();
        using var output = new CodedOutputStream(memoryStream);

        WriteInt32(output, 1, Outcome);
        WriteRepeatedString(output, 2, SurvivingShips);
        WriteInt32(output, 3, TickCount);
        WriteDouble(output, 4, DurationSeconds);

        output.Flush();
        return memoryStream.ToArray();
    }

    public static CombatEndedEvent Deserialize(byte[] data)
    {
        var result = new CombatEndedEvent();
        using var input = new CodedInputStream(data);

        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case 1: result.Outcome = input.ReadInt32(); break;
                case 2: result.SurvivingShips.Add(input.ReadString()); break;
                case 3: result.TickCount = input.ReadInt32(); break;
                case 4: result.DurationSeconds = input.ReadDouble(); break;
                default: input.SkipLastField(); break;
            }
        }

        return result;
    }
}
