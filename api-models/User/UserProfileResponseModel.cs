namespace psecsapi.api.Models.User
{
    [Serializable]
    public class UserProfileResponseModel
    {
        public string EntityId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public decimal? Tokens { get; set; }
        public DateTime? LastUpdated { get; set; }

        public List<Guid>? OwnedCorps { get; set; }
    }
}
