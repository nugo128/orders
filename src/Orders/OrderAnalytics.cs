namespace Orders;

public sealed record SalesSummary(
    int CompletedOrders,
    decimal TotalSales,
    decimal? AverageOrderValue,
    IReadOnlyList<string> MostPopularProducts,
    int MostPopularUnits,
    IReadOnlyDictionary<string, int> UnitsByProduct,
    IReadOnlyDictionary<string, int> NotCountedByStatus);

public static class OrderAnalytics
{
    public static SalesSummary Summarize(IEnumerable<Order> orders) =>
        throw new NotImplementedException();

    public static IReadOnlyList<Order> FindByCustomer(IEnumerable<Order> orders, string customer) =>
        throw new NotImplementedException();
}
