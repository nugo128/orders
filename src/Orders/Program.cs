using System.Globalization;
using System.Text;

namespace Orders;

public static class Program
{
    private const int Success = 0;
    private const int DataError = 1;
    private const int UsageError = 2;
    private const string DefaultFile = "orders.json";

    private const string Usage = """
        Usage: dotnet run --project src/Orders -- [--file <path>] [list | search <customer> | stats]

          (no command)       all orders, then the statistics
          list               all orders
          search <customer>  orders of one customer: whole name, any letter case
          stats              statistics for completed orders
          --file <path>      orders file to read, default: orders.json in the current folder
          -h, --help         this help
        """;

    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        return Run(args, Console.Out, Console.Error);
    }

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args is ["-h"] or ["--help"])
        {
            output.WriteLine(Usage);
            return Success;
        }

        var command = Parse(args);
        if (command is null)
        {
            error.WriteLine($"Error: invalid arguments: {string.Join(' ', args)}");
            error.WriteLine(Usage);
            return UsageError;
        }

        var (path, print) = command.Value;
        IReadOnlyList<Order> orders;
        try
        {
            orders = OrderLoader.Load(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            error.WriteLine($"Error: could not load orders from '{path}': {ex.Message}");
            return DataError;
        }

        print(output, orders);
        return Success;
    }

    private static (string Path, Action<TextWriter, IReadOnlyList<Order>> Print)? Parse(string[] args)
    {
        var path = DefaultFile;
        var command = args;
        var fileOption = Array.IndexOf(args, "--file");
        if (fileOption >= 0)
        {
            if (fileOption == args.Length - 1)
            {
                return null;
            }

            path = args[fileOption + 1];
            command = [.. args[..fileOption], .. args[(fileOption + 2)..]];
        }

        Action<TextWriter, IReadOnlyList<Order>>? print = command switch
        {
            [] => PrintEverything,
            ["list"] => PrintOrders,
            ["stats"] => PrintStatistics,
            ["search", .. var name] when name.Any(word => !string.IsNullOrWhiteSpace(word)) =>
                (output, orders) => PrintSearch(output, orders, string.Join(' ', name)),
            _ => null,
        };

        if (print is null)
        {
            return null;
        }

        return (path, print);
    }

    private static void PrintEverything(TextWriter output, IReadOnlyList<Order> orders)
    {
        PrintOrders(output, orders);
        output.WriteLine();
        PrintStatistics(output, orders);
    }

    private static void PrintOrders(TextWriter output, IReadOnlyList<Order> orders)
    {
        output.WriteLine($"All orders ({orders.Count}):");
        WriteOrders(output, orders);
    }

    private static void PrintSearch(TextWriter output, IReadOnlyList<Order> orders, string customer)
    {
        var found = OrderAnalytics.FindByCustomer(orders, customer);
        if (found.Count == 0)
        {
            output.WriteLine($"No orders found for customer \"{customer}\".");
            return;
        }

        output.WriteLine($"Orders of customer \"{customer}\" ({found.Count}):");
        WriteOrders(output, found);
    }

    private static void PrintStatistics(TextWriter output, IReadOnlyList<Order> orders)
    {
        var summary = OrderAnalytics.Summarize(orders);
        var average = summary.AverageOrderValue is { } value ? Money(value) : "n/a";

        output.WriteLine("Statistics (completed orders only):");
        output.WriteLine($"  Completed orders:      {summary.CompletedOrders}");
        output.WriteLine($"  Total sales:           {Money(summary.TotalSales)}");
        output.WriteLine($"  Average order value:   {average}");
        output.WriteLine($"  Most popular product:  {DescribeMostPopular(summary)}");
        output.WriteLine($"  Not counted:           {DescribeNotCounted(summary)}");

        if (summary.UnitsByProduct.Count == 0)
        {
            return;
        }

        var productWidth = summary.UnitsByProduct.Keys.Max(product => product.Length);
        output.WriteLine();
        output.WriteLine("Units sold per product:");
        foreach (var (product, units) in summary.UnitsByProduct.OrderByDescending(entry => entry.Value).ThenBy(entry => entry.Key, StringComparer.Ordinal))
        {
            output.WriteLine($"  {product.PadRight(productWidth)}  {units}");
        }
    }

    private static void WriteOrders(TextWriter output, IReadOnlyList<Order> orders)
    {
        if (orders.Count == 0)
        {
            return;
        }

        var customerWidth = orders.Max(order => order.Customer.Trim().Length);
        var statusWidth = orders.Max(order => order.Status.Trim().Length);
        var totalWidth = orders.Max(order => Money(order.Total).Length);
        var productWidth = orders.SelectMany(order => order.Items).Max(item => item.Product.Length);

        foreach (var order in orders)
        {
            var note = order.IsCompleted ? "" : "  (not counted in statistics)";
            output.WriteLine($"#{order.OrderId}  {order.Customer.Trim().PadRight(customerWidth)}  {order.Status.Trim().PadRight(statusWidth)}  {Money(order.Total).PadLeft(totalWidth)}{note}");
            foreach (var item in order.Items)
            {
                output.WriteLine($"      {item.Product.PadRight(productWidth)}  {item.Quantity} x {Money(item.Price)} = {Money(item.LineTotal)}");
            }
        }
    }

    private static string DescribeMostPopular(SalesSummary summary)
    {
        var units = summary.MostPopularUnits == 1 ? "unit" : "units";
        return summary.MostPopularProducts switch
        {
            [] => "n/a",
            [var product] => $"{product} ({summary.MostPopularUnits} {units})",
            var tied => $"{string.Join(", ", tied)} (tie, {summary.MostPopularUnits} {units} each)",
        };
    }

    private static string DescribeNotCounted(SalesSummary summary) =>
        summary.NotCountedByStatus.Count == 0
            ? "none"
            : string.Join(", ", summary.NotCountedByStatus.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => $"{entry.Value} {entry.Key}"));

    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
}
