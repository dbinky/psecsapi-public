namespace psecsapi.Console.Commands.Ship
{
    public record ExtractionJobStatusResponse(
        Guid JobId,
        Guid RawResourceId,
        string ResourceName,
        decimal RatePerMinute,
        decimal? QuantityLimit,
        DateTime StartTime,
        decimal AccumulatedQuantity);
}
