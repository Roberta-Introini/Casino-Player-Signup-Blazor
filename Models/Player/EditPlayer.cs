using System.ComponentModel.DataAnnotations;

namespace PlayerSignupBlazor.Models.PlayerSignup
{
    public class EditPlayer
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Username { get; set; }

        [MinLength(6, ErrorMessage = "The Password field must be a minimum of 6 characters")]
        public string Password { get; set; }

        public EditPlayer() { }

        public EditPlayer(Player user)
        {
            FirstName = user.FirstName;
            LastName = user.LastName;
            Username = user.Username;
        }
    }
}