using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Manufacturing;

/// <summary>
/// Request model for cancelling a manufacturing job.
/// </summary>
public class ManufacturingCancelRequestModel
{
    /// <summary>ID of the job to cancel</summary>
    [Required]
    public Guid JobId { get; set; }
}
