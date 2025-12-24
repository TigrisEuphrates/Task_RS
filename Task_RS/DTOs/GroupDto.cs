namespace Task_RS.DTOs
{
    public class GroupDto
    {
        public int Id { get; set; }
        public decimal PriceEur { get; set; }
    }

    public class ProductByGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public decimal PriceEur { get; set; }
        public int Quantity { get; set; }
    }
}
