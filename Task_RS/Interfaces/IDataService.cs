using Task_RS.DTOs;

namespace Task_RS.Interfaces
{
    public interface IDataService
    {
        Task AddProductsAsync(IEnumerable<ProductDto> products);
        Task<List<Product>> GetProductsSortedByPrice();
        Task<decimal> GetSum();
        Task AddGroupAsync(List<List<Product>> products, List<decimal> prices);
        Task<IEnumerable<GroupDto>> GetGroupsAsync();
        Task<IEnumerable<ProductByGroupDto>> GetProductsByGroupAsync(int id);
        Task SetStatusAsync(List<int> ids);
    }
}
