using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

// Модели (сущности)
public class Category
{
	public int Id { get; set; }
	public string Name { get; set; }

	// Навигационное свойство для связи с продуктами
	public List<Product> Products { get; set; }
}

public class Product
{
	public int Id { get; set; }
	public string Name { get; set; }
	public decimal Price { get; set; }

	// Внешний ключ и навигационное свойство для связи с категорией
	public int CategoryId { get; set; }
	public Category Category { get; set; }
}

// Контекст базы данных
public class ApplicationDbContext : DbContext
{
	public DbSet<Category> Categories { get; set; }
	public DbSet<Product> Products { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		// Строка подключения к PostgreSQL
		optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=ProductDb;Username=postgres;Password=your_password");
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Настройка связей между сущностями
		modelBuilder.Entity<Product>()
			.HasOne(p => p.Category)
			.WithMany(c => c.Products)
			.HasForeignKey(p => p.CategoryId);
	}
}

// Репозиторий для работы с данными
public class ProductRepository
{
	private readonly ApplicationDbContext _context;

	public ProductRepository(ApplicationDbContext context)
	{
		_context = context;
	}

	public Category CreateCategory(string name)
	{
		var category = new Category { Name = name };
		_context.Categories.Add(category);
		_context.SaveChanges();
		return category;
	}

	public Product CreateProduct(string name, decimal price, int categoryId)
	{
		var product = new Product
		{
			Name = name,
			Price = price,
			CategoryId = categoryId
		};
		_context.Products.Add(product);
		_context.SaveChanges();
		return product;
	}

	public List<Product> GetProductsByCategory(int categoryId)
	{
		return _context.Products
			.Where(p => p.CategoryId == categoryId)
			.Include(p => p.Category)
			.ToList();
	}

	public Product UpdateProductCategory(int productId, int newCategoryId)
	{
		var product = _context.Products.Find(productId);
		if (product == null)
			throw new Exception("Продукт не найден");

		product.CategoryId = newCategoryId;
		_context.SaveChanges();
		return product;
	}

	public void DeleteCategory(int categoryId)
	{
		var category = _context.Categories
			.Include(c => c.Products)
			.FirstOrDefault(c => c.Id == categoryId);

		if (category == null)
			throw new Exception("Категория не найдена");

		_context.Products.RemoveRange(category.Products);
		_context.Categories.Remove(category);
		_context.SaveChanges();
	}

	public static void Main(string[] args)
	{
		using (var context = new ApplicationDbContext())
		{
			var repository = new ProductRepository(context);

			var electronicsCategory = repository.CreateCategory("Электроника");
			var clothingCategory = repository.CreateCategory("Одежда");

			var smartphone = repository.CreateProduct("Смартфон", 50000, electronicsCategory.Id);
			var laptop = repository.CreateProduct("Ноутбук", 100000, electronicsCategory.Id);
			var tshirt = repository.CreateProduct("Футболка", 1500, clothingCategory.Id);
			var electronicProducts = repository.GetProductsByCategory(electronicsCategory.Id);
			Console.WriteLine("Электронные продукты:");
			foreach (var product in electronicProducts)
			{
				Console.WriteLine($"{product.Name} - {product.Price} руб.");
			}

			repository.UpdateProductCategory(tshirt.Id, electronicsCategory.Id);

			repository.DeleteCategory(clothingCategory.Id);
		}
	}
}