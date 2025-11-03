using Microsoft.Extensions.DependencyInjection;
using Sculptor.Sample;

// NOTE: Sculptor requires closed generic registrations to apply decorators.
// Open generic registrations like AddScoped(typeof(IRepository<>), typeof(DynamoDbRepository<>))
// are not supported and will fall through to standard DI registration without decorators.
var serviceProvider = new ServiceCollection()
    // Example 1: Open generic repository with typeof() syntax
    .AddScoped<IRepository<Customer>, DynamoDbRepository<Customer>>()
    // Example 2: Concrete service with generic attribute syntax
    .AddScoped<IUserService, UserService>()
    .BuildServiceProvider();

// Test Example 1: Open generic repository
Console.WriteLine("=== Example 1: Open Generic Repository [DecoratedBy(typeof(...))] ===");
var customerRepo = serviceProvider.GetRequiredService<IRepository<Customer>>();
Console.WriteLine($"Resolved: {customerRepo.GetType().Name}");
customerRepo.Save(new Customer { Id = 1, Name = "John Doe" });
Console.WriteLine();

// Test Example 2: Concrete service with generic syntax
Console.WriteLine("=== Example 2: Concrete Service [DecoratedBy<T>] ===");
var userService = serviceProvider.GetRequiredService<IUserService>();
userService.CreateUser("John Doe");
Console.WriteLine($"Resolved: {userService.GetType().Name}");
userService.CreateUser("Jane Smith");

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }
}