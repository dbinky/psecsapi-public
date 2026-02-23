namespace psecsapi.api.models.Manufacturing;

/// <summary>
/// Response model for starting a manufacturing job.
/// </summary>
public class ManufacturingStartResponseModel
{
    /// <summary>Whether the job was started successfully</summary>
    public bool Success { get; set; }

    /// <summary>ID of the created job</summary>
    public Guid JobId { get; set; }

    /// <summary>Current job status</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>When the first tick will process</summary>
    public DateTime? NextTickAt { get; set; }

    /// <summary>Error message if not successful</summary>
    public string? ErrorMessage { get; set; }
}
