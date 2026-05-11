using System.ComponentModel.DataAnnotations;

namespace ActiveCitizen.Web.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Почта обязательна")]
        [EmailAddress(ErrorMessage = "Некорректная почта")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
