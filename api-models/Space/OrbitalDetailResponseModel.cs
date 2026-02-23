namespace psecsapi.api.models.Space
{
    public class OrbitalDetailResponseModel
    {
        public int OrbitalPosition { get; set; }
        public string Type { get; set; } = string.Empty;
        public AsteroidBeltDetailResponseModel? AsteroidBelt { get; set; }
        public PlanetDetailResponseModel? Planet { get; set; }
    }
}