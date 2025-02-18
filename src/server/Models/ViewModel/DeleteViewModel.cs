using System.ComponentModel.DataAnnotations;

namespace talking_points.Models.ManageViewModels
{
    public class DeleteAccountViewModel
    {
        [Required]
        public string UserName { get; set; }
    }
}
