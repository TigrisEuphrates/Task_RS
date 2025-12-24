namespace Task_RS.DTOs
{
    public class ProductDto
    {
        public string Name { get; init; } = null!;
        public string Unit { get; init; } = null!;
        public decimal PriceEur { get; init; }
        public int Quantity { get; init; }
    }
}
