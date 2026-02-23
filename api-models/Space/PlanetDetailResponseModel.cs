namespace psecsapi.api.models.Space
{
    public class PlanetDetailResponseModel
    {
        public string Type { get; set; } = string.Empty;
        public decimal SolidComp { get; set; }
        public SolidCompDetailsResponseModel? SolidDetails { get; set; }
        public decimal LiquidComp { get; set; }
        public decimal GasComp { get; set; }
        public decimal AtmostphericPressure { get; set; }
        public int Claims { get; set; }
    }
}