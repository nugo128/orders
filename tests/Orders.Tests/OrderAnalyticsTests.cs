using static Orders.Tests.TestData;

namespace Orders.Tests;

public class OrderAnalyticsTests
{
    [Fact]
    public void Provided_file_gives_the_hand_computed_results()
    {
        var summary = OrderAnalytics.Summarize(OrderLoader.Load(ProvidedFile));

        Assert.Equal(3, summary.CompletedOrders);
        Assert.Equal(425m, summary.TotalSales);
        Assert.Equal(141.67m, summary.AverageOrderValue);
        Assert.Equal(new[] { "Keyboard", "Mouse" }, summary.MostPopularProducts);
        Assert.Equal(3, summary.MostPopularUnits);
        Assert.Equal(1, summary.UnitsByProduct["Monitor"]);
        Assert.Equal(1, summary.NotCountedByStatus["cancelled"]);
        Assert.Single(summary.NotCountedByStatus);
    }

    [Fact]
    public void Cancelled_orders_do_not_change_any_statistic()
    {
        var summary = OrderAnalytics.Summarize(
        [
            CreateOrder(1, "Nino", "completed", ("Mouse", 2, 25m)),
            CreateOrder(2, "Giorgi", "cancelled", ("Monitor", 10, 200m)),
        ]);

        Assert.Equal(1, summary.CompletedOrders);
        Assert.Equal(50m, summary.TotalSales);
        Assert.Equal(50m, summary.AverageOrderValue);
        Assert.Equal(new[] { "Mouse" }, summary.MostPopularProducts);
        Assert.DoesNotContain("Monitor", summary.UnitsByProduct.Keys);
    }

    [Fact]
    public void All_products_tied_for_first_place_are_reported_alphabetically()
    {
        var summary = OrderAnalytics.Summarize(
        [
            CreateOrder(1, "Ana", "completed", ("Mouse", 2, 25m), ("Keyboard", 1, 50m)),
            CreateOrder(2, "Ana", "completed", ("Keyboard", 1, 50m), ("Monitor", 1, 200m)),
        ]);

        Assert.Equal(new[] { "Keyboard", "Mouse" }, summary.MostPopularProducts);
        Assert.Equal(2, summary.MostPopularUnits);
    }

    [Fact]
    public void Tied_products_are_sorted_ignoring_letter_case()
    {
        var summary = OrderAnalytics.Summarize(
        [
            CreateOrder(1, "Ana", "completed", ("Mouse", 1, 25m), ("keyboard", 1, 50m)),
        ]);

        Assert.Equal(new[] { "keyboard", "Mouse" }, summary.MostPopularProducts);
    }

    [Fact]
    public void Units_sold_are_added_up_without_overflow()
    {
        var summary = OrderAnalytics.Summarize(
        [
            CreateOrder(1, "Ana", "completed", ("Big", int.MaxValue, 0m), ("Small", 1, 1m)),
            CreateOrder(2, "Ana", "completed", ("Big", int.MaxValue, 0m)),
        ]);

        Assert.Equal(new[] { "Big" }, summary.MostPopularProducts);
        Assert.Equal(2L * int.MaxValue, summary.MostPopularUnits);
    }

    [Fact]
    public void A_single_top_product_is_reported_alone()
    {
        var summary = OrderAnalytics.Summarize(
        [
            CreateOrder(1, "Ana", "completed", ("Keyboard", 1, 50m), ("Mouse", 3, 25m)),
        ]);

        Assert.Equal(new[] { "Mouse" }, summary.MostPopularProducts);
        Assert.Equal(3, summary.MostPopularUnits);
    }

    [Fact]
    public void No_completed_orders_gives_zero_totals_and_no_average()
    {
        var noOrders = OrderAnalytics.Summarize([]);
        var onlyCancelled = OrderAnalytics.Summarize([CreateOrder(1, "Giorgi", "cancelled", ("Keyboard", 1, 50m))]);

        foreach (var summary in new[] { noOrders, onlyCancelled })
        {
            Assert.Equal(0, summary.CompletedOrders);
            Assert.Equal(0m, summary.TotalSales);
            Assert.Null(summary.AverageOrderValue);
            Assert.Empty(summary.MostPopularProducts);
            Assert.Equal(0, summary.MostPopularUnits);
        }
    }

    [Fact]
    public void Only_completed_orders_count_and_status_ignores_case_and_spaces()
    {
        var summary = OrderAnalytics.Summarize(
        [
            CreateOrder(1, "Nino", " Completed ", ("Mouse", 1, 25m)),
            CreateOrder(2, "Nino", "pending", ("Mouse", 1, 25m)),
            CreateOrder(3, "Nino", "canceled", ("Mouse", 1, 25m)),
            CreateOrder(4, "Nino", "Cancelled", ("Mouse", 1, 25m)),
        ]);

        Assert.Equal(1, summary.CompletedOrders);
        Assert.Equal(25m, summary.TotalSales);
        Assert.Equal(
            "canceled=1, cancelled=1, pending=1",
            string.Join(", ", summary.NotCountedByStatus.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}")));
    }

    [Fact]
    public void Average_order_value_is_rounded_half_away_from_zero()
    {
        var summary = OrderAnalytics.Summarize(
        [
            CreateOrder(1, "Ana", "completed", ("Pen", 1, 1.00m)),
            CreateOrder(2, "Ana", "completed", ("Clip", 1, 0.25m)),
        ]);

        Assert.Equal(0.63m, summary.AverageOrderValue);
    }

    [Fact]
    public void Customer_search_matches_the_whole_name_ignoring_case_and_keeps_cancelled_orders()
    {
        Order[] orders =
        [
            CreateOrder(1, "Nino", "completed", ("Mouse", 1, 25m)),
            CreateOrder(2, "Giorgi", "cancelled", ("Keyboard", 1, 50m)),
            CreateOrder(3, "Anano", "completed", ("Mouse", 1, 25m)),
            CreateOrder(4, "nino ", "completed", ("Monitor", 1, 200m)),
        ];

        Assert.Equal(new[] { 1, 4 }, OrderAnalytics.FindByCustomer(orders, "  NINO ").Select(o => o.OrderId));
        Assert.Equal(new[] { 2 }, OrderAnalytics.FindByCustomer(orders, "giorgi").Select(o => o.OrderId));
        Assert.Empty(OrderAnalytics.FindByCustomer(orders, "Ana"));
    }
}
