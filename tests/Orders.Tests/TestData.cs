namespace Orders.Tests;

internal static class TestData
{
    public static string ProvidedFile { get; } = Path.Combine(AppContext.BaseDirectory, "orders.json");

    public static Order CreateOrder(int id, string customer, string status, params (string Product, int Quantity, decimal Price)[] items) =>
        new(id, customer, status, items.Select(item => new OrderItem(item.Product, item.Quantity, item.Price)).ToList());
}
