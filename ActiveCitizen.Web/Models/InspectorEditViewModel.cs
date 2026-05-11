using System.ComponentModel.DataAnnotations;

namespace ActiveCitizen.Web.Models
{
    public class InspectorEditViewModel
    {

        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int? DistrictId { get; set; }
        public string DistrictName { get; set; }

        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}
