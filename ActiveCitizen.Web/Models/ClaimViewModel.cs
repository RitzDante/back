namespace ActiveCitizen.Web.Models
{
    public class ClaimViewModel
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string StatusName { get; set; }
        public string ViolationTypeName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
