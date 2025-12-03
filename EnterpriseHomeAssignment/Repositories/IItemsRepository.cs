using EnterpriseHomeAssignment.Models;

namespace EnterpriseHomeAssignment.Repositories
{
    public interface IItemsRepository
    {
        Task<List<IItemValidating>> GetAllAsync();
        Task SaveAsync(List<IItemValidating> items);
        Task ApproveAsync(List<int> itemIds);
    }
}
