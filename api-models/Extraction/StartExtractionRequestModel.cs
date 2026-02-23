using System.ComponentModel.DataAnnotations;

namespace psecsapi.api.models.Extraction
{
    public class StartExtractionRequestModel
    {
        [Required]
        public Guid ResourceId { get; set; }

        public decimal? QuantityLimit { get; set; }
    }
}
