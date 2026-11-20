using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Models;
using MiniHttpServer.ORM;

namespace MiniHttpServer.Controllers;

[Controller]
public sealed class ProductController : BaseController
{
    private readonly IOrmContext? _orm;

    public ProductController(IOrmContext? orm = null) => _orm = orm;

    private static readonly IReadOnlyList<Product> DemoProducts = new List<Product>
    {
        new() { Id = 1, Name = "Кофе", Price = 250.00m, Description = "Учебная запись" },
        new() { Id = 2, Name = "Чай", Price = 120.00m, Description = "Учебная запись" },
        new() { Id = 3, Name = "Кружка", Price = 399.90m, Description = "Учебная запись" }
    };

    [Get]
    public async Task<IHttpResult> List()
    {
        IReadOnlyList<Product> products = _orm is null
            ? DemoProducts
            : await _orm.GetAllAsync<Product>("Products");

        return Page("Product/List.html", new { Products = products });
    }
}
