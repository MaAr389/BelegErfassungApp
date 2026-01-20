using Microsoft.AspNetCore.Identity;

namespace BelegErfassungApp.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        // Hier können zusätzliche Properties hinzugefügt werden
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? RegistrationDate { get; set; } = DateTime.UtcNow;
    }

}
