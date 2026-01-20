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
    }
}