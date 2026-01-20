using BelegErfassungApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BelegErfassungApp.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditLogService auditLogService,
            ILogger<UserManagementService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    Username = user.UserName ?? string.Empty,
                    Roles = roles.ToList()
                });
            }

            return userDtos;
        }

        public async Task<UserDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Username = user.UserName ?? string.Empty,
                Roles = roles.ToList()
            };
        }

        public async Task<IdentityResult> UpdateUserAsync(string userId, string email, string username)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Benutzer nicht gefunden"
                });
            }

            var oldUsername = user.UserName;
            var oldEmail = user.Email;

            user.Email = email;
            user.UserName = username;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _auditLogService.LogAsync(
                    "UserManagement",
                    "UPDATE",
                    $"Benutzer aktualisiert: {oldUsername} -> {username}, {oldEmail} -> {email}",
                    userId
                );

                _logger.LogInformation("Benutzer {UserId} wurde aktualisiert", userId);
            }

            return result;
        }

        public async Task<IdentityResult> ResetPasswordAsync(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Benutzer nicht gefunden"
                });
            }

            // Entferne altes Passwort
            await _userManager.RemovePasswordAsync(user);

            // Setze neues Passwort
            var result = await _userManager.AddPasswordAsync(user, newPassword);

            if (result.Succeeded)
            {
                await _auditLogService.LogAsync(
                    "UserManagement",
                    "PASSWORD_RESET",
                    $"Passwort für Benutzer {user.UserName} wurde zurückgesetzt",
                    userId
                );

                _logger.LogInformation("Passwort für Benutzer {Username} wurde zurückgesetzt", user.UserName);
            }

            return result;
        }


        public async Task<IdentityResult> CreateUserAsync(string email, string username, string password, string role)
        {
            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true // Auto-confirm für Admin-erstellte Benutzer
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(role) && await _roleManager.RoleExistsAsync(role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }

                await _auditLogService.LogAsync(
                    "UserManagement",
                    "CREATE",
                    $"Benutzer {username} wurde erstellt mit Rolle {role}",
                    user.Id
                );

                _logger.LogInformation("Benutzer {Username} wurde erfolgreich erstellt", username);
            }
            else
            {
                _logger.LogWarning("Fehler beim Erstellen des Benutzers {Username}: {Errors}",
                    username, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }

        public async Task<IdentityResult> UpdateUserRoleAsync(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Benutzer nicht gefunden"
                });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!removeResult.Succeeded)
            {
                return removeResult;
            }

            var addResult = await _userManager.AddToRoleAsync(user, newRole);

            if (addResult.Succeeded)
            {
                await _auditLogService.LogAsync(
                    "UserManagement",
                    "UPDATE_ROLE",
                    $"Rolle von Benutzer {user.UserName} wurde von {string.Join(", ", currentRoles)} zu {newRole} geändert",
                    userId
                );

                _logger.LogInformation("Rolle von Benutzer {Username} wurde zu {NewRole} geändert",
                    user.UserName, newRole);
            }

            return addResult;
        }

        public async Task<IdentityResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Benutzer nicht gefunden"
                });
            }

            var username = user.UserName;
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                await _auditLogService.LogAsync(
                    "UserManagement",
                    "DELETE",
                    $"Benutzer {username} wurde gelöscht",
                    userId
                );

                _logger.LogInformation("Benutzer {Username} wurde gelöscht", username);
            }

            return result;
        }

        public async Task<List<string>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        }
    }
}
