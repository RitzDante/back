using System.ComponentModel.DataAnnotations;

namespace ActiveCitizen.API.DTOs
{
    public class UpdateStatusDto
    {
        [Required]
        public int StatusId { get; set; }
    }
}
