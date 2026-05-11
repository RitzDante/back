using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ActiveCitizen.API.DTOs
{
    public class CreateClaimDto
    {
        [Required(ErrorMessage = "Адрес обязателен.")]
        [StringLength(200, ErrorMessage = "Адрес не может превышать 200 символов.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Широта обязательна.")]
        [Range(-90.0, 90.0, ErrorMessage = "Недопустимое значение широты.")]
         public decimal Latitude { get; set; }

        [Required(ErrorMessage = "Долгота обязательна.")]
        [Range(-180.0, 180.0, ErrorMessage = "Недопустимое значение долготы.")]
        public decimal Longitude { get; set; }

        [Required(ErrorMessage = "Тип нарушения обязателен.")]
        public int ViolationTypeId { get; set; }

        [Required(ErrorMessage = "Район обязателен.")] 
        public string DistrictName { get; set; }

        [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов.")]
        public string? Description { get; set; }

        public IFormFile? Photo { get; set; }
    }
}
