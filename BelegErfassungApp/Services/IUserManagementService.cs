using Microsoft.AspNetCore.Identity;
using BelegErfassungApp.Data;

namespace BelegErfassungApp.Services
{
    public interface IUserManagementService
    {
        Task<List<UserDto>> GetAllUsersAsync();
        Task<IdentityResult> CreateUserAsync(string email, string username, string password, string role);
        Task<IdentityResult> UpdateUserRoleAsync(string userId, string newRole);
        Task<IdentityResult> DeleteUserAsync(string userId);
        Task<List<string>> GetAllRolesAsync();
        Task<UserDto?> GetUserByIdAsync(string userId);
        Task<IdentityResult> UpdateUserAsync(string userId, string email, string username);
        Task<IdentityResult> ResetPasswordAsync(string userId, string newPassword);

    }
}