namespace Orders;

public sealed record OrderItem(string Product, int Quantity, decimal Price)
{
    public decimal LineTotal => throw new NotImplementedException();
}

public sealed record Order(int OrderId, string Customer, string Status, IReadOnlyList<OrderItem> Items)
{
    public decimal Total => throw new NotImplementedException();

    public bool IsCompleted => throw new NotImplementedException();
}
