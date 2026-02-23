using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Manufacturing;

/// <summary>
/// Request model for resuming a manufacturing job.
/// </summary>
public class ManufacturingResumeRequestModel
{
    /// <summary>ID of the job to resume</summary>
    [Required]
    public Guid JobId { get; set; }
}
