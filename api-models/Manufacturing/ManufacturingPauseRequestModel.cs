using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Manufacturing;

/// <summary>
/// Request model for pausing a manufacturing job.
/// </summary>
public class ManufacturingPauseRequestModel
{
    /// <summary>ID of the job to pause</summary>
    [Required]
    public Guid JobId { get; set; }
}
