namespace PlantCareMobile.Models
{
    public class PlantNetResponse
    {
        public List<PlantResult>? Results { get; set; }
    }

    public class PlantResult
    {
        public double Score { get; set; }
        public Species? Species { get; set; }
    }

    public class Species
    {
        public string? ScientificNameWithoutAuthor { get; set; }
        public List<string>? CommonNames { get; set; }
    }
}