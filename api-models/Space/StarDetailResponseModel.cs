namespace psecsapi.api.models.Space
{
    public class StarDetailResponseModel
    {
        public string Type { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Mass { get; set; }
        public decimal Luminosity { get; set; }
        public int Power { get; set; }
    }
}