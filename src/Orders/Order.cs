namespace Orders;

public sealed record OrderItem(string Product, int Quantity, decimal Price)
{
    public decimal LineTotal => Quantity * Price;
}

public sealed record Order(int OrderId, string Customer, string Status, IReadOnlyList<OrderItem> Items)
{
    public const string CompletedStatus = "completed";

    public decimal Total => Items.Sum(item => item.LineTotal);

    public bool IsCompleted => string.Equals(Status.Trim(), CompletedStatus, StringComparison.OrdinalIgnoreCase);
}
