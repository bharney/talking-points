using System.ComponentModel.DataAnnotations;

namespace talking_points.Models.ManageViewModels
{
    public class ChangeEmailViewModel
    {
        [Required]
        public string ConfirmedEmail { get; set; }
        [Required]
        public string UnConfirmedEmail { get; set; }
    }
}
