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
    public static SalesSummary Summarize(IEnumerable<Order> orders)
    {
        var completedOrders = 0;
        var totalSales = 0m;
        var unitsByProduct = new Dictionary<string, int>();
        var notCountedByStatus = new Dictionary<string, int>();

        foreach (var order in orders)
        {
            if (!order.IsCompleted)
            {
                var status = order.Status.Trim().ToLowerInvariant();
                notCountedByStatus[status] = notCountedByStatus.GetValueOrDefault(status) + 1;
                continue;
            }

            completedOrders++;
            foreach (var item in order.Items)
            {
                totalSales += item.LineTotal;
                unitsByProduct[item.Product] = unitsByProduct.GetValueOrDefault(item.Product) + item.Quantity;
            }
        }

        decimal? averageOrderValue = completedOrders == 0
            ? null
            : Math.Round(totalSales / completedOrders, 2, MidpointRounding.AwayFromZero);

        var mostPopularUnits = unitsByProduct.Count == 0 ? 0 : unitsByProduct.Values.Max();
        var mostPopularProducts = unitsByProduct
            .Where(entry => entry.Value == mostPopularUnits)
            .Select(entry => entry.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

        return new SalesSummary(
            completedOrders,
            totalSales,
            averageOrderValue,
            mostPopularProducts,
            mostPopularUnits,
            unitsByProduct,
            notCountedByStatus);
    }

    public static IReadOnlyList<Order> FindByCustomer(IEnumerable<Order> orders, string customer)
    {
        var name = customer.Trim();
        return orders
            .Where(order => string.Equals(order.Customer.Trim(), name, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
