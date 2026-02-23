namespace psecsapi.api.models.Space
{
    public class RubbleSectorResponseModel
    {
        public string Type { get; set; } = string.Empty;
        public decimal MetalComp { get; set; }
        public decimal OreComp { get; set; }
        public decimal GemstoneComp { get; set; }
        public int Claims { get; set; }
    }
}