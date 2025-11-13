using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DecoWeaver.Sample;

// NOTE: DecoWeaver requires closed generic registrations to apply decorators.
// Open generic registrations like AddScoped(typeof(IRepository<>), typeof(DynamoDbRepository<>))
// are not supported and will fall through to standard DI registration without decorators.
var serviceProvider = new ServiceCollection()
    // Example 1: Open generic repository with typeof() syntax (parameterless)
    .AddScoped<IRepository<Customer>, DynamoDbRepository<Customer>>()
    // Example 2: Concrete service with generic attribute syntax (parameterless)
    .AddScoped<IUserService, UserService>()
    // Example 3: Factory delegate with simple logic
    .AddScoped<IRepository<Order>, DynamoDbRepository<Order>>(sp =>
        new DynamoDbRepository<Order>())
    // Example 4: Factory delegate with complex dependencies
    .AddSingleton<ILoggerFactory>(sp => LoggerFactory.Create(builder => builder.AddConsole()))
    .AddScoped<IRepository<Product>, RepositoryWithLogger<Product>>(sp =>
    {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<RepositoryWithLogger<Product>>();
        return new RepositoryWithLogger<Product>(logger);
    })
    // Example 5: Keyed service with string key
    .AddKeyedScoped<IAssemblyInterface<string>, ConcreteClass<string>>("primary")
    // Example 6: Multiple keyed services with different keys
    .AddKeyedScoped<IAssemblyInterface<int>, ConcreteClass<int>>("cache")
    .AddKeyedScoped<IAssemblyInterface<int>, ConcreteClass<int>>("database")
    // Example 7: Instance registration (singleton only)
    // .AddSingleton<IRepository<Invoice>>(new DynamoDbRepository<Invoice>())
    .AddSingleton(new DynamoDbRepository<Invoice>())
    // .AddKeyedSingleton<IRepository<Invoice>>("instance", new DynamoDbRepository<Invoice>())
    .BuildServiceProvider();

// Test Example 1: Open generic repository (parameterless)
Console.WriteLine("=== Example 1: Open Generic Repository [Parameterless] ===");
var customerRepo = serviceProvider.GetRequiredService<IRepository<Customer>>();
Console.WriteLine($"Resolved: {customerRepo.GetType().Name}");
customerRepo.Save(new Customer { Id = 1, Name = "John Doe" });
Console.WriteLine();

// Test Example 2: Concrete service (parameterless)
Console.WriteLine("=== Example 2: Concrete Service [Parameterless] ===");
var userService = serviceProvider.GetRequiredService<IUserService>();
Console.WriteLine($"Resolved: {userService.GetType().Name}");
userService.CreateUser("John Doe");
Console.WriteLine();

// Test Example 3: Factory delegate with simple logic
Console.WriteLine("=== Example 3: Factory Delegate (Simple) ===");
var orderRepo = serviceProvider.GetRequiredService<IRepository<Order>>();
Console.WriteLine($"Resolved: {orderRepo.GetType().Name}");
orderRepo.Save(new Order { Id = 1, Total = 99.99m });
Console.WriteLine();

// Test Example 4: Factory delegate with complex dependencies
Console.WriteLine("=== Example 4: Factory Delegate (Complex Dependencies) ===");
var productRepo = serviceProvider.GetRequiredService<IRepository<Product>>();
Console.WriteLine($"Resolved: {productRepo.GetType().Name}");
productRepo.Save(new Product { Id = 1, Name = "Widget" });
Console.WriteLine();

// Test Example 5: Keyed service with string key
Console.WriteLine("=== Example 5: Keyed Service (String Key) ===");
var primaryService = serviceProvider.GetRequiredKeyedService<IAssemblyInterface<string>>("primary");
Console.WriteLine($"Resolved: {primaryService.GetType().Name}");
primaryService.DoSomething("Hello from primary");
Console.WriteLine();

// Test Example 6: Multiple keyed services with different keys
Console.WriteLine("=== Example 6: Keyed Services (Multiple Keys) ===");
var cacheService = serviceProvider.GetRequiredKeyedService<IAssemblyInterface<int>>("cache");
Console.WriteLine($"Resolved 'cache': {cacheService.GetType().Name}");
cacheService.DoSomething(42);
Console.WriteLine();

var databaseService = serviceProvider.GetRequiredKeyedService<IAssemblyInterface<int>>("database");
Console.WriteLine($"Resolved 'database': {databaseService.GetType().Name}");
databaseService.DoSomething(100);
Console.WriteLine();

// Test Example 7: Instance registration
Console.WriteLine("=== Example 7: Instance Registration (Singleton) ===");
var invoiceRepo = serviceProvider.GetRequiredService<IRepository<Invoice>>();
Console.WriteLine($"Resolved: {invoiceRepo.GetType().Name}");
invoiceRepo.Save(new Invoice { Id = 1, Amount = 1500.00m });

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

public class Invoice
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
}