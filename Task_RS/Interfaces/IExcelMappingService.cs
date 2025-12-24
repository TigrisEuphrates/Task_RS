using Task_RS.DTOs;

namespace Task_RS.Interfaces
{
    public interface IExcelMappingService
    {
        IReadOnlyList<ProductDto> MapProducts(Stream excelStream);
    }
}
