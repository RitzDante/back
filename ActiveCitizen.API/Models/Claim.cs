namespace ActiveCitizen.API.Models
{
    public class Claim
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int StatusId { get; set; }
        public int ViolationTypeId { get; set; }
        public int DistrictId { get; set; }
        public string Address { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? PhotoPath { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    
        public User? User { get; set; }
        public Status? Status { get; set; }
        public ViolationType? ViolationType { get; set; }
        public District? District { get; set; }
    }
}
