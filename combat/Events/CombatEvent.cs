using Google.Protobuf;

namespace psecsapi.Combat.Events;

/// <summary>
/// Base class for all combat events. Provides common serialization infrastructure.
/// Each event knows how to serialize/deserialize itself using Google.Protobuf
/// CodedOutputStream/CodedInputStream for compact binary encoding.
/// </summary>
public abstract class CombatEvent
{
    /// <summary>The event type discriminator for the replay stream.</summary>
    public abstract CombatEventType EventType { get; }

    /// <summary>Serialize this event to a Protobuf-encoded byte array.</summary>
    public abstract byte[] Serialize();

    /// <summary>
    /// Helper to write a string field using Protobuf encoding.
    /// Field number is used as the wire tag.
    /// </summary>
    protected static void WriteString(CodedOutputStream output, int fieldNumber, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            output.WriteTag(fieldNumber, WireFormat.WireType.LengthDelimited);
            output.WriteString(value);
        }
    }

    /// <summary>Helper to write a double field using Protobuf encoding.</summary>
    protected static void WriteDouble(CodedOutputStream output, int fieldNumber, double value)
    {
        if (value != 0.0)
        {
            output.WriteTag(fieldNumber, WireFormat.WireType.Fixed64);
            output.WriteDouble(value);
        }
    }

    /// <summary>Helper to write an int32 field using Protobuf encoding.</summary>
    protected static void WriteInt32(CodedOutputStream output, int fieldNumber, int value)
    {
        if (value != 0)
        {
            output.WriteTag(fieldNumber, WireFormat.WireType.Varint);
            output.WriteInt32(value);
        }
    }

    /// <summary>Helper to write a repeated string field.</summary>
    protected static void WriteRepeatedString(CodedOutputStream output, int fieldNumber, List<string> values)
    {
        foreach (var value in values)
        {
            output.WriteTag(fieldNumber, WireFormat.WireType.LengthDelimited);
            output.WriteString(value);
        }
    }

    /// <summary>
    /// Read fields from a CodedInputStream, dispatching by field number.
    /// Subclasses provide a handler action that processes each field.
    /// </summary>
    protected static void ReadFields(CodedInputStream input, Action<int, CodedInputStream> fieldHandler)
    {
        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            int fieldNumber = WireFormat.GetTagFieldNumber(tag);
            fieldHandler(fieldNumber, input);
        }
    }
}
