namespace psecsapi.api.models.Manufacturing;

/// <summary>
/// Response model for manufacturing status queries.
/// </summary>
public class ManufacturingStatusResponseModel
{
    /// <summary>List of manufacturing jobs</summary>
    public List<ManufacturingJobModel> Jobs { get; set; } = new();

    /// <summary>Total active jobs</summary>
    public int TotalActive => Jobs.Count(j => j.Status == "Active");

    /// <summary>Total paused/waiting jobs</summary>
    public int TotalPaused => Jobs.Count(j => j.Status != "Active" && j.Status != "Completed" && j.Status != "Cancelled");
}
