namespace ActiveCitizen.API.DTOs
{
    public class ClaimDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string StatusName { get; set; }
        public int StatusId { get; set; }
        public string ViolationTypeName { get; set; }
        public int ViolationTypeId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
