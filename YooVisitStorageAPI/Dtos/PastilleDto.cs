namespace YooVisitStorageAPI.Dtos
{
    public class PastilleDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string CreatedByUserName { get; set; } // Le nom du créateur
        public double AverageRating { get; set; }
        public List<PhotoDto> Photos { get; set; } = new();
    }
}
