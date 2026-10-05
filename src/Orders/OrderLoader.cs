using System.Globalization;
using System.Text.Json;

namespace Orders;

public static class OrderLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    public static IReadOnlyList<Order> Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Parse(stream);
    }

    public static IReadOnlyList<Order> Parse(Stream json)
    {
        var orders = Deserialize(json) ?? throw new InvalidDataException("The file must contain an array of orders, not null.");
        var seenIds = new HashSet<int>();
        var validated = new List<Order>(orders.Count);
        var grandTotal = 0m;

        for (var index = 0; index < orders.Count; index++)
        {
            var order = orders[index] ?? throw new InvalidDataException($"Order at $[{index}] is null.");
            if (!seenIds.Add(order.OrderId))
            {
                throw new InvalidDataException($"Order {order.OrderId}: duplicate orderId.");
            }

            if (FindProblem(order) is { } problem)
            {
                throw new InvalidDataException($"Order {order.OrderId}: {problem}.");
            }

            try
            {
                grandTotal += order.Total;
            }
            catch (OverflowException)
            {
                throw new InvalidDataException($"Order {order.OrderId}: amounts are too large to add up.");
            }

            validated.Add(order);
        }

        return validated;
    }

    private static List<Order?>? Deserialize(Stream json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<Order?>>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid order data at {ex.Path ?? "$"}: {ex.Message}", ex);
        }
    }

    private static string? FindProblem(Order order)
    {
        if (string.IsNullOrWhiteSpace(order.Customer)) return "customer is empty";
        if (string.IsNullOrWhiteSpace(order.Status)) return "status is empty";
        if (order.Items.Count == 0) return "has no items";

        for (var index = 0; index < order.Items.Count; index++)
        {
            var item = order.Items[index];
            var number = index + 1;
            if (item is null) return $"item {number} is null";
            if (string.IsNullOrWhiteSpace(item.Product)) return $"item {number} has no product name";
            if (item.Quantity < 1) return $"item {number} ({item.Product}) has quantity {item.Quantity}, but it must be at least 1";
            if (item.Price < 0) return $"item {number} ({item.Product}) has a negative price: {item.Price.ToString(CultureInfo.InvariantCulture)}";
        }

        return null;
    }
}
