namespace FitPulse.Domain.Entities;

using FitPulse.Domain.Exceptions;

public class Product
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int AvailableStock { get; private set; }

    // Зв'язок 1:1
    public ProductDetail? Detail { get; private set; }

    // Зв'язок 1:N
    public List<ProductImage> Images { get; private set; } = new();

    // Зв'язок M:N
    public List<ProductCategory> ProductCategories { get; private set; } = new();

    private Product() { } // Для EF Core

    public Product(string name, string sku, decimal price, int initialStock = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Назва товару FitPulse не може бути порожньою.");

        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("Артикул (SKU) є обов'язковим.");

        if (price <= 0)
            throw new DomainException("Ціна повинна бути більшою за 0.");

        if (initialStock < 0)
            throw new DomainException("Початковий залишок не може бути від'ємним.");

        Name = name;
        Sku = sku;
        Price = price;
        AvailableStock = initialStock;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new DomainException("Ціна повинна бути додатною.");

        Price = newPrice;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Кількість для списання має бути додатною.");

        if (AvailableStock < quantity)
            throw new DomainException($"Доступно лише {AvailableStock} од. товару.");

        AvailableStock -= quantity;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Кількість для додавання має бути додатною.");

        AvailableStock += quantity;
    }
}