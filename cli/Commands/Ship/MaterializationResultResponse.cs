namespace psecsapi.Console.Commands.Ship
{
    public record MaterializationResultResponse(
        Guid JobId,
        Guid BoxedResourceId,
        Guid RawResourceId,
        string ResourceName,
        decimal MaterializedQuantity);
}
