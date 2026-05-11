using System.ComponentModel.DataAnnotations;

namespace ActiveCitizen.Web.Models
{
    public class CreateInspectorViewModel
    {

        [Required(ErrorMessage = "ФИО обязательно")]
        [Display(Name = "ФИО")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректная почта")]
        [Display(Name = "Почта")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Выберите район")]
        [Display(Name = "Район")]
        public string DistrictId { get; set; }
    }
}
