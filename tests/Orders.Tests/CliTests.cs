using System.Globalization;
using System.Text.RegularExpressions;
using static Orders.Tests.TestData;

namespace Orders.Tests;

public class CliTests
{
    [Fact]
    public void No_command_prints_all_orders_and_the_statistics()
    {
        var (exitCode, output, error) = Run("--file", ProvidedFile);

        Assert.Equal(0, exitCode);
        Assert.Empty(error);
        Assert.Contains("#1 Nino completed 125.00", output);
        Assert.Contains("#4 Ana completed 50.00", output);
        Assert.Contains("Total sales: 425.00", output);
    }

    [Fact]
    public void Stats_command_prints_the_hand_computed_results()
    {
        var (exitCode, output, _) = Run("--file", ProvidedFile, "stats");

        Assert.Equal(0, exitCode);
        Assert.Contains("Completed orders: 3", output);
        Assert.Contains("Total sales: 425.00", output);
        Assert.Contains("Average order value: 141.67", output);
        Assert.Contains("Most popular product: Keyboard, Mouse (tie, 3 units each)", output);
        Assert.Contains("Not counted: 1 cancelled", output);
        Assert.Contains("Monitor 1", output);
        Assert.DoesNotContain("#1", output);
    }

    [Fact]
    public void Search_shows_cancelled_orders_marked_as_not_counted()
    {
        var (exitCode, output, _) = Run("--file", ProvidedFile, "search", "giorgi");

        Assert.Equal(0, exitCode);
        Assert.Contains("#2 Giorgi cancelled 50.00 (not counted in statistics)", output);
        Assert.DoesNotContain("#1", output);
        Assert.DoesNotContain("#3", output);
        Assert.DoesNotContain("#4", output);
    }

    [Fact]
    public void Numbers_are_printed_with_a_dot_even_on_a_Georgian_locale()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ka-GE");
            Assert.Equal(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            var (_, output, _) = Run("--file", ProvidedFile, "stats");

            Assert.Contains("Average order value: 141.67", output);
            Assert.DoesNotContain("141,67", output);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Missing_file_exits_with_code_1_and_a_readable_message()
    {
        var (exitCode, output, error) = Run("--file", "no-such-file.json");

        Assert.Equal(1, exitCode);
        Assert.Empty(output);
        Assert.Contains("no-such-file.json", error);
        Assert.DoesNotContain("Exception", error);
    }

    [Theory]
    [InlineData("bogus")]
    [InlineData("search")]
    [InlineData("list extra")]
    [InlineData("--file")]
    public void Usage_errors_exit_with_code_2_and_show_the_usage(string commandLine)
    {
        var (exitCode, output, error) = Run(commandLine.Split(' '));

        Assert.Equal(2, exitCode);
        Assert.Empty(output);
        Assert.Contains("Usage:", error);
    }

    [Fact]
    public void Empty_file_path_is_a_usage_error()
    {
        var (exitCode, output, error) = Run("--file", "", "stats");

        Assert.Equal(2, exitCode);
        Assert.Empty(output);
        Assert.Contains("Usage:", error);
    }

    private static (int ExitCode, string Output, string Error) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = Program.Run(args, output, error);
        return (exitCode, CollapseSpaces(output.ToString()), CollapseSpaces(error.ToString()));
    }

    private static string CollapseSpaces(string text) => Regex.Replace(text, " {2,}", " ");
}
