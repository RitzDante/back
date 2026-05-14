namespace ActiveCitizen.Web.Models
{
    public class ClaimViewModel
    {
        public int Id { get; set; }

        public string? Description { get; set; }

        public string Address { get; set; } = string.Empty;

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public int StatusId { get; set; }

        public string ViolationTypeName { get; set; } = string.Empty;

        public int ViolationTypeId { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? PhotoPath { get; set; }

        public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoPath);
    }
}