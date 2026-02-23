namespace psecsapi.api.models.Space
{
    public class NebulaSectorResponseModel
    {
        public string Type { get; set; } = string.Empty;
        public decimal OreComp { get; set; }
        public decimal GasComp { get; set; }
        public int Claims { get; set; }
    }
}