using System.Text;

namespace Orders.Tests;

public class OrderLoaderTests
{
    [Theory]
    [InlineData("""[{"orderId":1,"customer":"Nino","status":"completed","items":[{"product":"Mouse","quantity":0,"price":25}]}]""", "Order 1", "quantity")]
    [InlineData("""[{"orderId":1,"customer":"Nino","status":"completed","items":[{"product":"Mouse","quantity":1,"price":-25}]}]""", "Order 1", "price")]
    [InlineData("""[{"orderId":1,"customer":"Nino","status":"completed","items":[]}]""", "Order 1", "no items")]
    [InlineData("""[{"orderId":1,"customer":"Nino","status":"completed","items":[null]}]""", "Order 1", "null")]
    [InlineData("""[{"orderId":1,"customer":"Nino","status":"completed","items":[{"product":"Mouse","quantity":1,"price":25}]},{"orderId":1,"customer":"Ana","status":"completed","items":[{"product":"Mouse","quantity":1,"price":25}]}]""", "Order 1", "duplicate")]
    [InlineData("""[{"orderId":1,"status":"completed","items":[{"product":"Mouse","quantity":1,"price":25}]}]""", "$[0]", "customer")]
    [InlineData("""[null]""", "$[0]", "null")]
    [InlineData("""null""", "file", "array")]
    public void Invalid_data_is_rejected_with_a_message_saying_where_and_what(string json, string where, string what)
    {
        var error = Assert.Throws<InvalidDataException>(() => OrderLoader.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json))));

        Assert.Contains(where, error.Message);
        Assert.Contains(what, error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
