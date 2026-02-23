namespace psecsapi.api.models.Research
{
    public class ResearchStatusResponseModel
    {
        public int TotalCapacity { get; set; }
        public List<ActiveProjectModel> ActiveProjects { get; set; } = new();
        public int TotalAllocation { get; set; }
        public int AvailableAllocation => 100 - TotalAllocation;
    }
}
