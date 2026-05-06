namespace LostAndFound.Application.Interfaces
{
    public interface IAdminUserService
    {
        Task<bool> DeleteUserAsync(int userId);
    }
}
