namespace psecsapi.api.models.Space
{
    public class StarSystemSectorResponseModel
    {
        public List<StarDetailResponseModel> Stars { get; set; } = new();
        public List<OrbitalDetailResponseModel> Orbitals { get; set; } = new();
    }
}