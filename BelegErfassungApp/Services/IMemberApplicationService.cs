using BelegErfassungApp.Data;

namespace BelegErfassungApp.Services
{
    public interface IMemberApplicationService
    {
        Task<List<MemberApplication>> GetAllAsync();
        Task<List<MemberApplication>> GetByUserAsync(string userId);
        Task<MemberApplication?> GetByIdAsync(int id);
        Task<MemberApplication> CreateAsync(MemberApplication application);
        Task UpdateStatusAsync(int id, MemberApplicationStatus status, string adminUserId, string? note);
        Task UpdateFieldsAsync(int id, MemberApplication fields, string adminUserId);
        Task DeleteAsync(int id);
    }
}
